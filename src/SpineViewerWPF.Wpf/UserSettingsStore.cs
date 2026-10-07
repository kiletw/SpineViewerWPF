using System.IO;
using System.Text.Json;

namespace SpineViewerWPF.Wpf;

// TASK-066 / ADR-010: cross-session user preferences, kept apart from the
// Viewer project sidecar. A missing or unreadable file means defaults; a
// failed write keeps the in-memory value for the current session.
// TASK-073 adds recent files and the auto-reload preference.
public sealed class UserSettingsStore
{
    public const int MaxRecentFiles = 10;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string? path;
    private string? ffmpegPath;
    private List<string> recentFiles = [];
    private bool autoReload = true;

    private UserSettingsStore(string? path)
    {
        this.path = path;
        if (path is null || !File.Exists(path)) return;
        try
        {
            var document = JsonSerializer.Deserialize<SettingsDocument>(File.ReadAllText(path));
            ffmpegPath = string.IsNullOrWhiteSpace(document?.FfmpegPath) ? null : document.FfmpegPath;
            recentFiles = (document?.RecentFiles ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxRecentFiles)
                .ToList();
            autoReload = document?.AutoReload ?? true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            ffmpegPath = null;
            recentFiles = [];
            autoReload = true;
        }
    }

    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SpineViewerWPF",
        "settings.json");

    public static UserSettingsStore Load(string? path = null) => new(path ?? DefaultPath);

    // For tests and previews: never touches disk.
    public static UserSettingsStore InMemory() => new(null);

    public string? FfmpegPath => ffmpegPath;

    // Most recent first, unique ignoring case.
    public IReadOnlyList<string> RecentFiles => recentFiles;

    public bool AutoReload => autoReload;

    public bool SetFfmpegPath(string? value)
    {
        ffmpegPath = string.IsNullOrWhiteSpace(value) ? null : value;
        return Save();
    }

    public bool AddRecentFile(string file)
    {
        if (string.IsNullOrWhiteSpace(file)) return true;
        var full = Path.GetFullPath(file);
        recentFiles = recentFiles
            .Where(item => !string.Equals(item, full, StringComparison.OrdinalIgnoreCase))
            .Prepend(full)
            .Take(MaxRecentFiles)
            .ToList();
        return Save();
    }

    public bool RemoveRecentFile(string file)
    {
        recentFiles = recentFiles.Where(item => !string.Equals(item, file, StringComparison.OrdinalIgnoreCase)).ToList();
        return Save();
    }

    public bool ClearRecentFiles()
    {
        recentFiles = [];
        return Save();
    }

    public bool SetAutoReload(bool value)
    {
        autoReload = value;
        return Save();
    }

    private bool Save()
    {
        if (path is null) return true;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + ".tmp";
            File.WriteAllText(
                temporary,
                JsonSerializer.Serialize(new SettingsDocument(1, ffmpegPath, recentFiles, autoReload), JsonOptions));
            File.Move(temporary, path, overwrite: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private sealed record SettingsDocument(
        int SchemaVersion,
        string? FfmpegPath,
        IReadOnlyList<string>? RecentFiles = null,
        bool? AutoReload = null);
}
