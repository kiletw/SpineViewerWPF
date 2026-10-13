using System.IO;
using System.Text.Json;

namespace SpineViewerWPF.Wpf;

// TASK-066 / ADR-010: cross-session user preferences, kept apart from the
// Viewer project sidecar. A missing or unreadable file means defaults; a
// failed write keeps the in-memory value for the current session.
// TASK-068 adds the asset browser folder; TASK-073 adds recent files and the
// auto-reload preference.
public sealed class UserSettingsStore
{
    public const int MaxRecentFiles = 10;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string? path;
    private string? ffmpegPath;
    private string? browserFolder;
    private bool browserIncludeSubfolders;
    private List<string> recentFiles = [];
    private bool autoReload = true;
    private bool showAxes = true;
    private bool showExportFrame = true;
    private double mixDuration;
    private string language = UiLanguage.System;

    private UserSettingsStore(string? path)
    {
        this.path = path;
        if (path is null || !File.Exists(path)) return;
        try
        {
            var document = JsonSerializer.Deserialize<SettingsDocument>(File.ReadAllText(path));
            ffmpegPath = Normalize(document?.FfmpegPath);
            browserFolder = Normalize(document?.BrowserFolder);
            browserIncludeSubfolders = document?.BrowserIncludeSubfolders ?? false;
            recentFiles = (document?.RecentFiles ?? [])
                .Where(item => !string.IsNullOrWhiteSpace(item))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxRecentFiles)
                .ToList();
            autoReload = document?.AutoReload ?? true;
            showAxes = document?.ShowAxes ?? true;
            showExportFrame = document?.ShowExportFrame ?? true;
            mixDuration = ClampMix(document?.MixDuration ?? 0);
            language = UiLanguage.Normalize(document?.Language);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            ffmpegPath = null;
            browserFolder = null;
            browserIncludeSubfolders = false;
            recentFiles = [];
            autoReload = true;
            showAxes = true;
            showExportFrame = true;
            mixDuration = 0;
            language = UiLanguage.System;
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

    // TASK-068: the asset browser's last folder and subfolder choice.
    public string? BrowserFolder => browserFolder;
    public bool BrowserIncludeSubfolders => browserIncludeSubfolders;

    // Most recent first, unique ignoring case.
    public IReadOnlyList<string> RecentFiles => recentFiles;

    public bool AutoReload => autoReload;

    // TASK-071: viewport guides.
    public bool ShowAxes => showAxes;
    public bool ShowExportFrame => showExportFrame;

    // TASK-074: preview crossfade when the animation changes, 0 to 5 seconds; 0 is off.
    public double MixDuration => mixDuration;

    // TASK-077: "system", "en", or "zh-TW"; applied at the next start.
    public string Language => language;

    public bool SetFfmpegPath(string? value)
    {
        ffmpegPath = Normalize(value);
        return Save();
    }

    public bool SetBrowser(string? folder, bool includeSubfolders)
    {
        browserFolder = Normalize(folder);
        browserIncludeSubfolders = includeSubfolders;
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

    public bool SetShowAxes(bool value)
    {
        showAxes = value;
        return Save();
    }

    public bool SetShowExportFrame(bool value)
    {
        showExportFrame = value;
        return Save();
    }

    public bool SetMixDuration(double value)
    {
        mixDuration = ClampMix(value);
        return Save();
    }

    public bool SetLanguage(string? value)
    {
        language = UiLanguage.Normalize(value);
        return Save();
    }

    private static double ClampMix(double value) => double.IsFinite(value) ? Math.Clamp(value, 0, 5) : 0;

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private bool Save()
    {
        if (path is null) return true;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + ".tmp";
            File.WriteAllText(
                temporary,
                JsonSerializer.Serialize(
                    new SettingsDocument(1, ffmpegPath, browserFolder, browserIncludeSubfolders, recentFiles, autoReload, showAxes, showExportFrame, mixDuration, language),
                    JsonOptions));
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
        string? BrowserFolder = null,
        bool? BrowserIncludeSubfolders = null,
        IReadOnlyList<string>? RecentFiles = null,
        bool? AutoReload = null,
        bool? ShowAxes = null,
        bool? ShowExportFrame = null,
        double? MixDuration = null,
        string? Language = null);
}
