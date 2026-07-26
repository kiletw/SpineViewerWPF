using SpineRuntime.V41;
using SpineViewerWPF.Application;
using SpineViewerWPF.Core;
using SpineViewerWPF.Wpf;
using System.Globalization;
using System.Security.Cryptography;
using System.Windows.Threading;

var root = Path.Combine(Path.GetTempPath(), $"SpineViewerWPF-{Guid.NewGuid():N}");
Directory.CreateDirectory(root);

try
{
    var skeleton = Path.Combine(root, "hero.json");
    var project = Path.Combine(root, "hero.spineviewer.json");
    File.WriteAllText(skeleton, "source-must-not-change");

    var expected = new ViewerProjectDocument(
        1, skeleton, Path.Combine(root, "hero.atlas"), "walk", "armor",
        12.5, -4, 1.25, 15, true, false, true, 0.75, 0.6, "Dark");
    var store = new ViewerProjectStore();
    store.Save(project, expected);

    Assert(store.Load(project) == expected, "Project did not round-trip.");
    Assert(File.ReadAllText(skeleton) == "source-must-not-change", "Source skeleton was modified.");
    Assert(File.ReadAllText(project).Contains("\"schemaVersion\": 1"), "Schema version was not serialized.");
    Expect<ArgumentException>(() => store.Save(Path.Combine(root, "unsafe.json"), expected));
    Expect<InvalidDataException>(() => store.Save(project, expected with { TrackAlpha = double.NaN }));

    var uiProject = Path.Combine(root, "ui.spineviewer.json");
    var viewModel = new ShellViewModel(WorkspaceState.Ready, true, store, _ => uiProject);
    viewModel.SelectedAnimation = "walk";
    Assert(viewModel.IsDirty && viewModel.UndoCommand.CanExecute(null), "Edit did not set dirty/undo state.");
    viewModel.UndoCommand.Execute(null);
    Assert(!viewModel.IsDirty && viewModel.SelectedAnimation == "idle", "Undo did not restore the saved state.");
    viewModel.RedoCommand.Execute(null);
    viewModel.SaveCommand.Execute(null);
    Assert(!viewModel.IsDirty && File.Exists(uiProject), "Save did not clear dirty state.");
    Assert(store.Load(uiProject).SelectedAnimation == "walk", "UI edit was not saved.");

    var fixtureDirectory = Path.Combine(Directory.GetCurrentDirectory(), "tests", "fixtures", "v41-minimal");
    var assetService = new AssetService(new SpineV41Adapter());
    var discovered = assetService.Inspect(Path.Combine(fixtureDirectory, "minimal.json"), null, null);
    Assert(discovered.Asset.AtlasPath == Path.GetFullPath(Path.Combine(fixtureDirectory, "minimal.atlas")), "Same-stem atlas discovery failed.");
    var ambiguousDirectory = Path.Combine(root, "ambiguous-atlas");
    Directory.CreateDirectory(ambiguousDirectory);
    File.Copy(Path.Combine(fixtureDirectory, "minimal.json"), Path.Combine(ambiguousDirectory, "scene.json"));
    File.Copy(Path.Combine(fixtureDirectory, "minimal.atlas"), Path.Combine(ambiguousDirectory, "first.atlas"));
    File.Copy(Path.Combine(fixtureDirectory, "minimal.atlas"), Path.Combine(ambiguousDirectory, "second.atlas"));
    Expect<InvalidDataException>(() => assetService.Inspect(Path.Combine(ambiguousDirectory, "scene.json"), null, null));
    var previewPath = Path.Combine(root, "preview.png");
    var capturePath = Path.Combine(root, "capture.png");
    var previewViewModel = new ShellViewModel(
        WorkspaceState.Empty,
        true,
        store,
        _ => Path.Combine(root, "preview.spineviewer.json"),
        assetService,
        createPreviewPath: () => previewPath,
        chooseScreenshotPath: () => capturePath);
    await previewViewModel.OpenAssetAsync(Path.Combine(fixtureDirectory, "minimal.json"));
    Assert(previewViewModel.State == WorkspaceState.Ready, "PPM fixture did not reach Ready.");
    Assert(previewViewModel.PreviewImagePath == previewPath && File.Exists(previewPath), "Static preview was not rendered.");
    Assert(
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(previewPath)))
            == "7178BBFA4315C36332AB5C4743A413FE6A7CD165D75C907BBC34D88DB846301E",
        "WPF preview did not match the deterministic render baseline.");
    Assert(
        new PreviewImageConverter().Convert(previewPath, typeof(object), null, CultureInfo.InvariantCulture) is not null,
        "WPF could not load the rendered preview.");
    Assert(previewViewModel.DiagnosticsSummary == "No diagnostics.", "A clean asset did not report an empty diagnostics summary.");
    previewViewModel.DiagnosticsCommand.Execute(null);
    Assert(previewViewModel.IsDiagnosticsVisible, "Diagnostics command did not open the summary.");
    previewViewModel.DiagnosticsCommand.Execute(null);
    Assert(!previewViewModel.IsDiagnosticsVisible, "Diagnostics command did not close the summary.");
    var modelXBeforeFit = previewViewModel.ModelX;
    previewViewModel.ZoomViewport(2);
    previewViewModel.PanViewport(24, -12);
    Assert(previewViewModel.ViewportZoom == 2 && previewViewModel.ViewportPanX == 24 && previewViewModel.ViewportPanY == -12, "Viewport navigation did not update.");
    previewViewModel.FitCommand.Execute(null);
    Assert(previewViewModel.ViewportZoom == 1 && previewViewModel.ViewportPanX == 0 && previewViewModel.ViewportPanY == 0, "Fit did not reset the viewport.");
    Assert(previewViewModel.ModelX == modelXBeforeFit && !previewViewModel.IsDirty, "Viewport fit changed editable model state.");
    previewViewModel.ScreenshotCommand.Execute(null);
    Assert(File.Exists(capturePath), "Screenshot command did not create a PNG.");
    Assert(
        SHA256.HashData(File.ReadAllBytes(capturePath)).SequenceEqual(SHA256.HashData(File.ReadAllBytes(previewPath))),
        "Screenshot did not match the displayed preview.");
    Assert(!previewViewModel.IsDirty, "Screenshot changed the viewer project state.");
    await previewViewModel.OpenAssetAsync(Path.Combine(root, "missing.json"));
    Assert(previewViewModel.PreviewImagePath == previewPath && File.Exists(previewPath), "Failed replacement discarded the prior preview.");
    Assert(previewViewModel.Diagnostics.Count == 1 && previewViewModel.DiagnosticsSummary.Contains("OPEN_FAILED", StringComparison.Ordinal), "Failed open did not produce an actionable diagnostic.");
    previewViewModel.Dispose();
    Assert(!File.Exists(previewPath), "Disposed preview file was not deleted.");

    var pngSkeleton = Path.Combine(root, "png.json");
    var pngAtlas = Path.Combine(root, "png.atlas");
    var pngTexture = Path.Combine(root, "png.png");
    File.Copy(Path.Combine(fixtureDirectory, "minimal.json"), pngSkeleton);
    File.WriteAllText(
        pngAtlas,
        File.ReadAllText(Path.Combine(fixtureDirectory, "minimal.atlas"))
            .Replace("minimal.ppm", "png.png")
            .Replace("size: 2, 2", "size: 1, 1")
            .Replace("bounds: 0, 0, 2, 2", "bounds: 0, 0, 1, 1"));
    var validPng = Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR4nGP4z8DwHwAFAAH/iZk9HQAAAABJRU5ErkJggg==");
    File.WriteAllBytes(pngTexture, validPng);

    var pngService = assetService;
    var pngInspect = pngService.Inspect(pngSkeleton, null, null);
    Assert(pngInspect.Asset.Textures.SequenceEqual([Path.GetFullPath(pngTexture)]), "PNG texture path was not inspected.");
    var pngVariants = new[]
    {
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAAAAABX3VL4AAAADklEQVR4nGPk4mISEQEAAKgAQMS3sfYAAAAASUVORK5CYII=",
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAFklEQVR4nGP8z8DA+J+BiZHh/3+G/wAeHAUBO0YLBwAAAABJRU5ErkJggg==",
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAMAAABFaP0WAAAABlBMVEX/AAAA/wDSh+9xAAAAAnRSTlP/AOW3MEoAAAAOSURBVHicY2ZgZGH8DwABKQEJTiJzbQAAAABJRU5ErkJggg==",
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAQAAADYv8WvAAAAEklEQVR4nGPh+s/VyCLiyHUAAA3LArwHXQLTAAAAAElFTkSuQmCC",
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAGUlEQVR4nGP8z8Dwn/E/QyMLI8N/RyDnAAA8NwaF+/WO+wAAAABJRU5ErkJggg==",
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAABklEQVR4nGP4z8CGMy/JAAAAB0lEQVTwHwAFAAH/3G9/TQAAAABJRU5ErkJggg==",
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAYAAABytg0kAAAAF0lEQVR4nGP4z8DwHwgbGIC0w////xkAQBgHuub96GQAAAAASUVORK5CYII="
    };
    for (var i = 0; i < pngVariants.Length; i++)
    {
        File.WriteAllBytes(pngTexture, Convert.FromBase64String(pngVariants[i]));
        pngService.Render(
            pngSkeleton, pngAtlas, null, "move", 0.5f, 64, 64,
            Path.Combine(root, $"png-variant-{i}.png"), true, false, ["default"]);
    }
    Assert(
        SHA256.HashData(File.ReadAllBytes(Path.Combine(root, "png-variant-4.png")))
            .SequenceEqual(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, "png-variant-6.png")))),
        "PNG scanline filters changed decoded pixels.");
    File.WriteAllBytes(pngTexture, validPng);

    var directPngPreview = Path.Combine(root, "png-preview-direct.png");
    pngService.Render(pngSkeleton, pngAtlas, null, "move", 0.5f, 64, 64, directPngPreview, true, false, ["default"]);

    var sequenceRequest = new AnimationExportRequest(
        pngSkeleton,
        pngAtlas,
        null,
        "move",
        1,
        10,
        64,
        64,
        Path.Combine(root, "sequence-a"),
        "move.png",
        false,
        false,
        ["default"]);
    var sequence = pngService.Export(sequenceRequest);
    Assert(sequence.FrameCount == 11 && sequence.OutputPaths.Count == 11, "PNG sequence frame count was not deterministic.");
    Assert(sequence.OutputPaths[0].EndsWith("move-0000.png", StringComparison.Ordinal), "PNG sequence naming was not stable.");
    var repeatedSequence = pngService.Export(sequenceRequest with { OutputDirectory = Path.Combine(root, "sequence-b") });
    Assert(sequence.OutputPaths.Zip(repeatedSequence.OutputPaths).All(pair =>
        SHA256.HashData(File.ReadAllBytes(pair.First)).SequenceEqual(SHA256.HashData(File.ReadAllBytes(pair.Second)))),
        "PNG sequence output was not deterministic.");
    Expect<IOException>(() => pngService.Export(sequenceRequest));
    using (var canceledExport = new CancellationTokenSource())
    {
        canceledExport.Cancel();
        Expect<OperationCanceledException>(() => pngService.Export(
            sequenceRequest with { OutputDirectory = Path.Combine(root, "sequence-canceled") },
            canceledExport.Token));
    }
    Assert(!Directory.EnumerateFiles(Path.Combine(root, "sequence-canceled"), "*.png").Any(), "Canceled export left partial frames.");

    var realProject = Path.Combine(root, "real.spineviewer.json");
    var pngPreviewPath = Path.Combine(root, "png-preview.png");
    var exportPrefix = Path.Combine(root, "exported", "move.png");
    using var realViewModel = new ShellViewModel(
        WorkspaceState.Empty,
        true,
        store,
        _ => realProject,
        pngService,
        createPreviewPath: () => pngPreviewPath,
        chooseExportPath: () => exportPrefix);
    await realViewModel.OpenAssetAsync(pngSkeleton);
    Assert(
        realViewModel.State == WorkspaceState.Ready,
        $"PNG fixture did not reach Ready: {realViewModel.State}: {realViewModel.StateDetail}");
    Assert(realViewModel.PreviewImagePath == pngPreviewPath && File.Exists(pngPreviewPath), "PNG preview was not rendered.");
    Assert(
        new PreviewImageConverter().Convert(pngPreviewPath, typeof(object), null, CultureInfo.InvariantCulture) is not null,
        "WPF could not load the PNG-backed preview.");
    Assert(realViewModel.FilteredAnimations.SequenceEqual(["move"]), "Real animation metadata was not mapped.");
    Assert(realViewModel.Skins.SequenceEqual(["default"]), "Real skin metadata was not mapped.");
    Assert(realViewModel.RuntimeLabel == "Runtime 4.1" && realViewModel.Duration == 1, "Runtime or duration was not mapped.");
    Assert(Math.Abs(realViewModel.ExportFramesPerSecond - 30) < 0.001, "Export FPS default was not 30.");
    realViewModel.ExportFramesPerSecond = 10;
    Assert(Math.Abs(realViewModel.ExportFramesPerSecond - 10) < 0.001 && !realViewModel.IsDirty, "Export FPS did not change without dirtying the project.");
    Assert(
        SHA256.HashData(File.ReadAllBytes(pngPreviewPath)).SequenceEqual(SHA256.HashData(File.ReadAllBytes(directPngPreview))),
        "PNG-backed preview was not deterministic.");
    realViewModel.IsPlaying = true;
    var playbackBefore = SHA256.HashData(File.ReadAllBytes(pngPreviewPath));
    var frame = new DispatcherFrame();
    var stopFrame = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
    stopFrame.Tick += (_, _) =>
    {
        stopFrame.Stop();
        frame.Continue = false;
    };
    stopFrame.Start();
    Dispatcher.PushFrame(frame);
    await Task.Delay(100);
    Assert(realViewModel.Position > 0, "Playback timer did not advance the real preview.");
    Assert(realViewModel.PlaybackTimeLabel.StartsWith("0:00.", StringComparison.Ordinal), "Playback time label was not updated.");
    Assert(!playbackBefore.SequenceEqual(SHA256.HashData(File.ReadAllBytes(pngPreviewPath))), "Playback did not publish a new preview frame.");
    realViewModel.IsPlaying = false;
    realViewModel.ExportCommand.Execute(null);
    for (var attempt = 0; attempt < 100 && realViewModel.IsExporting; attempt++)
        await Task.Delay(10);
    Assert(!realViewModel.IsExporting && File.Exists(Path.Combine(root, "exported", "move-0000.png")), "WPF Export command did not finish a PNG sequence.");
    Assert(realViewModel.LastAction == "Exported 11 frames", "WPF Export command did not use the custom FPS.");
    realViewModel.ModelX = 12;
    realViewModel.SaveCommand.Execute(null);
    var realDocument = store.Load(realProject);
    Assert(realDocument.SkeletonPath == Path.GetFullPath(pngSkeleton), "Real skeleton path was not saved.");

    realViewModel.ModelY = 4;
    await realViewModel.OpenAssetAsync(Path.Combine(root, "missing.json"));
    Assert(realViewModel.State == WorkspaceState.Ready && realViewModel.IsDirty, "Dirty replacement was not canceled.");
    realViewModel.UndoCommand.Execute(null);

    await realViewModel.OpenAssetAsync(Path.Combine(root, "missing.json"));
    Assert(realViewModel.State == WorkspaceState.Failed, "Missing input did not reach Failed.");
    Assert(realViewModel.SkeletonFileName == "png.json", "Failed replacement discarded the prior document.");

    var corruptPng = validPng.ToArray();
    corruptPng[42] ^= 1;
    File.WriteAllBytes(pngTexture, corruptPng);
    pngService.Inspect(pngSkeleton, pngAtlas, null);
    var corruptRender = Expect<Exception>(() =>
        pngService.Render(pngSkeleton, pngAtlas, null, "move", 0.5f, 64, 64, Path.Combine(root, "corrupt.png"), true, false, []));
    Assert(corruptRender.InnerException is InvalidDataException, "Corrupt PNG render did not preserve its validation error.");
    var renderFailureViewModel = new ShellViewModel(
        WorkspaceState.Empty,
        true,
        store,
        _ => Path.Combine(root, "render-failure.spineviewer.json"),
        pngService,
        createPreviewPath: () => Path.Combine(root, "render-failure.png"));
    await renderFailureViewModel.OpenAssetAsync(pngSkeleton);
    Assert(renderFailureViewModel.State == WorkspaceState.RendererUnavailable && renderFailureViewModel.DiagnosticsSummary.Contains("RENDER_FAILED", StringComparison.Ordinal), "Failed render did not produce an actionable diagnostic.");
    renderFailureViewModel.Dispose();

    File.WriteAllBytes(pngTexture, [137, 80, 78, 71]);
    var malformedPng = Expect<Exception>(() => pngService.Inspect(pngSkeleton, pngAtlas, null));
    Assert(malformedPng.InnerException is InvalidDataException, "Malformed PNG did not preserve its validation error.");
    var zeroWidthPng = validPng.ToArray();
    Array.Clear(zeroWidthPng, 16, 4);
    File.WriteAllBytes(pngTexture, zeroWidthPng);
    var zeroWidth = Expect<Exception>(() => pngService.Inspect(pngSkeleton, pngAtlas, null));
    Assert(zeroWidth.InnerException is InvalidDataException, "Zero-width PNG did not preserve its validation error.");

    var unsupportedSkeleton = Path.Combine(root, "unsupported.json");
    File.WriteAllText(unsupportedSkeleton, "{}");
    File.WriteAllText(Path.ChangeExtension(unsupportedSkeleton, ".atlas"), "");
    var unsupportedViewModel = new ShellViewModel(
        WorkspaceState.Empty,
        true,
        store,
        _ => Path.Combine(root, "unsupported.spineviewer.json"),
        new AssetService(new UnsupportedRuntimeAdapter()));
    await unsupportedViewModel.OpenAssetAsync(unsupportedSkeleton);
    Assert(unsupportedViewModel.State == WorkspaceState.Unsupported, "Unsupported input did not reach Unsupported.");

    File.WriteAllText(project, File.ReadAllText(project).Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2"));
    Expect<InvalidDataException>(() => store.Load(project));

    Console.WriteLine("Viewer project, PNG render preview, edit history, validation, and source isolation passed.");
}
finally
{
    Directory.Delete(root, true);
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static T Expect<T>(Action action) where T : Exception
{
    try
    {
        action();
    }
    catch (T exception)
    {
        return exception;
    }

    throw new InvalidOperationException($"Expected {typeof(T).Name}.");
}

sealed class UnsupportedRuntimeAdapter : IRuntimeAdapter
{
    public string RuntimeLine => "4.1";
    public InspectResult Inspect(string skeletonPath, string atlasPath, bool overridden, CancellationToken cancellationToken) =>
        throw new NotSupportedException("Unsupported fixture.");
    public void Render(RenderRequest request, CancellationToken cancellationToken) => throw new NotSupportedException();
}
