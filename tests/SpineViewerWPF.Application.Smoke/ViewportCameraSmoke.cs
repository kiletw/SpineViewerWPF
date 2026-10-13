using System.Text.RegularExpressions;
using SpineViewerWPF.Application;
using SpineViewerWPF.Core;
using SpineViewerWPF.Wpf;

// TASK-071: one stable scene camera for preview, Screenshot, and fixed-size
// export, plus the viewport guides.
static class ViewportCameraSmoke
{
    public static async Task RunAsync(AssetService service, ViewerProjectStore store, AnimationExportRequest sequenceRequest, string temporaryRoot)
    {
        var root = Path.Combine(temporaryRoot, "viewport-camera");
        Directory.CreateDirectory(root);

        // Placement round trip: a skeleton point rendered through the camera and
        // composited with the placed layer lands at ppu * (scene - view center).
        foreach (var (rotation, flipX, flipY, scale) in new[] { (0d, false, false, 1d), (90d, true, false, 2d), (30d, false, true, 0.5d), (-135d, true, true, 1.5d) })
        {
            var layer = Layer(sequenceRequest, 12, -7, scale, rotation, flipX, flipY);
            const double viewX = 5, viewY = 9, ppu = 1.75;
            var placement = SceneCamera.Place(layer, viewX, viewY, ppu, 200, 120);
            foreach (var (skeletonX, skeletonY) in new[] { (0d, 0d), (10d, 4d), (-6d, 15d) })
            {
                var camera = placement.Camera;
                var rasterX = placement.FrameWidth / 2d + (skeletonX - camera.CenterX) * camera.Scale - placement.FrameWidth / 2d;
                var rasterY = placement.FrameHeight / 2d - (skeletonY - camera.CenterY) * camera.Scale - placement.FrameHeight / 2d;
                var radians = rotation * Math.PI / 180;
                var flippedX = rasterX * (flipX ? -1 : 1);
                var flippedY = rasterY * (flipY ? -1 : 1);
                var canvasX = Math.Cos(radians) * flippedX - Math.Sin(radians) * flippedY;
                var canvasY = Math.Sin(radians) * flippedX + Math.Cos(radians) * flippedY;
                var scene = SceneCamera.ToScene(layer, skeletonX, skeletonY);
                Check(Math.Abs(canvasX - ppu * (scene.X - viewX)) < 1e-3 && Math.Abs(canvasY - ppu * (scene.Y - viewY)) < 1e-3,
                    $"Scene camera round trip failed at rotation {rotation}, flips {flipX}/{flipY}, scale {scale}.");
            }
            Check(placement.Layer.ModelX == 0 && placement.Layer.ModelY == 0 && placement.Layer.ModelScale == 1
                  && placement.Layer.ModelRotation == rotation && placement.Layer.FlipX == flipX,
                "Placement did not fold translation and scale into the camera.");
        }
        Check(SceneCamera.Place(Layer(sequenceRequest, 0, 0, 1, 90, false, false), 0, 0, 1, 200, 120) is { FrameWidth: 120, FrameHeight: 200 }
              && SceneCamera.Place(Layer(sequenceRequest, 0, 0, 1, 45, false, false), 0, 0, 1, 200, 120) is { FrameWidth: 234, FrameHeight: 234 },
            "Rotated placements did not cover the canvas.");

        // Fixed-size export frames the scene 1:1: a large pose is clipped, not shrunk,
        // and a layer offset moves content by exactly that many pixels.
        var large = Path.Combine(root, "large.json");
        File.WriteAllText(large, Regex.Replace(
            File.ReadAllText(sequenceRequest.SkeletonPath), "\"width\":\\s*16,\\s*\"height\":\\s*16", "\"width\": 400, \"height\": 400"));
        File.Copy(sequenceRequest.AtlasPath!, Path.Combine(root, "large.atlas"));
        File.Copy(Path.Combine(Path.GetDirectoryName(sequenceRequest.AtlasPath!)!, "png.png"), Path.Combine(root, "png.png"));
        var largeFrame = Read(service.Export(sequenceRequest with
        {
            SkeletonPath = large,
            OutputDirectory = Path.Combine(root, "large"),
            DurationSeconds = 0
        }).OutputPaths[0]);
        Check(largeFrame.Width == 64 && Enumerable.Range(0, 64 * 64).All(index => largeFrame.Bgra32[index * 4 + 3] > 0),
            "Fixed-size export shrank a pose larger than the canvas instead of framing it 1:1.");
        var centered = Read(service.Export(sequenceRequest with { OutputDirectory = Path.Combine(root, "offset-0"), DurationSeconds = 0 }).OutputPaths[0]);
        var shifted = Read(service.Export(sequenceRequest with
        {
            OutputDirectory = Path.Combine(root, "offset-10"),
            DurationSeconds = 0,
            SceneLayers = [Layer(sequenceRequest, 10, 0, 1, 0, false, false)]
        }).OutputPaths[0]);
        Check(Math.Abs(CentroidX(shifted) - CentroidX(centered) - 10) < 0.6,
            $"A 10 px layer offset moved fixed-size content by {CentroidX(shifted) - CentroidX(centered):0.##} px.");

        // Shell: Fit uses content bounds; Screenshot equals the CPU viewport.
        var capture = Path.Combine(root, "capture.png");
        using var shell = new ShellViewModel(
            WorkspaceState.Empty,
            true,
            store,
            _ => null,
            service,
            chooseAssetPath: () => large,
            chooseScreenshotPath: () => capture,
            userSettings: UserSettingsStore.Load(Path.Combine(root, "settings.json")));
        shell.SetViewportSize(256, 256);
        await shell.OpenAssetAsync(large);
        Check(shell.State == WorkspaceState.Ready, $"The large asset did not open: {shell.State} {shell.DiagnosticsSummary}");
        await Until(() => Math.Abs(shell.ViewportZoom - 1) > 0.01, "Opening a large asset did not fit the view.");
        var expectedZoom = (256 - 28 - 32) / 400d;
        Check(Math.Abs(shell.ViewportZoom - expectedZoom) < 0.01 && Math.Abs(shell.ViewportPanX) < 0.5 && Math.Abs(shell.ViewportPanY) < 0.5,
            $"Fit zoom was {shell.ViewportZoom:0.###}, expected {expectedZoom:0.###}.");
        shell.ZoomViewport(2);
        shell.PanViewport(30, -12);
        shell.FitCommand.Execute(null);
        Check(Math.Abs(shell.ViewportZoom - expectedZoom) < 0.01 && Math.Abs(shell.ViewportPanX) < 0.5, "Fit did not restore the content fit.");

        shell.PanViewport(40, 0);
        await Until(() => shell.PreviewFrame is { } frame && CentroidX(frame) > frame.Width / 2d + 20, "The CPU preview ignored viewport pan.");
        await Task.Delay(100);
        var preview = shell.PreviewFrame!;
        shell.ScreenshotCommand.Execute(null);
        await Until(() => shell.LastAction == "Captured capture.png", "Screenshot did not finish.");
        var captured = Read(capture);
        Check(captured.Width == preview.Width && captured.Height == preview.Height && captured.Bgra32.SequenceEqual(preview.Bgra32),
            "Screenshot did not match the CPU viewport.");

        // Guides: remembered toggles; the export frame needs Fixed size.
        Check(shell.ShowAxes && shell.ShowExportFrame && !shell.IsExportFrameVisible, "Guide defaults were wrong.");
        shell.ExportSizeMode = "Fixed size";
        Check(shell.IsExportFrameVisible, "Fixed size did not show the export frame.");
        shell.ShowAxes = false;
        shell.ShowExportFrame = false;
        var reloaded = UserSettingsStore.Load(Path.Combine(root, "settings.json"));
        Check(!shell.IsExportFrameVisible && !reloaded.ShowAxes && !reloaded.ShowExportFrame && !shell.IsDirty,
            "Guide toggles were not remembered or dirtied the project.");
        Console.WriteLine("TASK-071: scene camera, fit, screenshot, and guides passed.");
    }

    private static SceneLayerDocument Layer(AnimationExportRequest request, double x, double y, double scale, double rotation, bool flipX, bool flipY) =>
        new(request.SkeletonPath, request.AtlasPath, null, "move", "default", x, y, scale, rotation, flipX, flipY, true, 1, 0, 1, false, null);

    private static double CentroidX(RenderedFrame frame)
    {
        double sum = 0, weight = 0;
        for (var index = 0; index < frame.Width * frame.Height; index++)
        {
            var alpha = frame.Bgra32[index * 4 + 3];
            sum += (index % frame.Width) * alpha;
            weight += alpha;
        }
        return weight == 0 ? double.NaN : sum / weight;
    }

    private static RenderedFrame Read(string path)
    {
        var decoder = new System.Windows.Media.Imaging.PngBitmapDecoder(
            new Uri(path), System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,
            System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
        var source = new System.Windows.Media.Imaging.FormatConvertedBitmap(decoder.Frames[0], System.Windows.Media.PixelFormats.Bgra32, null, 0);
        var pixels = new byte[source.PixelWidth * source.PixelHeight * 4];
        source.CopyPixels(pixels, source.PixelWidth * 4, 0);
        return new RenderedFrame(source.PixelWidth, source.PixelHeight, pixels);
    }

    private static async Task Until(Func<bool> condition, string message)
    {
        for (var attempt = 0; attempt < 500 && !condition(); attempt++) await Task.Delay(10);
        Check(condition(), message);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
