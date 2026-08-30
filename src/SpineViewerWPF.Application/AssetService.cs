using SpineViewerWPF.Core;

namespace SpineViewerWPF.Application;

public interface IRuntimeAdapter
{
    string RuntimeLine { get; }
    InspectResult Inspect(string skeletonPath, string atlasPath, bool overridden, CancellationToken cancellationToken);
    IRuntimeRenderSession OpenSession(string skeletonPath, string atlasPath, CancellationToken cancellationToken);
}

public interface IRuntimeRenderSession : IDisposable
{
    void Render(RenderRequest request, CancellationToken cancellationToken);
    RenderedFrame RenderFrame(FrameRenderRequest request, CancellationToken cancellationToken);
    PreviewSceneFrame RenderScene(PreviewSceneRequest request, CancellationToken cancellationToken);
}

public sealed class AssetRenderSession : IDisposable
{
    private readonly object gate = new();
    private readonly IRuntimeRenderSession session;
    private readonly string skeletonPath;
    private readonly string atlasPath;
    private bool disposed;

    internal AssetRenderSession(IRuntimeRenderSession session, string skeletonPath, string atlasPath)
    {
        this.session = session;
        this.skeletonPath = skeletonPath;
        this.atlasPath = atlasPath;
    }

    public string Render(
        string animation,
        float timeSeconds,
        int width,
        int height,
        string outputPath,
        bool overwrite,
        bool pma,
        IReadOnlyList<string> skins,
        CancellationToken cancellationToken = default,
        float trackAlpha = 1,
        IReadOnlyList<SlotDisplayDocument>? slots = null)
    {
        if (string.IsNullOrWhiteSpace(animation)) throw new ArgumentException("Animation is required.");
        if (!float.IsFinite(timeSeconds) || timeSeconds < 0) throw new ArgumentOutOfRangeException(nameof(timeSeconds));
        if (!float.IsFinite(trackAlpha) || trackAlpha is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(trackAlpha), "Track alpha must be between 0 and 1.");
        if (width is < 1 or > 16384 || height is < 1 or > 16384
            || (long)width * height > AssetService.MaxFramePixels)
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "Dimensions must be between 1 and 16384 and contain at most 16777216 pixels.");

        var output = Path.GetFullPath(outputPath);
        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            cancellationToken.ThrowIfCancellationRequested();
            if (File.Exists(output) && !overwrite) throw new IOException($"Output exists: {output}");
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? ".");
            session.Render(
                new RenderRequest(skeletonPath, atlasPath, animation, timeSeconds, width, height, output, overwrite, pma, skins, trackAlpha, slots),
                cancellationToken);
        }
        return output;
    }

    public RenderedFrame RenderFrame(
        string animation,
        float timeSeconds,
        int width,
        int height,
        bool pma,
        IReadOnlyList<string> skins,
        CancellationToken cancellationToken = default,
        float trackAlpha = 1,
        bool linearFiltering = true,
        IReadOnlyList<SlotDisplayDocument>? slots = null)
    {
        if (string.IsNullOrWhiteSpace(animation)) throw new ArgumentException("Animation is required.");
        if (!float.IsFinite(timeSeconds) || timeSeconds < 0) throw new ArgumentOutOfRangeException(nameof(timeSeconds));
        if (!float.IsFinite(trackAlpha) || trackAlpha is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(trackAlpha), "Track alpha must be between 0 and 1.");
        if (width is < 1 or > 4096 || height is < 1 or > 4096)
            throw new ArgumentOutOfRangeException(nameof(width), "Interactive frame dimensions must be between 1 and 4096.");

        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            cancellationToken.ThrowIfCancellationRequested();
            return session.RenderFrame(
                new FrameRenderRequest(animation, timeSeconds, width, height, pma, skins, trackAlpha, linearFiltering, slots),
                cancellationToken);
        }
    }

    public PreviewSceneFrame RenderScene(
        string animation,
        float timeSeconds,
        bool pma,
        IReadOnlyList<string> skins,
        CancellationToken cancellationToken = default,
        float trackAlpha = 1,
        IReadOnlyList<SlotDisplayDocument>? slots = null)
    {
        if (string.IsNullOrWhiteSpace(animation)) throw new ArgumentException("Animation is required.");
        if (!float.IsFinite(timeSeconds) || timeSeconds < 0) throw new ArgumentOutOfRangeException(nameof(timeSeconds));
        if (!float.IsFinite(trackAlpha) || trackAlpha is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(trackAlpha), "Track alpha must be between 0 and 1.");

        lock (gate)
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            cancellationToken.ThrowIfCancellationRequested();
            return session.RenderScene(
                new PreviewSceneRequest(animation, timeSeconds, pma, skins, trackAlpha, slots),
                cancellationToken);
        }
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            session.Dispose();
            disposed = true;
        }
    }
}

public sealed class AssetService
{
    internal const long MaxFramePixels = 4096L * 4096;
    internal const long MaxCompositeBufferBytes = 256L * 1024 * 1024;

    private readonly IReadOnlyList<IRuntimeAdapter> runtimes;

    public AssetService(IRuntimeAdapter runtime)
        : this([runtime])
    {
    }

    public AssetService(IEnumerable<IRuntimeAdapter> runtimes)
    {
        this.runtimes = runtimes?.Where(item => item is not null).ToArray()
            ?? throw new ArgumentNullException(nameof(runtimes));
        if (this.runtimes.Count == 0) throw new ArgumentException("At least one Runtime adapter is required.", nameof(runtimes));
    }

    public InspectResult Inspect(
        string skeletonPath,
        string? atlasPath,
        string? runtimeOverride,
        CancellationToken cancellationToken = default)
    {
        var paths = ValidateInput(skeletonPath, atlasPath);
        return InspectWithRuntime(paths, runtimeOverride, cancellationToken).Result;
    }

    public string Render(
        string skeletonPath,
        string? atlasPath,
        string? runtimeOverride,
        string animation,
        float timeSeconds,
        int width,
        int height,
        string outputPath,
        bool overwrite,
        bool pma,
        IReadOnlyList<string> skins,
        CancellationToken cancellationToken = default,
        float trackAlpha = 1,
        IReadOnlyList<SlotDisplayDocument>? slots = null)
    {
        var opened = OpenRenderSession(skeletonPath, atlasPath, runtimeOverride, cancellationToken);
        using (opened.Session)
            return opened.Session.Render(animation, timeSeconds, width, height, outputPath, overwrite, pma, skins, cancellationToken, trackAlpha, slots);
    }

    public (InspectResult Inspection, AssetRenderSession Session) OpenRenderSession(
        string skeletonPath,
        string? atlasPath,
        string? runtimeOverride,
        CancellationToken cancellationToken = default)
    {
        var paths = ValidateInput(skeletonPath, atlasPath);
        var selected = InspectWithRuntime(paths, runtimeOverride, cancellationToken);
        var session = selected.Adapter.OpenSession(paths.Skeleton, paths.Atlas, cancellationToken);
        return (selected.Result, new AssetRenderSession(session, paths.Skeleton, paths.Atlas));
    }

    public IReadOnlyList<string> RenderScene(
        IReadOnlyList<SceneLayerRenderRequest> requests,
        int width,
        int height,
        CancellationToken cancellationToken = default)
    {
        if (requests is null) throw new ArgumentNullException(nameof(requests));
        if (requests.Count > 8) throw new ArgumentOutOfRangeException(nameof(requests), "A scene is limited to eight layers.");
        if (width is < 1 or > 16384 || height is < 1 or > 16384
            || (long)width * height > MaxFramePixels)
            throw new ArgumentOutOfRangeException(
                nameof(width),
                "Dimensions must be between 1 and 16384 and contain at most 16777216 pixels.");

        var outputs = new string[requests.Count];
        for (var index = 0; index < requests.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var request = requests[index] ?? throw new ArgumentException("Scene layer request is required.", nameof(requests));
            outputs[index] = Render(
                request.SkeletonPath,
                request.AtlasPath,
                request.RuntimeOverride,
                request.Animation,
                request.TimeSeconds,
                width,
                height,
                request.OutputPath,
                request.Overwrite,
                request.Pma,
                request.Skins ?? [],
                cancellationToken,
                request.TrackAlpha,
                request.Slots);
        }
        return outputs;
    }

    public SceneLayerOpenResult OpenSceneLayer(
        string skeletonPath,
        string? atlasPath,
        string? runtimeOverride,
        int width,
        int height,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        var opened = OpenRenderSession(skeletonPath, atlasPath, runtimeOverride, cancellationToken);
        using var session = opened.Session;
        var inspection = opened.Inspection;
        var animation = inspection.Animations.FirstOrDefault()
            ?? throw new InvalidDataException("The current renderer requires an animation.");
        var skin = inspection.Skins.FirstOrDefault() ?? "default";
        try
        {
            session.Render(
                animation.Name,
                animation.DurationSeconds / 2,
                width,
                height,
                outputPath,
                true,
                false,
                [skin],
                cancellationToken);
            return new SceneLayerOpenResult(inspection, animation.Name, skin, Path.GetFullPath(outputPath));
        }
        catch
        {
            TryDelete(outputPath);
            throw;
        }
    }

    public AnimationExportResult Export(
        AnimationExportRequest request,
        CancellationToken cancellationToken = default,
        IProgress<AnimationExportProgress>? progress = null)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        if (string.IsNullOrWhiteSpace(request.Animation)) throw new ArgumentException("Animation is required.", nameof(request));
        if (!float.IsFinite(request.DurationSeconds) || request.DurationSeconds < 0)
            throw new ArgumentOutOfRangeException(nameof(request.DurationSeconds));
        if (!float.IsFinite(request.FramesPerSecond) || request.FramesPerSecond <= 0 || request.FramesPerSecond > 240)
            throw new ArgumentOutOfRangeException(nameof(request.FramesPerSecond), "Frames per second must be between 0 and 240.");
        if (request.Width is < 1 or > 16384 || request.Height is < 1 or > 16384
            || (long)request.Width * request.Height > MaxFramePixels)
            throw new ArgumentOutOfRangeException(
                nameof(request.Width),
                "Dimensions must be between 1 and 16384 and contain at most 16777216 pixels.");
        if (request.SceneLayers is not null)
        {
            if (request.Width > 4096 || request.Height > 4096)
                throw new ArgumentOutOfRangeException(nameof(request.Width), "Composite dimensions must be between 1 and 4096.");
            ValidateSceneLayers(request.SceneLayers);
            var retainedFrameCount = request.SceneLayers.Count(layer => layer.IsVisible && layer.Opacity > 0) + 1L;
            if ((long)request.Width * request.Height * 4 * retainedFrameCount > MaxCompositeBufferBytes)
                throw new ArgumentOutOfRangeException(
                    nameof(request.SceneLayers),
                    "Composite frame buffers must not exceed 256 MiB.");
        }

        var frameCountValue = Math.Floor(request.DurationSeconds * request.FramesPerSecond) + 1;
        if (!double.IsFinite(frameCountValue) || frameCountValue > 10000)
            throw new ArgumentOutOfRangeException(nameof(request.DurationSeconds), "The export is limited to 10000 frames.");
        var frameCount = Math.Max(1, (int)frameCountValue);
        var directory = Path.GetFullPath(request.OutputDirectory);
        var rawPrefix = request.FilePrefix?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(rawPrefix) || Path.GetFileName(rawPrefix) != rawPrefix)
            throw new ArgumentException("File prefix must be a file name.", nameof(request.FilePrefix));
        var prefix = Path.GetFileNameWithoutExtension(rawPrefix);
        if (string.IsNullOrWhiteSpace(prefix)) throw new ArgumentException("File prefix is required.", nameof(request.FilePrefix));

        var digits = Math.Max(4, frameCount.ToString().Length);
        var outputs = Enumerable.Range(0, frameCount)
            .Select(index => Path.Combine(directory, $"{prefix}-{index.ToString(string.Concat("D", digits))}.png"))
            .ToArray();
        if (!request.Overwrite)
        {
            var existing = outputs.FirstOrDefault(File.Exists);
            if (existing is not null) throw new IOException($"Output exists: {existing}");
        }

        Directory.CreateDirectory(directory);
        var created = new List<string>(frameCount);
        progress?.Report(new AnimationExportProgress(0, frameCount));
        try
        {
            if (request.SceneLayers is null)
            {
                var opened = OpenRenderSession(
                    request.SkeletonPath,
                    request.AtlasPath,
                    request.RuntimeOverride,
                    cancellationToken);
                using var session = opened.Session;
                for (var index = 0; index < frameCount; index++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var time = Math.Min(request.DurationSeconds, index / request.FramesPerSecond);
                    session.Render(
                        request.Animation,
                        time,
                        request.Width,
                        request.Height,
                        outputs[index],
                        request.Overwrite,
                        request.Pma,
                        request.Skins ?? [],
                        cancellationToken,
                        request.TrackAlpha,
                        request.Slots);
                    created.Add(outputs[index]);
                    progress?.Report(new AnimationExportProgress(index + 1, frameCount));
                }
            }
            else
                ExportSceneFrames(request, outputs, created, progress, cancellationToken);
        }
        catch
        {
            if (!request.Overwrite)
                foreach (var output in created) TryDelete(output);
            throw;
        }

        return new AnimationExportResult(outputs, frameCount, request.DurationSeconds);
    }

    private void ExportSceneFrames(
        AnimationExportRequest request,
        IReadOnlyList<string> outputs,
        ICollection<string> created,
        IProgress<AnimationExportProgress>? progress,
        CancellationToken cancellationToken)
    {
        var openedLayers = new List<(SceneLayerDocument Layer, AssetRenderSession Session, float Duration)>();
        try
        {
            foreach (var item in request.SceneLayers!
                         .Select((layer, index) => (Layer: layer, Index: index))
                         .Where(item => item.Layer.IsVisible && item.Layer.Opacity > 0)
                         .OrderBy(item => item.Layer.ZIndex)
                         .ThenBy(item => item.Index))
            {
                cancellationToken.ThrowIfCancellationRequested();
                var opened = OpenRenderSession(
                    item.Layer.SkeletonPath,
                    item.Layer.AtlasPath,
                    item.Layer.RuntimeOverride,
                    cancellationToken);
                var animation = opened.Inspection.Animations.FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, item.Layer.Animation, StringComparison.Ordinal));
                if (animation is null)
                {
                    opened.Session.Dispose();
                    throw new InvalidDataException($"Animation not found: {item.Layer.Animation}");
                }
                openedLayers.Add((item.Layer, opened.Session, animation.DurationSeconds));
            }

            for (var index = 0; index < outputs.Count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var time = Math.Min(request.DurationSeconds, index / request.FramesPerSecond);
                var frames = new SceneFrameLayer[openedLayers.Count];
                for (var layerIndex = 0; layerIndex < openedLayers.Count; layerIndex++)
                {
                    var item = openedLayers[layerIndex];
                    var frame = item.Session.RenderFrame(
                        item.Layer.Animation,
                        Math.Min(time, item.Duration),
                        request.Width,
                        request.Height,
                        item.Layer.Pma ?? false,
                        string.IsNullOrWhiteSpace(item.Layer.SelectedSkin) ? [] : [item.Layer.SelectedSkin],
                        cancellationToken,
                        (float)(item.Layer.TrackAlpha ?? 1),
                        true,
                        item.Layer.Slots);
                    frames[layerIndex] = new SceneFrameLayer(frame, item.Layer);
                }

                var composite = SceneFrameCompositor.Compose(frames, request.Width, request.Height);
                PngFrameWriter.Write(outputs[index], composite, request.Overwrite);
                created.Add(outputs[index]);
                progress?.Report(new AnimationExportProgress(index + 1, outputs.Count));
            }
        }
        finally
        {
            foreach (var item in openedLayers) item.Session.Dispose();
        }
    }

    private static void ValidateSceneLayers(IReadOnlyList<SceneLayerDocument> layers)
    {
        if (layers.Count > 8)
            throw new ArgumentOutOfRangeException(nameof(layers), "A scene is limited to eight layers.");
        foreach (var layer in layers)
        {
            if (layer is null) throw new ArgumentException("Scene layer is required.", nameof(layers));
            if (string.IsNullOrWhiteSpace(layer.SkeletonPath))
                throw new ArgumentException("Scene layer skeleton path is required.", nameof(layers));
            if (string.IsNullOrWhiteSpace(layer.Animation))
                throw new ArgumentException("Scene layer animation is required.", nameof(layers));
            if (!double.IsFinite(layer.ModelX) || !double.IsFinite(layer.ModelY)
                || !double.IsFinite(layer.ModelRotation)
                || !double.IsFinite(layer.ModelScale) || layer.ModelScale <= 0)
                throw new ArgumentOutOfRangeException(nameof(layers), "Layer transforms must be finite and scale must be positive.");
            if (!double.IsFinite(layer.Opacity) || layer.Opacity is < 0 or > 1)
                throw new ArgumentOutOfRangeException(nameof(layers), "Layer opacity must be between 0 and 1.");
            if (layer.TrackAlpha is { } trackAlpha
                && (!double.IsFinite(trackAlpha) || trackAlpha is < 0 or > 1))
                throw new ArgumentOutOfRangeException(nameof(layers), "Layer track alpha must be between 0 and 1.");
        }
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private (IRuntimeAdapter Adapter, InspectResult Result) InspectWithRuntime(
        (string Skeleton, string Atlas) paths,
        string? runtimeOverride,
        CancellationToken cancellationToken)
    {
        var candidates = runtimes
            .Where(item => runtimeOverride is null || MatchesRuntime(item, runtimeOverride))
            .ToArray();
        if (candidates.Length == 0)
            throw new NotSupportedException($"Runtime '{runtimeOverride}' is not supported.");

        Exception? last = null;
        foreach (var candidate in candidates)
        {
            try
            {
                return (candidate, candidate.Inspect(paths.Skeleton, paths.Atlas, runtimeOverride is not null, cancellationToken));
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                last = exception;
                if (runtimeOverride is not null) break;
            }
        }

        if (last is not null) throw last;
        throw new NotSupportedException(
            runtimeOverride is null
                ? "No installed Runtime adapter could read this export."
                : $"Runtime '{runtimeOverride}' could not read this export.");
    }

    private static bool MatchesRuntime(IRuntimeAdapter runtime, string requested) =>
        string.Equals(requested, runtime.RuntimeLine, StringComparison.OrdinalIgnoreCase)
        || requested.StartsWith(runtime.RuntimeLine + ".", StringComparison.OrdinalIgnoreCase);

    private (string Skeleton, string Atlas) ValidateInput(string skeletonPath, string? atlasPath)
    {

        var skeleton = Path.GetFullPath(skeletonPath);
        if (!File.Exists(skeleton)) throw new FileNotFoundException("Skeleton file not found.", skeleton);

        var atlas = atlasPath is null
            ? FindAtlas(skeleton)
            : Path.GetFullPath(atlasPath);
        if (!File.Exists(atlas)) throw new FileNotFoundException("Atlas file not found.", atlas);

        return (skeleton, atlas);
    }

    private static string FindAtlas(string skeleton)
    {
        var directory = Path.GetDirectoryName(skeleton) ?? ".";
        var stem = Path.GetFileNameWithoutExtension(skeleton);
        var exact = Path.ChangeExtension(skeleton, ".atlas");
        if (File.Exists(exact)) return exact;

        foreach (var suffix in new[] { "-pro", "-ess" })
        {
            if (!stem.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)) continue;
            var baseAtlas = Path.Combine(directory, stem[..^suffix.Length] + ".atlas");
            if (File.Exists(baseAtlas)) return baseAtlas;
        }

        var nearby = Directory.EnumerateFiles(directory, "*.atlas", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return nearby.Length switch
        {
            1 => nearby[0],
            > 1 => throw new InvalidDataException($"Multiple atlas files found beside '{skeleton}'. Specify an atlas explicitly."),
            _ => throw new FileNotFoundException("Atlas file not found beside skeleton.", exact)
        };
    }
}
