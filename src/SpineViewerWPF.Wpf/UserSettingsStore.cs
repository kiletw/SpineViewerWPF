using System.IO;
using System.Text.Json;

namespace SpineViewerWPF.Wpf;

// TASK-066 / ADR-010: cross-session user preferences, kept apart from the
// Viewer project sidecar. A missing or unreadable file means defaults; a
// failed write keeps the in-memory value for the current session.
public sealed class UserSettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string? path;
    private string? ffmpegPath;

    private UserSettingsStore(string? path)
    {
        this.path = path;
        if (path is null || !File.Exists(path)) return;
        try
        {
            var document = JsonSerializer.Deserialize<SettingsDocument>(File.ReadAllText(path));
            ffmpegPath = string.IsNullOrWhiteSpace(document?.FfmpegPath) ? null : document.FfmpegPath;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            ffmpegPath = null;
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

    public bool SetFfmpegPath(string? value)
    {
        ffmpegPath = string.IsNullOrWhiteSpace(value) ? null : value;
        if (path is null) return true;
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporary = path + ".tmp";
            File.WriteAllText(temporary, JsonSerializer.Serialize(new SettingsDocument(1, ffmpegPath), JsonOptions));
            File.Move(temporary, path, overwrite: true);
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    private sealed record SettingsDocument(int SchemaVersion, string? FfmpegPath);
}
