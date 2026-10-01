using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;
using SpineViewerWPF.Core;

namespace SpineViewerWPF.Application;

// TASK-066 / ADR-010: FFmpeg is optional and user-installed. A remembered path
// wins when the file exists; otherwise PATH is searched.
public static class FfmpegLocator
{
    public static string ExecutableName => OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";

    public static string? Find(string? preferredPath)
    {
        if (!string.IsNullOrWhiteSpace(preferredPath) && File.Exists(preferredPath))
            return Path.GetFullPath(preferredPath);

        var path = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var entry in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            try
            {
                var candidate = Path.Combine(entry.Trim().Trim('"'), ExecutableName);
                if (File.Exists(candidate)) return Path.GetFullPath(candidate);
            }
            catch (ArgumentException)
            {
                // Ignore malformed PATH entries.
            }
        }
        return null;
    }
}

internal static partial class FfmpegEncoder
{
    private const int StderrTailLines = 40;

    internal static string Extension(AnimationEncodeFormat format) => format switch
    {
        AnimationEncodeFormat.Gif => ".gif",
        AnimationEncodeFormat.WebP => ".webp",
        AnimationEncodeFormat.Apng => ".png",
        AnimationEncodeFormat.Mp4 => ".mp4",
        _ => throw new ArgumentOutOfRangeException(nameof(format))
    };

    internal static bool IsValidBackground(string? value) => value is not null && BackgroundPattern().IsMatch(value);

    // Arguments are passed as a list (never through a shell). Frames are read as
    // an image sequence starting at index 0.
    internal static IReadOnlyList<string> BuildArguments(
        AnimationEncodeOptions options,
        float framesPerSecond,
        string inputPattern,
        int width,
        int height,
        string outputPath)
    {
        var fps = framesPerSecond.ToString("0.###", CultureInfo.InvariantCulture);
        var arguments = new List<string>
        {
            "-hide_banner", "-nostdin", "-loglevel", "error", "-y",
            "-progress", "pipe:1", "-nostats",
            "-framerate", fps, "-start_number", "0", "-i", inputPattern
        };
        switch (options.Format)
        {
            case AnimationEncodeFormat.Gif:
                arguments.AddRange([
                    "-filter_complex",
                    "[0:v]split[a][b];[a]palettegen=reserve_transparent=1:stats_mode=full[p];[b][p]paletteuse=alpha_threshold=128",
                    "-loop", "0"]);
                break;
            case AnimationEncodeFormat.WebP:
                arguments.AddRange(["-c:v", "libwebp_anim", "-lossless", "0", "-quality", "90", "-pix_fmt", "yuva420p", "-loop", "0"]);
                break;
            case AnimationEncodeFormat.Apng:
                arguments.AddRange(["-c:v", "apng", "-plays", "0", "-f", "apng"]);
                break;
            case AnimationEncodeFormat.Mp4:
                var color = "0x" + options.VideoBackground[1..];
                arguments.AddRange([
                    "-filter_complex",
                    $"color=c={color}:s={width}x{height}:r={fps}[bg];[bg][0:v]overlay=shortest=1:format=auto,pad=ceil(iw/2)*2:ceil(ih/2)*2:color={color},format=yuv420p",
                    "-c:v", "libx264", "-preset", "medium", "-crf", "18", "-movflags", "+faststart"]);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(options));
        }
        arguments.Add(outputPath);
        return arguments;
    }

    // Runs FFmpeg to completion. Progress reports encoded frames; cancellation
    // terminates the whole process tree.
    internal static void Run(
        string ffmpegPath,
        IReadOnlyList<string> arguments,
        string workingDirectory,
        Action<int>? frameEncoded,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var startInfo = new ProcessStartInfo(ffmpegPath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDirectory
        };
        foreach (var argument in arguments) startInfo.ArgumentList.Add(argument);

        var stderr = new Queue<string>();
        using var process = new Process { StartInfo = startInfo };
        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is { } line && line.StartsWith("frame=", StringComparison.Ordinal)
                && int.TryParse(line.AsSpan(6), NumberStyles.Integer, CultureInfo.InvariantCulture, out var frame))
                frameEncoded?.Invoke(frame);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (string.IsNullOrWhiteSpace(e.Data)) return;
            lock (stderr)
            {
                stderr.Enqueue(e.Data);
                while (stderr.Count > StderrTailLines) stderr.Dequeue();
            }
        };

        try
        {
            process.Start();
        }
        catch (Win32Exception exception)
        {
            throw new InvalidOperationException($"FFmpeg could not be started: {exception.Message}", exception);
        }
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        using (cancellationToken.Register(() =>
               {
                   try
                   {
                       if (!process.HasExited) process.Kill(entireProcessTree: true);
                   }
                   catch (InvalidOperationException)
                   {
                       // The process exited between the check and the kill.
                   }
               }))
        {
            process.WaitForExit();
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (process.ExitCode != 0)
        {
            string tail;
            lock (stderr) tail = string.Join(Environment.NewLine, stderr);
            throw new InvalidOperationException(
                $"FFmpeg failed with exit code {process.ExitCode}." + (tail.Length > 0 ? $" {tail}" : ""));
        }
    }

    [GeneratedRegex("^#[0-9A-Fa-f]{6}$")]
    private static partial Regex BackgroundPattern();
}
