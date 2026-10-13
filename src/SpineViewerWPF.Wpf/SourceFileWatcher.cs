using System.IO;

namespace SpineViewerWPF.Wpf;

// TASK-073: watches loaded source files (skeleton, atlas, atlas page textures)
// and reports the changed set once writes have been quiet for the debounce
// interval, so a multi-file Spine export triggers one reload. The callback runs
// on a thread-pool thread.
internal sealed class SourceFileWatcher : IDisposable
{
    private static readonly string[] ImageExtensions = [".png", ".jpg", ".jpeg", ".webp", ".bmp", ".tga", ".ppm"];
    private readonly object gate = new();
    private readonly Action<IReadOnlyCollection<string>> changed;
    private readonly TimeSpan debounce;
    private readonly Timer timer;
    private readonly List<FileSystemWatcher> watchers = [];
    private HashSet<string> files = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> pending = new(StringComparer.OrdinalIgnoreCase);
    private bool disposed;

    public SourceFileWatcher(Action<IReadOnlyCollection<string>> changed, TimeSpan debounce)
    {
        this.changed = changed;
        this.debounce = debounce;
        timer = new Timer(_ => Flush());
    }

    public IReadOnlyCollection<string> WatchedFiles
    {
        get
        {
            lock (gate) return files.ToArray();
        }
    }

    // Replaces the watched set; an empty set stops watching.
    public void Watch(IEnumerable<string> paths)
    {
        var next = new HashSet<string>(
            paths.Where(path => !string.IsNullOrWhiteSpace(path)).Select(Path.GetFullPath),
            StringComparer.OrdinalIgnoreCase);
        lock (gate)
        {
            if (disposed || next.SetEquals(files)) return;
            files = next;
            pending.Clear();
            foreach (var watcher in watchers) watcher.Dispose();
            watchers.Clear();
            foreach (var directory in files.Select(Path.GetDirectoryName).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory)) continue;
                var watcher = new FileSystemWatcher(directory)
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                    IncludeSubdirectories = false
                };
                watcher.Changed += (_, e) => Record(e.FullPath);
                watcher.Created += (_, e) => Record(e.FullPath);
                watcher.Renamed += (_, e) => Record(e.FullPath);
                watcher.EnableRaisingEvents = true;
                watchers.Add(watcher);
            }
        }
    }

    // The skeleton, the atlas, and each atlas page image named in the atlas.
    public static IEnumerable<string> SourceFiles(string skeletonPath, string? atlasPath)
    {
        yield return skeletonPath;
        if (string.IsNullOrWhiteSpace(atlasPath)) yield break;
        yield return atlasPath;
        string[] lines;
        try
        {
            lines = File.ReadAllLines(atlasPath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            yield break;
        }
        var directory = Path.GetDirectoryName(Path.GetFullPath(atlasPath)) ?? ".";
        foreach (var line in lines.Select(item => item.Trim()))
        {
            if (line.Length == 0 || line.Contains(':')) continue;
            if (!ImageExtensions.Contains(Path.GetExtension(line), StringComparer.OrdinalIgnoreCase)) continue;
            yield return Path.Combine(directory, line);
        }
    }

    private void Record(string path)
    {
        lock (gate)
        {
            if (disposed || !files.Contains(path)) return;
            pending.Add(path);
            timer.Change(debounce, Timeout.InfiniteTimeSpan);
        }
    }

    private void Flush()
    {
        string[] batch;
        lock (gate)
        {
            if (disposed || pending.Count == 0) return;
            batch = pending.ToArray();
            pending.Clear();
        }
        changed(batch);
    }

    public void Dispose()
    {
        lock (gate)
        {
            if (disposed) return;
            disposed = true;
            foreach (var watcher in watchers) watcher.Dispose();
            watchers.Clear();
            pending.Clear();
        }
        timer.Dispose();
    }
}
