using System.Reflection;
using System.Runtime.InteropServices;

namespace SpineViewerWPF.Wpf;

// TASK-076: facts shown in Help > About and copied for bug reports.
public sealed record AboutInfo(
    string Version,
    IReadOnlyList<string> RuntimeLines,
    string Renderer,
    string FfmpegStatus)
{
    public const string RepositoryUrl = "https://github.com/kiletw/SpineViewerWPF";
    public const string ReleasesUrl = RepositoryUrl + "/releases";
    public const string NoticesUrl = RepositoryUrl + "/blob/master/THIRD-PARTY-NOTICES.md";
    public const string SpineLicenseUrl = "https://esotericsoftware.com/spine-editor-license";

    public static AboutInfo Create(Assembly assembly, IEnumerable<string> runtimeLines, string renderer, string ffmpegStatus) =>
        new(
            DisplayVersion(assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                ?? assembly.GetName().Version?.ToString()),
            runtimeLines.Distinct(StringComparer.Ordinal).OrderBy(VersionKey).ToArray(),
            renderer,
            ffmpegStatus);

    // "3.0.0-alpha.1+abc123" -> "3.0.0-alpha.1": the build metadata is a commit hash.
    public static string DisplayVersion(string? informational)
    {
        if (string.IsNullOrWhiteSpace(informational)) return "unknown";
        var plus = informational.IndexOf('+');
        return plus > 0 ? informational[..plus] : informational;
    }

    // Plain text for bug reports.
    public string CopyText =>
        string.Join(
            Environment.NewLine,
            $"SpineViewerWPF {Version}",
            $"Windows {Environment.OSVersion.Version} ({RuntimeInformation.OSArchitecture})",
            $".NET {Environment.Version}",
            $"Renderer: {Renderer}",
            $"FFmpeg: {FfmpegStatus}",
            $"Spine Runtimes: {string.Join(", ", RuntimeLines)}");

    // Numeric order; a missing or non-numeric part ("4.0" is 4.0.64, "3.2.xx")
    // sorts as the newest of its line.
    private static string VersionKey(string line)
    {
        var parts = line.Split('.');
        return string.Join('.', Enumerable.Range(0, 3).Select(index =>
            index < parts.Length && int.TryParse(parts[index], out var number) ? number.ToString("D4") : "9999"));
    }
}
