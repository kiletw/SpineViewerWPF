using System.Text;

namespace SpineViewerWPF.Application;

// TASK-068: lists Spine skeleton files in a folder for the WPF asset browser.
// JSON files are accepted only when their beginning looks like Spine data, so
// Viewer project sidecars and unrelated JSON are skipped without parsing them.
public static class AssetBrowser
{
    public const int DefaultLimit = 5000;
    private const int JsonProbeBytes = 4096;
    private static readonly byte[][] SpineJsonMarkers =
    [
        Encoding.ASCII.GetBytes("\"skeleton\""),
        Encoding.ASCII.GetBytes("\"bones\"")
    ];

    public static AssetBrowserScan Scan(
        string folder,
        bool includeSubfolders,
        int limit = DefaultLimit,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(folder)) throw new ArgumentException("Folder is required.", nameof(folder));
        if (limit < 1) throw new ArgumentOutOfRangeException(nameof(limit));
        var root = Path.GetFullPath(folder);
        if (!Directory.Exists(root)) throw new DirectoryNotFoundException($"Folder not found: {root}");

        var options = new EnumerationOptions
        {
            RecurseSubdirectories = includeSubfolders,
            IgnoreInaccessible = true,
            MatchCasing = MatchCasing.CaseInsensitive,
            AttributesToSkip = FileAttributes.Hidden | FileAttributes.System
        };
        var found = new List<string>();
        var truncated = false;
        foreach (var path in Directory.EnumerateFiles(root, "*", options))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsSkeletonFile(path)) continue;
            if (found.Count == limit)
            {
                truncated = true;
                break;
            }
            found.Add(path);
        }
        found.Sort(StringComparer.OrdinalIgnoreCase);
        return new AssetBrowserScan(root, found, truncated);
    }

    public static bool IsSkeletonFile(string path)
    {
        var name = Path.GetFileName(path);
        if (name.EndsWith(".skel", StringComparison.OrdinalIgnoreCase)) return true;
        if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(ViewerProjectStore.Extension, StringComparison.OrdinalIgnoreCase))
            return false;
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var buffer = new byte[JsonProbeBytes];
            var read = stream.Read(buffer, 0, buffer.Length);
            var prefix = buffer.AsSpan(0, read);
            foreach (var marker in SpineJsonMarkers)
                if (prefix.IndexOf(marker) >= 0) return true;
            return false;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}

public sealed record AssetBrowserScan(string Folder, IReadOnlyList<string> Files, bool Truncated);
