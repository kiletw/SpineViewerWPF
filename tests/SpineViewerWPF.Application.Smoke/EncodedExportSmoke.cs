using System.Diagnostics;
using System.Text;
using SpineViewerWPF.Application;
using SpineViewerWPF.Core;
using SpineViewerWPF.Wpf;

// TASK-066: GIF / WebP / APNG / MP4 export through a user-installed FFmpeg.
static class EncodedExportSmoke
{
    public static void Run(AssetService service, AnimationExportRequest request, string temporaryRoot)
    {
        var root = Path.Combine(temporaryRoot, "encoded");
        Directory.CreateDirectory(root);
        var tempBefore = CountWorkDirectories();

        Expect<FileNotFoundException>(() => service.ExportEncoded(
            request,
            new AnimationEncodeOptions(AnimationEncodeFormat.Gif, Path.Combine(root, "missing-ffmpeg.exe")),
            Path.Combine(root, "missing.gif"),
            false));
        Check(!File.Exists(Path.Combine(root, "missing.gif")), "Missing FFmpeg wrote an output.");

        // GIF uses a palette pass and an encode pass instead of one split graph,
        // which would hold every frame in memory until the palette is complete.
        var gifPasses = FfmpegEncoder.BuildPasses(
            new AnimationEncodeOptions(AnimationEncodeFormat.Gif, "ffmpeg"), 30, "frame-%04d.png", 64, 64, "out.gif", root);
        Check(gifPasses.Count == 2
              && gifPasses[0].Any(argument => argument.StartsWith("palettegen", StringComparison.Ordinal))
              && !gifPasses[0].Any(argument => argument.Contains("paletteuse", StringComparison.Ordinal))
              && gifPasses[1].Contains(Path.Combine(root, "palette.png"))
              && gifPasses[1].Any(argument => argument.Contains("paletteuse", StringComparison.Ordinal))
              && !gifPasses.SelectMany(pass => pass).Any(argument => argument.Contains("split", StringComparison.Ordinal)),
            "GIF encoding did not use separate palette and encode passes.");
        foreach (var format in new[] { AnimationEncodeFormat.WebP, AnimationEncodeFormat.Apng, AnimationEncodeFormat.Mp4 })
            Check(FfmpegEncoder.BuildPasses(new AnimationEncodeOptions(format, "ffmpeg"), 30, "frame-%04d.png", 64, 64, "out", root).Count == 1,
                $"{format} encoding did not use a single pass.");

        var ffmpeg = FfmpegLocator.Find(null);
        if (ffmpeg is null)
        {
            Console.WriteLine("TASK-066: FFmpeg was not found on PATH; encoding checks skipped.");
            return;
        }
        Check(FfmpegLocator.Find(Path.Combine(root, "missing-ffmpeg.exe")) == ffmpeg,
            "An invalid remembered FFmpeg path did not fall back to PATH.");

        Expect<ArgumentException>(() => service.ExportEncoded(
            request,
            new AnimationEncodeOptions(AnimationEncodeFormat.Mp4, ffmpeg, "black"),
            Path.Combine(root, "invalid.mp4"),
            false));

        foreach (var (format, extension, signature) in new (AnimationEncodeFormat, string, Func<byte[], bool>)[]
                 {
                     (AnimationEncodeFormat.Gif, ".gif", data => Ascii(data, 0, "GIF89a")),
                     (AnimationEncodeFormat.WebP, ".webp", data => Ascii(data, 0, "RIFF") && Ascii(data, 8, "WEBP") && Contains(data, "ANIM")),
                     (AnimationEncodeFormat.Apng, ".png", data => data.Length > 8 && data[0] == 0x89 && Ascii(data, 1, "PNG") && Contains(data, "acTL")),
                     (AnimationEncodeFormat.Mp4, ".mp4", data => Ascii(data, 4, "ftyp"))
                 })
        {
            var output = Path.Combine(root, "move" + extension);
            var reports = new List<AnimationExportProgress>();
            var result = service.ExportEncoded(
                request,
                new AnimationEncodeOptions(format, ffmpeg),
                output,
                false,
                default,
                new InlineProgress(reports.Add));
            Check(File.Exists(output) && result.OutputPath == Path.GetFullPath(output), $"{format} export did not write its file.");
            Check(signature(File.ReadAllBytes(output)), $"{format} export wrote an unexpected file format.");
            Check(result.FrameCount > 1 && result.Width > 0 && result.Height > 0, $"{format} export did not report its frames and size.");
            Check(reports.Count > 0 && reports[^1].CompletedFrames == reports[^1].TotalFrames
                && reports[^1].TotalFrames == result.FrameCount * 2,
                $"{format} export progress did not finish at render plus encode frames.");
            Check(!Directory.EnumerateFiles(root, "frame-*.png").Any(), $"{format} export left frames beside the output.");
        }

        // MP4 pads odd sizes to even; the result must report the encoded size.
        var oddOutput = Path.Combine(root, "odd.mp4");
        var odd = service.ExportEncoded(
            request with { Framing = null, Width = 65, Height = 63 },
            new AnimationEncodeOptions(AnimationEncodeFormat.Mp4, ffmpeg),
            oddOutput,
            false);
        var trackSize = Mp4TrackSize(File.ReadAllBytes(oddOutput));
        Check(odd.Width == 66 && odd.Height == 64 && trackSize == (66, 64),
            $"MP4 did not report its padded size: result {odd.Width} x {odd.Height}, track {trackSize}.");

        var existing = Path.Combine(root, "move.gif");
        var before = File.GetLastWriteTimeUtc(existing);
        Expect<IOException>(() => service.ExportEncoded(
            request, new AnimationEncodeOptions(AnimationEncodeFormat.Gif, ffmpeg), existing, false));
        Check(File.GetLastWriteTimeUtc(existing) == before, "A refused overwrite changed the existing output.");
        service.ExportEncoded(request, new AnimationEncodeOptions(AnimationEncodeFormat.Gif, ffmpeg), existing, true);
        Check(Ascii(File.ReadAllBytes(existing), 0, "GIF89a"), "Confirmed overwrite did not replace the output.");

        var canceledOutput = Path.Combine(root, "canceled.mp4");
        using (var canceled = new CancellationTokenSource())
        {
            canceled.Cancel();
            Expect<OperationCanceledException>(() => service.ExportEncoded(
                request, new AnimationEncodeOptions(AnimationEncodeFormat.Mp4, ffmpeg), canceledOutput, false, canceled.Token));
        }
        using (var canceledWhileEncoding = new CancellationTokenSource())
        {
            // Cancel as soon as rendering finishes, before FFmpeg is started.
            var progress = new InlineProgress(value =>
            {
                if (value.CompletedFrames >= value.TotalFrames / 2) canceledWhileEncoding.Cancel();
            });
            Expect<OperationCanceledException>(() => service.ExportEncoded(
                request, new AnimationEncodeOptions(AnimationEncodeFormat.Mp4, ffmpeg), canceledOutput, false,
                canceledWhileEncoding.Token, progress));
        }
        Check(!File.Exists(canceledOutput), "Canceled encoded export left an output file.");

        // A running FFmpeg must stop promptly when canceled: a ten-minute
        // synthetic encode is canceled after one second.
        var ffmpegBefore = Process.GetProcessesByName("ffmpeg").Length;
        using (var running = new CancellationTokenSource(TimeSpan.FromSeconds(1)))
        {
            var stopwatch = Stopwatch.StartNew();
            Expect<OperationCanceledException>(() => FfmpegEncoder.Run(
                ffmpeg,
                ["-hide_banner", "-nostdin", "-loglevel", "error", "-y", "-progress", "pipe:1", "-nostats",
                 "-f", "lavfi", "-i", "testsrc=size=1280x720:rate=30", "-t", "600",
                 "-c:v", "libx264", "-preset", "veryslow", "-f", "null", "-"],
                root,
                null,
                running.Token));
            Check(stopwatch.Elapsed < TimeSpan.FromSeconds(15), "Canceling a running FFmpeg did not stop it promptly.");
        }
        for (var attempt = 0; attempt < 50 && Process.GetProcessesByName("ffmpeg").Length > ffmpegBefore; attempt++)
            Thread.Sleep(100);
        Check(Process.GetProcessesByName("ffmpeg").Length <= ffmpegBefore, "Canceled FFmpeg process was left running.");
        Check(CountWorkDirectories() == tempBefore, "Encoded export left a temporary work directory.");
        Console.WriteLine($"TASK-066: GIF, WebP, APNG, and MP4 encoded export passed with {ffmpeg}.");
    }

    // TASK-066: WPF settings store and Shell export flow.
    public static async Task RunShellAsync(AssetService service, ViewerProjectStore store, string skeletonPath, string temporaryRoot)
    {
        var root = Path.Combine(temporaryRoot, "encoded-shell");
        Directory.CreateDirectory(root);

        var settingsPath = Path.Combine(root, "settings", "settings.json");
        var settings = UserSettingsStore.Load(settingsPath);
        Check(settings.FfmpegPath is null && !File.Exists(settingsPath), "A missing settings file did not load defaults.");
        Check(settings.SetFfmpegPath(@"C:\tools\ffmpeg.exe") && File.Exists(settingsPath), "The FFmpeg path was not saved.");
        Check(UserSettingsStore.Load(settingsPath).FfmpegPath == @"C:\tools\ffmpeg.exe", "The FFmpeg path did not round-trip.");
        File.WriteAllText(settingsPath, "{ not json");
        Check(UserSettingsStore.Load(settingsPath).FfmpegPath is null, "A corrupt settings file was not ignored.");
        settings = UserSettingsStore.Load(settingsPath);

        var ffmpeg = FfmpegLocator.Find(null);
        var output = Path.Combine(root, "shell.gif");
        using var shell = new ShellViewModel(
            WorkspaceState.Empty,
            true,
            store,
            _ => null,
            service,
            chooseAssetPath: () => skeletonPath,
            chooseEncodedExportPath: format => format == "GIF" ? output : null,
            chooseFfmpegPath: () => Path.Combine(root, "missing", "ffmpeg.exe"),
            userSettings: settings);
        shell.SetViewportSize(256, 256);
        await shell.OpenAssetAsync(skeletonPath);
        Check(shell.State == WorkspaceState.Ready, "The encoded export shell did not open its asset.");

        Check(shell.ExportFormat == "PNG sequence" && !shell.IsEncodedExport, "PNG sequence was not the default export format.");
        shell.ExportFormat = "MP4";
        Check(shell.IsEncodedExport && shell.IsVideoExport && shell.ExportSizeSummary.StartsWith("MP4 · ", StringComparison.Ordinal),
            "MP4 did not switch the export settings to an encoded video format.");
        shell.ExportVideoBackground = "12ab34";
        Check(shell.ExportVideoBackground == "#12AB34", "A hex video background was not normalized.");
        shell.ExportVideoBackground = "red";
        Check(shell.ExportVideoBackground == "#12AB34", "An invalid video background was accepted.");

        shell.BrowseFfmpegCommand.Execute(null);
        Check(shell.HasCustomFfmpegPath && UserSettingsStore.Load(settingsPath).FfmpegPath == Path.Combine(root, "missing", "ffmpeg.exe"),
            "Browsing for FFmpeg did not remember the chosen path.");
        Check(shell.ResolvedFfmpegPath == ffmpeg
              && (ffmpeg is null ? shell.FfmpegStatus.StartsWith("Not found: ", StringComparison.Ordinal) : shell.FfmpegStatus == $"PATH: {ffmpeg}"),
            "A missing custom FFmpeg path did not fall back to PATH in the status.");
        shell.UseFfmpegFromPathCommand.Execute(null);
        Check(!shell.HasCustomFfmpegPath && UserSettingsStore.Load(settingsPath).FfmpegPath is null, "Use PATH did not forget the custom path.");
        Check(!shell.IsDirty, "Export format, background, or FFmpeg settings dirtied the project.");

        if (ffmpeg is null) return;
        shell.ExportFormat = "GIF";
        shell.ExportFramesPerSecond = 10;
        shell.ExportCommand.Execute(null);
        for (var attempt = 0; attempt < 600 && (shell.IsExporting || !File.Exists(output)); attempt++)
            await Task.Delay(20);
        Check(!shell.IsExporting && Ascii(File.ReadAllBytes(output), 0, "GIF89a")
              && shell.LastAction == "Exported 11 frames to shell.gif" && shell.LastExportSize.Contains('×'),
            $"Shell GIF export did not finish: {shell.LastAction}");
        Check(shell.ExportCommand.CanExecute(null), "Export was not re-enabled after an encoded export.");
    }

    // Width and height (16.16 fixed point) from the first track header box.
    private static (int Width, int Height) Mp4TrackSize(byte[] data)
    {
        var index = data.AsSpan().IndexOf("tkhd"u8);
        if (index < 0) return (0, 0);
        var body = index + 4;
        var offset = body + (data[body] == 1 ? 88 : 76);
        int Fixed(int at) => (data[at] << 8) | data[at + 1];
        return (Fixed(offset), Fixed(offset + 4));
    }

    private static int CountWorkDirectories() =>
        Directory.EnumerateDirectories(Path.GetTempPath(), "SpineViewerWPF-export-*").Count();

    private static bool Ascii(byte[] data, int offset, string text) =>
        data.Length >= offset + text.Length && Encoding.ASCII.GetString(data, offset, text.Length) == text;

    private static bool Contains(byte[] data, string text) =>
        data.AsSpan().IndexOf(Encoding.ASCII.GetBytes(text)) >= 0;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Expect<T>(Action action) where T : Exception
    {
        try
        {
            action();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private sealed class InlineProgress(Action<AnimationExportProgress> report) : IProgress<AnimationExportProgress>
    {
        public void Report(AnimationExportProgress value) => report(value);
    }
}
