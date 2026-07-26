using SpineViewerWPF.Core;

namespace SpineViewerWPF.Application;

public interface IRuntimeAdapter
{
    string RuntimeLine { get; }
    InspectResult Inspect(string skeletonPath, string atlasPath, bool overridden, CancellationToken cancellationToken);
    void Render(RenderRequest request, CancellationToken cancellationToken);
}

public sealed class AssetService(IRuntimeAdapter runtime)
{
    public InspectResult Inspect(
        string skeletonPath,
        string? atlasPath,
        string? runtimeOverride,
        CancellationToken cancellationToken = default)
    {
        var paths = ValidateInput(skeletonPath, atlasPath, runtimeOverride);
        return runtime.Inspect(paths.Skeleton, paths.Atlas, runtimeOverride is not null, cancellationToken);
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
        CancellationToken cancellationToken = default)
    {
        var paths = ValidateInput(skeletonPath, atlasPath, runtimeOverride);
        if (string.IsNullOrWhiteSpace(animation)) throw new ArgumentException("Animation is required.");
        if (!float.IsFinite(timeSeconds) || timeSeconds < 0) throw new ArgumentOutOfRangeException(nameof(timeSeconds));
        if (width is < 1 or > 16384 || height is < 1 or > 16384)
            throw new ArgumentOutOfRangeException(nameof(width), "Dimensions must be between 1 and 16384.");

        var output = Path.GetFullPath(outputPath);
        if (File.Exists(output) && !overwrite) throw new IOException($"Output exists: {output}");
        Directory.CreateDirectory(Path.GetDirectoryName(output) ?? ".");

        runtime.Render(
            new RenderRequest(paths.Skeleton, paths.Atlas, animation, timeSeconds, width, height, output, overwrite, pma, skins),
            cancellationToken);
        return output;
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
        if (request.Width is < 1 or > 16384 || request.Height is < 1 or > 16384)
            throw new ArgumentOutOfRangeException(nameof(request.Width), "Dimensions must be between 1 and 16384.");

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
            for (var index = 0; index < frameCount; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var time = Math.Min(request.DurationSeconds, index / request.FramesPerSecond);
                Render(
                    request.SkeletonPath,
                    request.AtlasPath,
                    request.RuntimeOverride,
                    request.Animation,
                    time,
                    request.Width,
                    request.Height,
                    outputs[index],
                    request.Overwrite,
                    request.Pma,
                    request.Skins ?? [],
                    cancellationToken);
                created.Add(outputs[index]);
                progress?.Report(new AnimationExportProgress(index + 1, frameCount));
            }
        }
        catch
        {
            if (!request.Overwrite)
                foreach (var output in created) TryDelete(output);
            throw;
        }

        return new AnimationExportResult(outputs, frameCount, request.DurationSeconds);
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private (string Skeleton, string Atlas) ValidateInput(string skeletonPath, string? atlasPath, string? runtimeOverride)
    {
        if (runtimeOverride is not null && runtimeOverride is not ("4.1" or "4.1.00"))
            throw new NotSupportedException($"Runtime '{runtimeOverride}' is not supported by this slice.");

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
