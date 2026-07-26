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
