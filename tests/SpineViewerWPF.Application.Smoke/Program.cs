using SpineRuntime.V40;
using SpineRuntime.V41;
using SpineViewerWPF.Application;
using SpineViewerWPF.Core;
using SpineViewerWPF.Wpf;
using System.Globalization;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

try
{
    var fragmentShader = typeof(GpuViewport).GetField(
        "FragmentShaderSource",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?.GetRawConstantValue() as string
        ?? throw new InvalidOperationException("GPU fragment shader policy was not found.");
    Assert(fragmentShader.Contains("sampled.rgb * sampled.a * uTint.rgb * uTint.a", StringComparison.Ordinal),
        "Straight-alpha GPU input was not converted to premultiplied output.");
    var colorBlendFactors = typeof(GpuViewport).GetMethod(
        "ColorBlendFactors",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
        ?? throw new InvalidOperationException("GPU Screen blend policy was not found.");
    var screenBlend = (System.Runtime.CompilerServices.ITuple)(colorBlendFactors.Invoke(null, [PreviewBlendMode.Screen])
        ?? throw new InvalidOperationException("GPU Screen blend policy returned no factors."));
    Assert(screenBlend[0]?.ToString() == "One" && screenBlend[1]?.ToString() == "OneMinusSrcColor",
        "GPU Screen blending did not consume premultiplied source color.");

    var dispatcher = Dispatcher.CurrentDispatcher;
    SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
    var smokeFrame = new DispatcherFrame();
    var smokeTask = RunSmokeAsync();
    _ = smokeTask.ContinueWith(
        _ => dispatcher.BeginInvoke(new Action(() => smokeFrame.Continue = false)),
        TaskScheduler.Default);
    Dispatcher.PushFrame(smokeFrame);
    smokeTask.GetAwaiter().GetResult();
}
catch (Exception exception)
{
    Console.Error.WriteLine(exception);
    Environment.ExitCode = 1;
}

static async Task RunSmokeAsync()
{
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
    var slotProject = expected with
    {
        SceneLayers =
        [
            new SceneLayerDocument(
                skeleton,
                expected.AtlasPath,
                "4.1",
                "walk",
                "armor",
                0,
                0,
                1,
                0,
                false,
                false,
                true,
                1,
                0,
                1,
                false,
                [new SlotDisplayDocument("body", false, 0.35, "armor")])
        ]
    };
    var slotProjectPath = Path.Combine(root, "slots.spineviewer.json");
    store.Save(slotProjectPath, slotProject);
    var loadedSlotProject = store.Load(slotProjectPath);
    var loadedSlots = loadedSlotProject.SceneLayers?.FirstOrDefault()?.Slots;
    Assert(
        loadedSlots is { Count: 1 } && loadedSlots[0] == new SlotDisplayDocument("body", false, 0.35, "armor"),
        "Slot display settings did not round-trip.");
    var legacySlotProjectPath = Path.Combine(root, "slots-without-attachment.spineviewer.json");
    var legacySlotJson = System.Text.RegularExpressions.Regex.Replace(
        File.ReadAllText(slotProjectPath),
        ",\\s*\"attachmentName\"\\s*:\\s*\"armor\"",
        "");
    File.WriteAllText(legacySlotProjectPath, legacySlotJson);
    Assert(
        store.Load(legacySlotProjectPath).SceneLayers?[0].Slots?[0].AttachmentName is null,
        "Older slot JSON without attachmentName did not retain animation/setup behavior.");
    Assert(File.ReadAllText(skeleton) == "source-must-not-change", "Source skeleton was modified.");
    Assert(File.ReadAllText(project).Contains("\"schemaVersion\": 1"), "Schema version was not serialized.");
    Expect<ArgumentException>(() => store.Save(Path.Combine(root, "unsafe.json"), expected));
    Expect<InvalidDataException>(() => store.Save(project, expected with { TrackAlpha = double.NaN }));

    var uiProject = Path.Combine(root, "ui.spineviewer.json");
    var viewModel = new ShellViewModel(WorkspaceState.Ready, true, store, _ => uiProject);
    viewModel.SetViewportSize(1600, 900);
    Assert(
        viewModel.PreviewPixelWidth == 1536 && viewModel.PreviewPixelHeight == 864,
        "Viewport render size did not preserve the physical-pixel aspect within its bounds.");
    viewModel.SelectedAnimation = "walk";
    Assert(viewModel.IsDirty && viewModel.UndoCommand.CanExecute(null), "Edit did not set dirty/undo state.");
    viewModel.UndoCommand.Execute(null);
    Assert(!viewModel.IsDirty && viewModel.SelectedAnimation == "idle", "Undo did not restore the saved state.");
    viewModel.RedoCommand.Execute(null);
    viewModel.SaveCommand.Execute(null);
    Assert(!viewModel.IsDirty && File.Exists(uiProject), "Save did not clear dirty state.");
    Assert(store.Load(uiProject).SelectedAnimation == "walk", "UI edit was not saved.");

    var fixtureDirectory = Path.Combine(Directory.GetCurrentDirectory(), "tests", "fixtures", "v41-minimal");
    var assetService = new AssetService(new IRuntimeAdapter[]
    {
        new SpineRuntime.V21_08.LegacyRuntimeAdapter(), new SpineRuntime.V21_25.LegacyRuntimeAdapter(),
        new SpineRuntime.V31_07.LegacyRuntimeAdapter(), new SpineRuntime.V32.LegacyRuntimeAdapter(),
        new SpineRuntime.V34_02.LegacyRuntimeAdapter(), new SpineRuntime.V35_51.LegacyRuntimeAdapter(),
        new SpineRuntime.V36_32.LegacyRuntimeAdapter(), new SpineRuntime.V36_39.LegacyRuntimeAdapter(),
        new SpineRuntime.V36_53.LegacyRuntimeAdapter(), new SpineRuntime.V37_94.LegacyRuntimeAdapter(),
        new SpineRuntime.V38_95.LegacyRuntimeAdapter(), new SpineRuntime.V40_31.LegacyRuntimeAdapter(),
        new SpineV40Adapter(), new SpineV41Adapter(), new SpineRuntime.V42.Adapter(),
        new SpineRuntime.V43.Adapter()
    });
    var discovered = assetService.Inspect(Path.Combine(fixtureDirectory, "minimal.json"), null, null);
    Assert(discovered.Asset.AtlasPath == Path.GetFullPath(Path.Combine(fixtureDirectory, "minimal.atlas")), "Same-stem atlas discovery failed.");
    using (var sceneSession = assetService.OpenRenderSession(Path.Combine(fixtureDirectory, "minimal.json"), null, "4.1").Session)
    {
        var scene = sceneSession.RenderScene("move", 0.5f, false, ["default"]);
        Assert(scene.DrawCommands.Any(command => !string.IsNullOrWhiteSpace(command.SlotName)), "Preview scene did not retain slot names.");
    }
    var pmaFrame = Path.Combine(root, "v41-pma.png");
    assetService.Render(Path.Combine(fixtureDirectory, "minimal.json"), null, "4.1", "move", 0.5f, 64, 64, pmaFrame, true, true, ["default"]);
    Assert(File.Exists(pmaFrame), "PMA render did not produce a frame.");
    var fullTrackAlphaFrame = Path.Combine(root, "v41-track-alpha-full.png");
    assetService.Render(
        Path.Combine(fixtureDirectory, "minimal.json"),
        null,
        "4.1",
        "move",
        0.5f,
        64,
        64,
        fullTrackAlphaFrame,
        true,
        false,
        ["default"]);
    var zeroTrackAlphaFrame = Path.Combine(root, "v41-track-alpha-zero.png");
    assetService.Render(
        Path.Combine(fixtureDirectory, "minimal.json"),
        null,
        "4.1",
        "move",
        0.5f,
        64,
        64,
        zeroTrackAlphaFrame,
        true,
        false,
        ["default"],
        trackAlpha: 0);
    Assert(
        !File.ReadAllBytes(fullTrackAlphaFrame).SequenceEqual(File.ReadAllBytes(zeroTrackAlphaFrame)),
        "Track alpha did not change Runtime animation mixing.");
    Expect<ArgumentOutOfRangeException>(() => assetService.Render(
        Path.Combine(fixtureDirectory, "minimal.json"),
        null,
        "4.1",
        "move",
        0.5f,
        64,
        64,
        Path.Combine(root, "invalid-track-alpha.png"),
        true,
        false,
        ["default"],
        trackAlpha: -0.1f));
    var multipageDirectory = Path.Combine(Directory.GetCurrentDirectory(), "tests", "fixtures", "v41-multipage");
    var multipageSkeleton = Path.Combine(multipageDirectory, "multi.json");
    var multipageInspection = assetService.Inspect(multipageSkeleton, null, "4.1");
    Assert(multipageInspection.Asset.Textures.Count == 2, "Multi-page atlas did not expose both texture pages.");
    var multipageFirst = Path.Combine(root, "multipage-a.png");
    var multipageSecond = Path.Combine(root, "multipage-b.png");
    foreach (var output in new[] { multipageFirst, multipageSecond })
        assetService.Render(multipageSkeleton, null, "4.1", "move", 0, 64, 64, output, true, false, ["default"]);
    var multipageHash = SHA256.HashData(File.ReadAllBytes(multipageFirst));
    Assert(multipageHash.SequenceEqual(SHA256.HashData(File.ReadAllBytes(multipageSecond))), "Multi-page render was not deterministic.");
    Assert(Convert.ToHexString(multipageHash) == "89115E7CC5AA6B1B594C14C9E1F7F74B5C30644F926B1D032420DC7B753ED6AA", "Multi-page render did not match its baseline.");

    var redSlots = new[]
    {
        new SlotDisplayDocument("left", true, 1),
        new SlotDisplayDocument("right", false, 1)
    };
    var blueSlots = new[]
    {
        new SlotDisplayDocument("left", false, 1),
        new SlotDisplayDocument("right", true, 1)
    };
    var redLayer = new SceneLayerDocument(
        multipageSkeleton,
        multipageInspection.Asset.AtlasPath,
        "4.1",
        "move",
        "default",
        16,
        0,
        1,
        0,
        false,
        false,
        true,
        1,
        1,
        1,
        false,
        redSlots);
    var blueLayer = redLayer with
    {
        ModelX = -16,
        ZIndex = 0,
        Slots = blueSlots
    };
    RenderedFrame redFrame;
    RenderedFrame blueFrame;
    using (var redSession = assetService.OpenRenderSession(multipageSkeleton, null, "4.1").Session)
        redFrame = redSession.RenderFrame("move", 0, 64, 64, false, ["default"], slots: redSlots);
    using (var blueSession = assetService.OpenRenderSession(multipageSkeleton, null, "4.1").Session)
        blueFrame = blueSession.RenderFrame("move", 0, 64, 64, false, ["default"], slots: blueSlots);

    var sceneCapture = SceneFrameCompositor.Compose(
        [new SceneFrameLayer(redFrame, redLayer), new SceneFrameLayer(blueFrame, blueLayer)],
        64,
        64);
    var captureColors = MeasureRedBlue(sceneCapture);
    Assert(
        captureColors.RedCount > 0
        && captureColors.BlueCount > 0
        && captureColors.BlueX < captureColors.RedX,
        "The screenshot-equivalent scene composite did not contain translated blue and red layers.");

    var opaqueRed = new RenderedFrame(1, 1, [0, 0, 255, 255]);
    var opaqueBlue = new RenderedFrame(1, 1, [255, 0, 0, 255]);
    var identityRedLayer = redLayer with
    {
        ModelX = 0,
        ModelY = 0,
        ModelScale = 1,
        ModelRotation = 0,
        FlipX = false,
        FlipY = false,
        IsVisible = true,
        Opacity = 1,
        ZIndex = 0
    };
    var halfBlueLayer = identityRedLayer with { Opacity = 0.5, ZIndex = 1 };
    var blueOverRed = SceneFrameCompositor.Compose(
        [new SceneFrameLayer(opaqueRed, identityRedLayer), new SceneFrameLayer(opaqueBlue, halfBlueLayer)],
        1,
        1);
    Assert(
        blueOverRed.Bgra32.SequenceEqual(new byte[] { 128, 0, 128, 255 }),
        "Half-opacity blue over opaque red did not use deterministic source-over composition.");
    var redOverBlue = SceneFrameCompositor.Compose(
        [
            new SceneFrameLayer(opaqueRed, identityRedLayer with { ZIndex = 1 }),
            new SceneFrameLayer(opaqueBlue, halfBlueLayer with { ZIndex = 0 })
        ],
        1,
        1);
    Assert(
        redOverBlue.Bgra32.SequenceEqual(new byte[] { 0, 0, 255, 255 })
        && !redOverBlue.Bgra32.SequenceEqual(blueOverRed.Bgra32),
        "Changing layer Z order did not put opaque red above half-opacity blue.");

    var sceneSequenceRequest = new AnimationExportRequest(
        multipageSkeleton,
        multipageInspection.Asset.AtlasPath,
        "4.1",
        "move",
        0,
        30,
        64,
        64,
        Path.Combine(root, "scene-sequence-a"),
        "scene.png",
        false,
        false,
        ["default"],
        SceneLayers: [redLayer, blueLayer]);
    var oversizedCompositeDirectory = Path.Combine(root, "scene-sequence-oversized");
    Expect<ArgumentOutOfRangeException>(() => assetService.Export(sceneSequenceRequest with
    {
        Width = 4096,
        Height = 4096,
        OutputDirectory = oversizedCompositeDirectory,
        SceneLayers = [redLayer, blueLayer, redLayer, blueLayer]
    }));
    Assert(!Directory.Exists(oversizedCompositeDirectory), "Oversized composite export created its output directory.");
    var sceneSequence = assetService.Export(sceneSequenceRequest);
    Assert(sceneSequence.FrameCount == 1, "The zero-duration scene export did not produce exactly one frame.");
    var exportedSceneFrame = ReadPngFrame(sceneSequence.OutputPaths.Single());
    var exportedColors = MeasureRedBlue(exportedSceneFrame);
    Assert(
        exportedColors.RedCount > 0
        && exportedColors.BlueCount > 0
        && exportedColors.BlueX < exportedColors.RedX,
        "The PNG scene sequence did not contain translated blue and red layers.");
    Assert(
        FrameHash(exportedSceneFrame).SequenceEqual(FrameHash(sceneCapture)),
        "Screenshot-equivalent and PNG-sequence scene composition pixels differed.");

    var repeatedSceneSequence = assetService.Export(sceneSequenceRequest with
    {
        OutputDirectory = Path.Combine(root, "scene-sequence-b")
    });
    Assert(
        File.ReadAllBytes(sceneSequence.OutputPaths.Single())
            .SequenceEqual(File.ReadAllBytes(repeatedSceneSequence.OutputPaths.Single())),
        "Repeated multi-layer PNG sequence output was not deterministic.");

    var singleLayerSequence = assetService.Export(sceneSequenceRequest with
    {
        OutputDirectory = Path.Combine(root, "scene-sequence-single"),
        SceneLayers = null
    });
    Assert(
        SHA256.HashData(File.ReadAllBytes(singleLayerSequence.OutputPaths.Single())).SequenceEqual(multipageHash),
        "Adding scene composition changed the existing single-layer PNG baseline.");

    var hiddenLayers = new[]
    {
        redLayer with { IsVisible = false },
        blueLayer with { IsVisible = false }
    };
    var hiddenCapture = SceneFrameCompositor.Compose(
        [new SceneFrameLayer(redFrame, hiddenLayers[0]), new SceneFrameLayer(blueFrame, hiddenLayers[1])],
        64,
        64);
    Assert(hiddenCapture.Bgra32.All(value => value == 0), "An all-hidden scene composite was not transparent.");
    var hiddenSceneSequence = assetService.Export(sceneSequenceRequest with
    {
        OutputDirectory = Path.Combine(root, "scene-sequence-hidden"),
        SceneLayers = hiddenLayers
    });
    Assert(
        ReadPngFrame(hiddenSceneSequence.OutputPaths.Single()).Bgra32.All(value => value == 0),
        "An all-hidden PNG scene sequence was not transparent.");

    var v40Directory = Path.Combine(Directory.GetCurrentDirectory(), "tests", "fixtures", "v40-minimal");
    var v40Skeleton = Path.Combine(v40Directory, "minimal.json");
    var v40Inspection = assetService.Inspect(v40Skeleton, null, null);
    Assert(v40Inspection.Runtime.SelectedLine == "4.0" && v40Inspection.Runtime.DetectedExportVersion is { } version && version.StartsWith("4.0.64", StringComparison.Ordinal), "4.0.64 Runtime selection failed.");
    var v40First = Path.Combine(root, "v40-a.png");
    var v40Second = Path.Combine(root, "v40-b.png");
    foreach (var output in new[] { v40First, v40Second })
        assetService.Render(v40Skeleton, null, "4.0.64", "move", 0.5f, 64, 64, output, true, false, ["default"]);
    Assert(File.ReadAllBytes(v40First).SequenceEqual(File.ReadAllBytes(v40Second)), "4.0.64 render was not deterministic.");
    Expect<NotSupportedException>(() => assetService.Inspect(v40Skeleton, null, "4.1"));
    var v42Directory = Path.Combine(Directory.GetCurrentDirectory(), "tests", "fixtures", "v42-minimal");
    var v42Skeleton = Path.Combine(v42Directory, "minimal.json");
    var v42AutoInspection = assetService.Inspect(v42Skeleton, null, null);
    Assert(
        v42AutoInspection.Runtime.SelectedLine == "4.2"
        && v42AutoInspection.Runtime.DetectedExportVersion is { } v42Version
        && v42Version.StartsWith("4.2", StringComparison.Ordinal),
        "4.2 automatic Runtime selection failed.");
    var v42ExplicitInspection = assetService.Inspect(v42Skeleton, null, "4.2");
    Assert(v42ExplicitInspection.Runtime.SelectedLine == "4.2" && v42ExplicitInspection.Runtime.Overridden, "4.2 explicit Runtime selection failed.");
    var v42Slot = v42ExplicitInspection.Slots?.Single(slot => slot.Name == "sprite");
    Assert(
        v42Slot is not null
        && v42Slot.SetupAttachment == "square"
        && v42Slot.Attachments.SequenceEqual(["square", "tall"]),
        "4.2 slot attachment metadata was incomplete.");
    Expect<NotSupportedException>(() => assetService.Inspect(v42Skeleton, null, "4.1"));
    var v42First = Path.Combine(root, "v42-a.png");
    var v42Second = Path.Combine(root, "v42-b.png");
    foreach (var output in new[] { v42First, v42Second })
        assetService.Render(v42Skeleton, null, "4.2", "move", 0.5f, 64, 64, output, true, false, ["default"]);
    Assert(File.ReadAllBytes(v42First).SequenceEqual(File.ReadAllBytes(v42Second)), "4.2 render was not deterministic.");
    using (var v42Session = assetService.OpenRenderSession(v42Skeleton, null, "4.2").Session)
    {
        var v42Scene = v42Session.RenderScene("move", 0.5f, false, ["default"]);
        var v42AverageX = v42Scene.DrawCommands.SelectMany(command => command.Vertices).Average(vertex => vertex.X);
        Assert(
            v42Scene.BoundsWidth > 0
            && v42Scene.BoundsHeight > 0
            && v42Scene.DrawCommands.Any(command =>
                command.Alpha > 0
                && command.Vertices.Length >= 3
                && command.Indices.Length >= 3
                && command.Texture.Rgba32.Length == command.Texture.Width * command.Texture.Height * 4),
            "4.2 preview scene did not contain visible textured triangle geometry.");
        Assert(Math.Abs(v42AverageX - 4) > 0.1, "4.2 preview scene did not apply fixed-step physics replay.");
        var tallSetting = new SlotDisplayDocument("sprite", true, 1, "tall");
        var v42TallScene = v42Session.RenderScene("move", 0.5f, false, ["default"], slots: [tallSetting]);
        Assert(
            Math.Abs(v42TallScene.BoundsWidth - v42Scene.BoundsWidth) > 0.1,
            "4.2 attachment selection did not change Runtime-neutral preview geometry.");
        var v42HalfOpacityScene = v42Session.RenderScene(
            "move", 0.5f, false, ["default"],
            slots: [new SlotDisplayDocument("sprite", true, 0.5)]);
        Assert(
            Math.Abs(v42HalfOpacityScene.DrawCommands.Single().Alpha - v42Scene.DrawCommands.Single().Alpha * 0.5f) < 0.001,
            "Runtime-neutral preview scene did not apply slot opacity exactly once.");
        var v42DefaultFrame = v42Session.RenderFrame("move", 0.5f, 64, 64, false, ["default"]);
        var v42TallFrame = v42Session.RenderFrame("move", 0.5f, 64, 64, false, ["default"], slots: [tallSetting]);
        Assert(!FrameHash(v42DefaultFrame).SequenceEqual(FrameHash(v42TallFrame)), "4.2 attachment selection did not change CPU output.");
        var v42InvalidFrame = v42Session.RenderFrame(
            "move", 0.5f, 64, 64, false, ["default"],
            slots: [new SlotDisplayDocument("sprite", true, 1, "missing")]);
        Assert(FrameHash(v42DefaultFrame).SequenceEqual(FrameHash(v42InvalidFrame)), "Unknown attachment did not fall back to the animated/setup pose.");
        _ = v42Session.RenderScene("move", 0.1f, false, ["default"], slots: [tallSetting]);
        var v42RepeatedScene = v42Session.RenderScene("move", 0.5f, false, ["default"]);
        Assert(
            v42Scene.DrawCommands.Count == v42RepeatedScene.DrawCommands.Count
            && v42Scene.DrawCommands.Zip(v42RepeatedScene.DrawCommands).All(pair =>
                pair.First.Vertices.SequenceEqual(pair.Second.Vertices)
                && pair.First.Indices.SequenceEqual(pair.Second.Indices)),
            "4.2 reused session leaked physics state between absolute frame requests.");
    }
    var v43Directory = Path.Combine(Directory.GetCurrentDirectory(), "tests", "fixtures", "v43-minimal");
    var v43Skeleton = Path.Combine(v43Directory, "minimal.json");
    var v43AutoInspection = assetService.Inspect(v43Skeleton, null, null);
    Assert(
        v43AutoInspection.Runtime.SelectedLine == "4.3"
        && v43AutoInspection.Runtime.DetectedExportVersion is { } v43Version
        && v43Version.StartsWith("4.3", StringComparison.Ordinal),
        "4.3 automatic Runtime selection failed.");
    var v43ExplicitInspection = assetService.Inspect(v43Skeleton, null, "4.3");
    Assert(v43ExplicitInspection.Runtime.SelectedLine == "4.3" && v43ExplicitInspection.Runtime.Overridden, "4.3 explicit Runtime selection failed.");
    var v43Slot = v43ExplicitInspection.Slots?.Single(slot => slot.Name == "sprite");
    Assert(
        v43Slot is not null
        && v43Slot.SetupAttachment == "square"
        && v43Slot.Attachments.SequenceEqual(["square", "tall"]),
        "4.3 slot attachment metadata was incomplete.");
    Expect<NotSupportedException>(() => assetService.Inspect(v43Skeleton, null, "4.2"));
    var v43First = Path.Combine(root, "v43-a.png");
    var v43Second = Path.Combine(root, "v43-b.png");
    foreach (var output in new[] { v43First, v43Second })
        assetService.Render(v43Skeleton, null, "4.3", "move", 0.5f, 64, 64, output, true, false, ["default"]);
    Assert(File.ReadAllBytes(v43First).SequenceEqual(File.ReadAllBytes(v43Second)), "4.3 render was not deterministic.");
    Assert(
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(v43First))) == "7178BBFA4315C36332AB5C4743A413FE6A7CD165D75C907BBC34D88DB846301E",
        "4.3 minimal render did not match its baseline.");
    using (var v43Session = assetService.OpenRenderSession(v43Skeleton, null, "4.3").Session)
    {
        var v43Scene = v43Session.RenderScene("move", 0.5f, false, ["default"]);
        var v43AverageX = v43Scene.DrawCommands.SelectMany(command => command.Vertices).Average(vertex => vertex.X);
        Assert(
            v43Scene.BoundsWidth > 0
            && v43Scene.BoundsHeight > 0
            && v43Scene.DrawCommands.Any(command =>
                command.Alpha > 0
                && command.Vertices.Length >= 3
                && command.Indices.Length >= 3
                && command.Texture.Rgba32.Length == command.Texture.Width * command.Texture.Height * 4),
            "4.3 preview scene did not contain visible textured triangle geometry.");
        Assert(
            Math.Abs(v43AverageX - 4) > 0.1,
            $"4.3 preview scene did not apply fixed-step physics replay (average X: {v43AverageX}).");
        var tallSetting = new SlotDisplayDocument("sprite", true, 1, "tall");
        var v43TallScene = v43Session.RenderScene("move", 0.5f, false, ["default"], slots: [tallSetting]);
        Assert(
            Math.Abs(v43TallScene.BoundsWidth - v43Scene.BoundsWidth) > 0.1,
            "4.3 attachment selection did not change Runtime-neutral preview geometry.");
        var v43HalfOpacityScene = v43Session.RenderScene(
            "move", 0.5f, false, ["default"],
            slots: [new SlotDisplayDocument("sprite", true, 0.5)]);
        Assert(
            Math.Abs(v43HalfOpacityScene.DrawCommands.Single().Alpha - v43Scene.DrawCommands.Single().Alpha * 0.5f) < 0.001,
            "4.3 Runtime-neutral preview scene did not apply slot opacity exactly once.");
        var v43HiddenScene = v43Session.RenderScene(
            "move", 0.5f, false, ["default"],
            slots: [new SlotDisplayDocument("sprite", false, 1)]);
        Assert(v43HiddenScene.DrawCommands.Count == 0, "4.3 slot visibility did not hide preview geometry.");
        var v43DefaultFrame = v43Session.RenderFrame("move", 0.5f, 64, 64, false, ["default"]);
        var v43TallFrame = v43Session.RenderFrame("move", 0.5f, 64, 64, false, ["default"], slots: [tallSetting]);
        Assert(!FrameHash(v43DefaultFrame).SequenceEqual(FrameHash(v43TallFrame)), "4.3 attachment selection did not change CPU output.");
        var v43ZeroTrackScene = v43Session.RenderScene("move", 0.5f, false, ["default"], trackAlpha: 0);
        Assert(
            !v43Scene.DrawCommands.Single().Vertices.SequenceEqual(v43ZeroTrackScene.DrawCommands.Single().Vertices),
            "4.3 Track 0 Alpha did not change Runtime-neutral preview geometry.");
        var v43InvalidFrame = v43Session.RenderFrame(
            "move", 0.5f, 64, 64, false, ["default"],
            slots: [new SlotDisplayDocument("sprite", true, 1, "missing")]);
        Assert(FrameHash(v43DefaultFrame).SequenceEqual(FrameHash(v43InvalidFrame)), "4.3 unknown attachment did not fall back to the animated/setup pose.");
        _ = v43Session.RenderScene("move", 0.1f, false, ["default"], slots: [tallSetting]);
        var v43RepeatedScene = v43Session.RenderScene("move", 0.5f, false, ["default"]);
        Assert(
            v43Scene.DrawCommands.Count == v43RepeatedScene.DrawCommands.Count
            && v43Scene.DrawCommands.Zip(v43RepeatedScene.DrawCommands).All(pair =>
                pair.First.Vertices.SequenceEqual(pair.Second.Vertices)
                && pair.First.Indices.SequenceEqual(pair.Second.Indices)),
            "4.3 reused session leaked physics or slot state between absolute frame requests.");
    }
    var historicalFixtures = new[]
    {
        (Directory: "v21_08-minimal", Runtime: "2.1.08"), (Directory: "v21_25-minimal", Runtime: "2.1.25"),
        (Directory: "v31_07-minimal", Runtime: "3.1.07"), (Directory: "v32-minimal", Runtime: "3.2.xx"),
        (Directory: "v34_02-minimal", Runtime: "3.4.02"), (Directory: "v35_51-minimal", Runtime: "3.5.51"),
        (Directory: "v36_32-minimal", Runtime: "3.6.32"), (Directory: "v36_39-minimal", Runtime: "3.6.39"),
        (Directory: "v36_53-minimal", Runtime: "3.6.53"), (Directory: "v37_94-minimal", Runtime: "3.7.94"),
        (Directory: "v38_95-minimal", Runtime: "3.8.95"), (Directory: "v40_31-minimal", Runtime: "4.0.31")
    };
    foreach (var fixture in historicalFixtures)
    {
        var directory = Path.Combine(Directory.GetCurrentDirectory(), "tests", "fixtures", fixture.Directory);
        var skeletonPath = Path.Combine(directory, "minimal.json");
        var inspection = assetService.Inspect(skeletonPath, null, fixture.Runtime);
        Assert(inspection.Runtime.SelectedLine == fixture.Runtime, $"{fixture.Runtime} Runtime selection failed.");
        var autoInspection = assetService.Inspect(skeletonPath, null, null);
        Assert(autoInspection.Runtime.SelectedLine == fixture.Runtime, $"{fixture.Runtime} automatic Runtime selection failed.");
        var first = Path.Combine(root, $"{fixture.Directory}-a.png");
        var second = Path.Combine(root, $"{fixture.Directory}-b.png");
        assetService.Render(skeletonPath, null, fixture.Runtime, "move", 0.5f, 64, 64, first, true, false, ["default"]);
        assetService.Render(skeletonPath, null, fixture.Runtime, "move", 0.5f, 64, 64, second, true, false, ["default"]);
        Assert(File.ReadAllBytes(first).SequenceEqual(File.ReadAllBytes(second)), $"{fixture.Runtime} render was not deterministic.");
    }
    var ambiguousDirectory = Path.Combine(root, "ambiguous-atlas");
    Directory.CreateDirectory(ambiguousDirectory);
    File.Copy(Path.Combine(fixtureDirectory, "minimal.json"), Path.Combine(ambiguousDirectory, "scene.json"));
    File.Copy(Path.Combine(fixtureDirectory, "minimal.ppm"), Path.Combine(ambiguousDirectory, "minimal.ppm"));
    File.Copy(Path.Combine(fixtureDirectory, "minimal.atlas"), Path.Combine(ambiguousDirectory, "first.atlas"));
    File.Copy(Path.Combine(fixtureDirectory, "minimal.atlas"), Path.Combine(ambiguousDirectory, "second.atlas"));
    Expect<InvalidDataException>(() => assetService.Inspect(Path.Combine(ambiguousDirectory, "scene.json"), null, null));
    using (var manualAtlasViewModel = new ShellViewModel(
               WorkspaceState.Empty,
               true,
               store,
               assetService: assetService,
               chooseAtlasPath: () => Path.Combine(ambiguousDirectory, "first.atlas")))
    {
        await manualAtlasViewModel.OpenAssetAsync(Path.Combine(ambiguousDirectory, "scene.json"));
        Assert(manualAtlasViewModel.State == WorkspaceState.Ready, "Manual atlas selection did not recover an ambiguous asset.");
    }
    using (var gpuViewModel = new ShellViewModel(WorkspaceState.Empty, true, store, assetService: assetService))
    {
        gpuViewModel.SetGpuPreviewAvailable(true);
        await gpuViewModel.OpenAssetAsync(v42Skeleton);
        for (var attempt = 0; attempt < 50 && gpuViewModel.SceneLayers[0].PreviewScene is null; attempt++)
            await Task.Delay(10);
        Assert(gpuViewModel.SceneLayers[0].PreviewScene is not null, "GPU preview was not queued after the initial load.");
        Assert(gpuViewModel.SceneLayers[0].Slots.Count > 0, "GPU preview did not expose slot display controls.");
        var slot = gpuViewModel.SceneLayers[0].Slots.Single(item => item.Name == "sprite");
        Assert(slot.AttachmentOptions.SequenceEqual(["", "square", "tall"]), "WPF slot attachment options were incomplete.");
        slot.SelectedAttachmentKey = "tall";
        Assert(slot.SelectedAttachmentKey == "tall", "WPF slot attachment selection did not update.");
        Assert(gpuViewModel.IsDirty && gpuViewModel.UndoCommand.CanExecute(null), "Attachment selection did not dirty the Viewer project.");
        gpuViewModel.UndoCommand.Execute(null);
        Assert(slot.SelectedAttachmentKey == "", "Undo did not restore animated/setup attachment selection.");
        slot.IsVisible = false;
        Assert(!slot.IsVisible, "WPF slot visibility toggle did not update.");
        Assert(gpuViewModel.IsDirty && gpuViewModel.UndoCommand.CanExecute(null), "Slot visibility did not dirty the Viewer project.");
        for (var attempt = 0; attempt < 50
             && gpuViewModel.SceneLayers[0].PreviewScene?.DrawCommands.Any(command => command.SlotName == "sprite") != false;
             attempt++)
            await Task.Delay(10);
        Assert(
            gpuViewModel.SceneLayers[0].PreviewScene?.DrawCommands.All(command => command.SlotName != "sprite") == true,
            "Slot visibility did not remain applied after the GPU preview rerendered.");
        gpuViewModel.UndoCommand.Execute(null);
        Assert(slot.IsVisible, "Undo did not restore slot visibility.");
        gpuViewModel.RedoCommand.Execute(null);
        Assert(!slot.IsVisible, "Redo did not reapply slot visibility.");
    }
    var capturePath = Path.Combine(root, "capture.png");
    var previewViewModel = new ShellViewModel(
        WorkspaceState.Empty,
        true,
        store,
        _ => Path.Combine(root, "preview.spineviewer.json"),
        assetService,
        chooseScreenshotPath: () => capturePath);
    previewViewModel.SetViewportSize(256, 256);
    await previewViewModel.OpenAssetAsync(Path.Combine(fixtureDirectory, "minimal.json"));
    Assert(previewViewModel.State == WorkspaceState.Ready, "PPM fixture did not reach Ready.");
    var retainedPreview = previewViewModel.PreviewFrame
        ?? throw new InvalidOperationException("Static preview was not rendered.");
    var retainedLayer = previewViewModel.SelectedSceneLayer
        ?? throw new InvalidOperationException("Static preview did not retain its scene layer.");
    Assert(
        retainedPreview.Width == 256
        && retainedPreview.Height == 256
        && retainedPreview.Bgra32.Length == 256 * 256 * 4,
        "Static preview dimensions or BGRA byte length were invalid.");
    Assert(
        new PreviewFrameConverter().Convert([retainedPreview!, "RGBA"], typeof(object), null, CultureInfo.InvariantCulture) is not null,
        "WPF could not load the rendered preview.");
    var alphaBitmap = new PreviewFrameConverter().Convert(
        [retainedPreview!, "Alpha"], typeof(object), null, CultureInfo.InvariantCulture) as BitmapSource
        ?? throw new InvalidOperationException("WPF could not inspect the rendered alpha channel.");
    var alphaPixel = new byte[4];
    alphaBitmap.CopyPixels(new Int32Rect(0, 0, 1, 1), alphaPixel, 4, 0);
    Assert(
        alphaPixel[0] == retainedPreview.Bgra32[3]
        && alphaPixel[1] == retainedPreview.Bgra32[3]
        && alphaPixel[2] == retainedPreview.Bgra32[3]
        && alphaPixel[3] == 255,
        "Alpha channel conversion did not produce an opaque grayscale inspection image.");
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
    for (var attempt = 0; attempt < 200
         && (!File.Exists(capturePath) || new FileInfo(capturePath).Length <= 8);
         attempt++)
        await Task.Delay(10);
    Assert(File.Exists(capturePath), "Screenshot command did not create a PNG.");
    Assert(new FileInfo(capturePath).Length > 8, "Screenshot command created an empty PNG.");
    Assert(!previewViewModel.IsDirty, "Screenshot changed the viewer project state.");
    for (var attempt = 0; attempt < 200
         && !previewViewModel.ScreenshotCommand.CanExecute(null);
         attempt++)
        await Task.Delay(10);
    Assert(previewViewModel.ScreenshotCommand.CanExecute(null), "Screenshot command did not finish cleanly.");
    await previewViewModel.OpenAssetAsync(Path.Combine(root, "missing.json"));
    Assert(
        ReferenceEquals(previewViewModel.SelectedSceneLayer, retainedLayer)
        && previewViewModel.PreviewFrame is not null,
        "Failed replacement discarded the prior preview.");
    Assert(previewViewModel.Diagnostics.Count == 1 && previewViewModel.DiagnosticsSummary.Contains("OPEN_FAILED", StringComparison.Ordinal), "Failed open did not produce an actionable diagnostic.");
    previewViewModel.Dispose();

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
    var declaredSizeDirectory = Path.Combine(root, "declared-atlas-size");
    Directory.CreateDirectory(declaredSizeDirectory);
    var declaredSizeSkeleton = Path.Combine(declaredSizeDirectory, "minimal.json");
    var declaredSizeAtlas = Path.Combine(declaredSizeDirectory, "minimal.atlas");
    File.Copy(Path.Combine(fixtureDirectory, "minimal.json"), declaredSizeSkeleton);
    File.Copy(Path.Combine(fixtureDirectory, "minimal.ppm"), Path.Combine(declaredSizeDirectory, "minimal.ppm"));
    File.WriteAllText(
        declaredSizeAtlas,
        File.ReadAllText(Path.Combine(fixtureDirectory, "minimal.atlas"))
            .Replace("size: 2, 2", "size: 1, 1")
            .Replace("bounds: 0, 0, 2, 2", "bounds: 0, 0, 1, 1"));
    using (var declaredSizeSession = pngService.OpenRenderSession(declaredSizeSkeleton, declaredSizeAtlas, "4.1").Session)
    {
        var declaredSizeFrame = declaredSizeSession.RenderFrame("move", 0.5f, 64, 64, false, ["default"], linearFiltering: false);
        var colors = new HashSet<(byte B, byte G, byte R)>();
        for (var offset = 0; offset < declaredSizeFrame.Bgra32.Length; offset += 4)
            if (declaredSizeFrame.Bgra32[offset + 3] > 0)
                colors.Add((declaredSizeFrame.Bgra32[offset], declaredSizeFrame.Bgra32[offset + 1], declaredSizeFrame.Bgra32[offset + 2]));
        Assert(colors.Count == 4, "Texture loading overwrote the atlas page size and shifted its UV coordinates.");
    }
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

    var sessionFirst = Path.Combine(root, "session-a.png");
    var sessionSecond = Path.Combine(root, "session-b.png");
    var openedSession = pngService.OpenRenderSession(pngSkeleton, pngAtlas, null);
    using (openedSession.Session)
    {
        openedSession.Session.Render("move", 0.5f, 64, 64, sessionFirst, true, false, ["default"]);
        openedSession.Session.Render("move", 0.5f, 64, 64, sessionSecond, true, false, ["default"]);
        Expect<ArgumentOutOfRangeException>(() => openedSession.Session.Render(
            "move", 0.5f, 4097, 4096, Path.Combine(root, "oversized-session.png"), true, false, ["default"]));
    }
    Assert(
        File.ReadAllBytes(sessionFirst).SequenceEqual(File.ReadAllBytes(sessionSecond)),
        "Reusable render session changed deterministic output.");

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
    var oversizedSequenceDirectory = Path.Combine(root, "sequence-oversized");
    Expect<ArgumentOutOfRangeException>(() => pngService.Export(sequenceRequest with
    {
        Width = 4097,
        Height = 4096,
        OutputDirectory = oversizedSequenceDirectory
    }));
    Assert(!Directory.Exists(oversizedSequenceDirectory), "Oversized sequence export created its output directory.");
    var sequence = pngService.Export(sequenceRequest);
    Assert(sequence.FrameCount == 11 && sequence.OutputPaths.Count == 11, "PNG sequence frame count was not deterministic.");
    Assert(sequence.OutputPaths[0].EndsWith("move-0000.png", StringComparison.Ordinal), "PNG sequence naming was not stable.");
    var repeatedSequence = pngService.Export(sequenceRequest with { OutputDirectory = Path.Combine(root, "sequence-b") });
    Assert(sequence.OutputPaths.Zip(repeatedSequence.OutputPaths).All(pair =>
        SHA256.HashData(File.ReadAllBytes(pair.First)).SequenceEqual(SHA256.HashData(File.ReadAllBytes(pair.Second)))),
        "PNG sequence output was not deterministic.");
    var hiddenSequence = pngService.Export(sequenceRequest with
    {
        OutputDirectory = Path.Combine(root, "sequence-hidden"),
        Slots = [new SlotDisplayDocument("sprite", false, 1)]
    });
    Assert(
        !SHA256.HashData(File.ReadAllBytes(sequence.OutputPaths[0]))
            .SequenceEqual(SHA256.HashData(File.ReadAllBytes(hiddenSequence.OutputPaths[0]))),
        "PNG sequence export ignored slot visibility.");
    Expect<IOException>(() => pngService.Export(sequenceRequest));
    using (var canceledExport = new CancellationTokenSource())
    {
        canceledExport.Cancel();
        Expect<OperationCanceledException>(() => pngService.Export(
            sequenceRequest with { OutputDirectory = Path.Combine(root, "sequence-canceled") },
            canceledExport.Token));
    }
    Assert(!Directory.EnumerateFiles(Path.Combine(root, "sequence-canceled"), "*.png").Any(), "Canceled export left partial frames.");

    var sceneOutputs = pngService.RenderScene(
        [
            new SceneLayerRenderRequest(pngSkeleton, pngAtlas, null, "move", 0.5f, Path.Combine(root, "scene-a.png"), true, false, ["default"]),
            new SceneLayerRenderRequest(pngSkeleton, pngAtlas, null, "move", 0.5f, Path.Combine(root, "scene-b.png"), true, false, ["default"])
        ],
        64,
        64);
    Assert(sceneOutputs.Count == 2 && sceneOutputs.All(File.Exists), "Scene layer rendering did not produce both layer previews.");
    Assert(
        SHA256.HashData(File.ReadAllBytes(sceneOutputs[0])).SequenceEqual(SHA256.HashData(File.ReadAllBytes(sceneOutputs[1]))),
        "Repeated scene layers did not use the deterministic renderer path.");
    Expect<ArgumentOutOfRangeException>(() => pngService.RenderScene(
        [new SceneLayerRenderRequest(pngSkeleton, pngAtlas, null, "move", 0.5f, Path.Combine(root, "oversized-scene.png"), true, false, ["default"])],
        4097,
        4096));

    var realProject = Path.Combine(root, "real.spineviewer.json");
    var exportPrefix = Path.Combine(root, "exported", "move.png");
    IReadOnlyList<string> nextAssetPaths = [pngSkeleton];
    using var realViewModel = new ShellViewModel(
        WorkspaceState.Empty,
        true,
        store,
        _ => realProject,
        pngService,
        chooseAssetPath: () => pngSkeleton,
        chooseAssetPaths: () => nextAssetPaths,
        chooseExportPath: () => exportPrefix);
    realViewModel.SetViewportSize(256, 256);
    await realViewModel.OpenAssetAsync(pngSkeleton);
    Assert(
        realViewModel.State == WorkspaceState.Ready,
        $"PNG fixture did not reach Ready: {realViewModel.State}: {realViewModel.StateDetail}");
    Assert(realViewModel.PreviewFrame is not null, "PNG preview was not rendered.");
    Assert(
        new PreviewFrameConverter().Convert([realViewModel.PreviewFrame!, "RGBA"], typeof(object), null, CultureInfo.InvariantCulture) is not null,
        "WPF could not load the PNG-backed preview.");
    Assert(realViewModel.FilteredAnimations.SequenceEqual(["move"]), "Real animation metadata was not mapped.");
    Assert(realViewModel.Skins.SequenceEqual(["default"]), "Real skin metadata was not mapped.");
    Assert(realViewModel.RuntimeLabel == "Runtime 4.1" && realViewModel.Duration == 1, "Runtime or duration was not mapped.");
    Assert(realViewModel.SelectedAnimation == "move" && realViewModel.CanPlay && !realViewModel.IsDirty,
        "Opening a real asset did not preserve its clean, playable animation selection.");
    Assert(realViewModel.SceneLayers.Count == 1, "Opening an asset did not create the initial scene layer.");
    Assert(Math.Abs(realViewModel.PreviewFramesPerSecond - 30) < 0.001, "Preview FPS default was not 30.");
    Assert(Math.Abs(realViewModel.ExportFramesPerSecond - 30) < 0.001, "Export FPS default was not 30.");
    realViewModel.ExportFramesPerSecond = 10;
    Assert(Math.Abs(realViewModel.ExportFramesPerSecond - 10) < 0.001 && !realViewModel.IsDirty, "Export FPS did not change without dirtying the project.");
    var undoBeforePreviewFps = realViewModel.UndoCommand.CanExecute(null);
    realViewModel.PreviewFramesPerSecond = 60;
    Assert(
        Math.Abs(realViewModel.PreviewFramesPerSecond - 60) < 0.001
        && Math.Abs(realViewModel.ExportFramesPerSecond - 10) < 0.001,
        "Preview FPS was not independently customizable from Export FPS.");
    realViewModel.PreviewFramesPerSecond = 0;
    Assert(Math.Abs(realViewModel.PreviewFramesPerSecond - 1) < 0.001, "Preview FPS did not enforce its lower bound.");
    realViewModel.PreviewFramesPerSecond = 241;
    Assert(Math.Abs(realViewModel.PreviewFramesPerSecond - 240) < 0.001, "Preview FPS did not enforce its upper bound.");
    Assert(
        !realViewModel.IsDirty && realViewModel.UndoCommand.CanExecute(null) == undoBeforePreviewFps,
        "Preview FPS changed project dirty state or Undo history.");
    using (var directSession = pngService.OpenRenderSession(pngSkeleton, pngAtlas, null).Session)
    {
        var directScene = directSession.RenderScene("move", 0.5f, false, ["default"]);
        Assert(
            directScene.BoundsWidth > 0
            && directScene.BoundsHeight > 0
            && directScene.DrawCommands.Count > 0
            && directScene.DrawCommands.All(command =>
                command.Vertices.Length >= 3
                && command.Indices.Length >= 3
                && command.Texture.Rgba32.Length == command.Texture.Width * command.Texture.Height * 4),
            "Runtime-neutral GPU scene data was incomplete.");
        var directFrame = directSession.RenderFrame(
            "move", 0.5f, realViewModel.PreviewPixelWidth, realViewModel.PreviewPixelHeight, false, ["default"]);
        Assert(FrameHash(realViewModel.PreviewFrame).SequenceEqual(FrameHash(directFrame)), "PNG-backed preview was not deterministic.");
        var slotName = directScene.DrawCommands.First(command => !string.IsNullOrWhiteSpace(command.SlotName)).SlotName;
        var hiddenSlotFrame = directSession.RenderFrame(
            "move", 0.5f, realViewModel.PreviewPixelWidth, realViewModel.PreviewPixelHeight, false, ["default"],
            slots: [new SlotDisplayDocument(slotName, false, 1)]);
        Assert(!FrameHash(hiddenSlotFrame).SequenceEqual(FrameHash(directFrame)), "Slot visibility did not affect the CPU preview.");
        _ = directSession.RenderFrame(
            "move", 0.1f, realViewModel.PreviewPixelWidth, realViewModel.PreviewPixelHeight, false, [],
            trackAlpha: 0.35f);
        var restoredFrame = directSession.RenderFrame(
            "move", 0.5f, realViewModel.PreviewPixelWidth, realViewModel.PreviewPixelHeight, false, ["default"]);
        Assert(
            FrameHash(restoredFrame).SequenceEqual(FrameHash(directFrame)),
            "A reused Runtime session leaked time, alpha, or slot state between absolute frame requests.");
        var wideFrame = directSession.RenderFrame("move", 0.5f, 384, 256, false, ["default"]);
        Assert(
            wideFrame.Width == 384
            && wideFrame.Height == 256
            && wideFrame.Bgra32.Length == 384 * 256 * 4
            && wideFrame.Bgra32[3] == 0,
            "Wide interactive frames did not retain their viewport dimensions and framing gutter.");
    }
    realViewModel.IsPlaying = false;
    realViewModel.IsPlaying = true;
    var playbackBefore = FrameHash(realViewModel.PreviewFrame);
    var playbackDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(2);
    var frame = new DispatcherFrame();
    var stopFrame = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
    stopFrame.Tick += (_, _) =>
    {
        if (realViewModel.Position > 0 || DateTime.UtcNow >= playbackDeadline)
        {
            stopFrame.Stop();
            frame.Continue = false;
        }
    };
    stopFrame.Start();
    Dispatcher.PushFrame(frame);
    await Task.Delay(100);
    Assert(realViewModel.Position > 0, "Playback timer did not advance the real preview.");
    Assert(realViewModel.PlaybackTimeLabel.StartsWith("0:00.", StringComparison.Ordinal), "Playback time label was not updated.");
    for (var attempt = 0; attempt < 250 && playbackBefore.SequenceEqual(FrameHash(realViewModel.PreviewFrame)); attempt++)
        await Task.Delay(20);
    Assert(!playbackBefore.SequenceEqual(FrameHash(realViewModel.PreviewFrame)), "Playback did not publish a new preview frame.");
    for (var attempt = 0; attempt < 150 && !realViewModel.PreviewPerformanceLabel.Contains(" FPS ", StringComparison.Ordinal); attempt++)
        await Task.Delay(20);
    Assert(
        realViewModel.PreviewPerformanceLabel.Contains(" ms work ", StringComparison.Ordinal)
        && realViewModel.PreviewPerformanceLabel.Contains(" coalesced", StringComparison.Ordinal),
        "Playback performance metrics were not published.");
    realViewModel.IsPlaying = false;
    Assert(realViewModel.PreviewPerformanceLabel == "Paused", "Playback performance metrics did not reset when paused.");
    var previousPreviewFps = realViewModel.PreviewFramesPerSecond;
    realViewModel.Position = 0;
    realViewModel.Loop = false;
    realViewModel.PreviewFramesPerSecond = 1;
    realViewModel.IsPlaying = true;
    var lowFpsDeadline = DateTime.UtcNow + TimeSpan.FromSeconds(2.5);
    var lowFpsFrame = new DispatcherFrame();
    var lowFpsStop = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(10) };
    lowFpsStop.Tick += (_, _) =>
    {
        if (realViewModel.Position >= 0.75 || DateTime.UtcNow >= lowFpsDeadline)
        {
            lowFpsStop.Stop();
            lowFpsFrame.Continue = false;
        }
    };
    lowFpsStop.Start();
    Dispatcher.PushFrame(lowFpsFrame);
    Assert(
        realViewModel.Position >= 0.75,
        "A scheduled 1 FPS tick was throttled below real playback cadence.");
    realViewModel.IsPlaying = false;
    realViewModel.Loop = true;
    realViewModel.PreviewFramesPerSecond = previousPreviewFps;
    realViewModel.Position = 0;
    realViewModel.SetGpuPreviewAvailable(true);
    for (var attempt = 0; attempt < 100 && !realViewModel.HasGpuPreview; attempt++)
        await Task.Delay(10);
    Assert(
        realViewModel.HasGpuPreview
        && realViewModel.SceneLayers.All(layer => layer.PreviewScene?.DrawCommands.Count > 0),
        "GPU playback did not publish Runtime-neutral scene data.");
    var cpuFrameBeforeGpuPlayback = FrameHash(realViewModel.PreviewFrame);
    var gpuPositionBefore = realViewModel.Position;
    realViewModel.IsPlaying = true;
    for (var attempt = 0; attempt < 100 && realViewModel.Position == gpuPositionBefore; attempt++)
        await Task.Delay(10);
    await Task.Delay(50);
    Assert(
        cpuFrameBeforeGpuPlayback.SequenceEqual(FrameHash(realViewModel.PreviewFrame)),
        "GPU playback unexpectedly replaced the CPU preview frame.");
    realViewModel.IsPlaying = false;
    realViewModel.SetGpuPreviewAvailable(false);
    realViewModel.ExportCommand.Execute(null);
    for (var attempt = 0; attempt < 100 && realViewModel.IsExporting; attempt++)
        await Task.Delay(10);
    Assert(!realViewModel.IsExporting && File.Exists(Path.Combine(root, "exported", "move-0000.png")), "WPF Export command did not finish a PNG sequence.");
    Assert(realViewModel.LastAction == "Exported 11 frames", "WPF Export command did not use the custom FPS.");
    realViewModel.BackgroundMode = "Transparent";
    Assert(realViewModel.UndoCommand.CanExecute(null), "Property edit did not create Undo history before layer import.");
    nextAssetPaths = [pngSkeleton, Path.Combine(fixtureDirectory, "minimal.json")];
    realViewModel.AddLayerCommand.Execute(null);
    for (var attempt = 0; attempt < 200 && realViewModel.SceneLayers.Count < 3; attempt++)
        await Task.Delay(10);
    Assert(realViewModel.SceneLayers.Count == 3, "Batch layer import did not create both additional scene layers.");
    Assert(realViewModel.SceneLayers.All(layer => layer.PreviewFrame is not null), "Scene layer previews were not rendered.");
    Assert(
        realViewModel.IsDirty && !realViewModel.UndoCommand.CanExecute(null),
        "Adding a scene layer did not stay dirty while invalidating incompatible Undo history.");
    Assert(realViewModel.SceneLayers.All(layer => layer.Animations.Contains("move") && layer.Skins.Contains("default")), "Scene layers did not retain independent animation and skin metadata.");
    realViewModel.BackgroundMode = "Solid";
    Assert(realViewModel.UndoCommand.CanExecute(null), "Property edit did not create Undo history before auto layout.");
    realViewModel.AutoLayoutCommand.Execute(null);
    Assert(realViewModel.SceneLayers.Select(layer => (Math.Round(layer.ModelX), Math.Round(layer.ModelY))).Distinct().Count() == 3, "Auto layout did not separate scene layers.");
    Assert(!realViewModel.UndoCommand.CanExecute(null), "Auto layout retained incompatible Undo history.");
    realViewModel.SelectedSceneLayer!.Opacity = 0.5;
    var layerAlphaBefore = FrameHash(realViewModel.SelectedSceneLayer.PreviewFrame);
    realViewModel.SelectedSceneLayer.TrackAlpha = 0.35;
    for (var attempt = 0; attempt < 100
         && layerAlphaBefore.SequenceEqual(FrameHash(realViewModel.SelectedSceneLayer.PreviewFrame));
         attempt++)
        await Task.Delay(10);
    Assert(
        !layerAlphaBefore.SequenceEqual(FrameHash(realViewModel.SelectedSceneLayer.PreviewFrame)),
        "Changing layer Track Alpha did not refresh its rendered preview.");
    realViewModel.SelectedSceneLayer.Pma = true;
    Assert(
        realViewModel.SelectedSceneLayer.Opacity == 0.5
        && realViewModel.SelectedSceneLayer.TrackAlpha == 0.35
        && realViewModel.SelectedSceneLayer.Pma
        && realViewModel.SceneLayers[0].Opacity == 1
        && realViewModel.SceneLayers[0].TrackAlpha == 1
        && !realViewModel.SceneLayers[0].Pma,
        "Scene layer alpha properties were not independent.");
    realViewModel.MoveLayerUpCommand.Execute(null);
    Assert(
        realViewModel.SceneLayers[1].ZIndex == 1
        && realViewModel.SceneLayers[1].Opacity == 0.5
        && realViewModel.SceneLayers[1].TrackAlpha == 0.35
        && realViewModel.SceneLayers[1].Pma,
        "Scene layer reorder did not preserve alpha state.");
    Assert(!realViewModel.UndoCommand.CanExecute(null), "Scene layer reorder retained incompatible Undo history.");
    realViewModel.ModelX = 12;
    realViewModel.SceneLayers[0].ModelX = 37;
    realViewModel.SaveCommand.Execute(null);
    var realDocument = store.Load(realProject);
    Assert(realViewModel.SceneLayers[0].ModelX == 37, "Save mutated the primary layer transform.");
    Assert(realDocument.SkeletonPath == Path.GetFullPath(pngSkeleton), "Real skeleton path was not saved.");
    Assert(realDocument.SceneLayers?.Count == 3, "Saved Viewer project did not preserve scene layers.");
    Assert(
        realDocument.ModelX == 37 && realDocument.SceneLayers?[0].ModelX == 37,
        "Saved Viewer project did not derive legacy transform fields from the current primary layer.");
    Assert(realDocument.SceneLayers?[1].Opacity == 0.5, "Saved Viewer project did not preserve per-layer opacity.");
    Assert(
        realDocument.SceneLayers?[1].TrackAlpha == 0.35 && realDocument.SceneLayers[1].Pma == true,
        "Saved Viewer project did not preserve per-layer Track Alpha and PMA.");
    Expect<InvalidDataException>(() => store.Save(
        Path.Combine(root, "invalid-layer-alpha.spineviewer.json"),
        realDocument with
        {
            SceneLayers = [realDocument.SceneLayers![0] with { TrackAlpha = double.NaN }]
        }));

    using var reopenedViewModel = new ShellViewModel(
        WorkspaceState.Empty,
        true,
        store,
        assetService: pngService,
        chooseProjectPathToOpen: () => realProject);
    await reopenedViewModel.OpenProjectAsync();
    Assert(reopenedViewModel.State == WorkspaceState.Ready && !reopenedViewModel.IsDirty, "Saved Viewer project did not reopen cleanly.");
    Assert(reopenedViewModel.ProjectPath == Path.GetFullPath(realProject), "Reopened project path was not restored.");
    Assert(reopenedViewModel.SceneLayers.Count == 3, "Reopened Viewer project did not restore scene layers.");
    Assert(
        reopenedViewModel.ModelX == 37
        && reopenedViewModel.SceneLayers[1].Opacity == 0.5
        && reopenedViewModel.SceneLayers[1].TrackAlpha == 0.35
        && reopenedViewModel.SceneLayers[1].Pma,
        "Reopened Viewer project did not restore alpha edits.");
    Assert(reopenedViewModel.SceneLayers.All(layer => layer.PreviewFrame is not null), "Reopened scene previews were not rendered.");
    await reopenedViewModel.OpenProjectAsync(Path.Combine(root, "missing.spineviewer.json"));
    Assert(reopenedViewModel.State == WorkspaceState.Ready && reopenedViewModel.SceneLayers.Count == 3 && reopenedViewModel.ModelX == 37,
        "Failed project open did not preserve the current session.");

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
        pngService);
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

    if (!string.Equals(
            Environment.GetEnvironmentVariable("SPINEVIEWER_SKIP_WINDOW_SMOKE"),
            "1",
            StringComparison.Ordinal))
        AssertCompactWindowShows(store, assetService);

    File.WriteAllText(project, File.ReadAllText(project).Replace("\"schemaVersion\": 1", "\"schemaVersion\": 2"));
    Expect<InvalidDataException>(() => store.Load(project));

    Console.WriteLine("Viewer project, PNG render preview, edit history, validation, and source isolation passed.");
}
finally
{
    Directory.Delete(root, true);
}
}

static void Assert(bool condition, string message)
{
    if (!condition) throw new InvalidOperationException(message);
}

static byte[] FrameHash(RenderedFrame? frame)
{
    return SHA256.HashData((frame ?? throw new InvalidOperationException("Expected a rendered frame.")).Bgra32);
}

static RenderedFrame ReadPngFrame(string path)
{
    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    var decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
    BitmapSource source = decoder.Frames.Single();
    if (source.Format != PixelFormats.Bgra32)
        source = new FormatConvertedBitmap(source, PixelFormats.Bgra32, null, 0);

    var pixels = new byte[checked(source.PixelWidth * source.PixelHeight * 4)];
    source.CopyPixels(pixels, source.PixelWidth * 4, 0);
    return new RenderedFrame(source.PixelWidth, source.PixelHeight, pixels);
}

static (int RedCount, double RedX, int BlueCount, double BlueX) MeasureRedBlue(RenderedFrame frame)
{
    long redX = 0;
    long blueX = 0;
    var redCount = 0;
    var blueCount = 0;
    for (var y = 0; y < frame.Height; y++)
    {
        for (var x = 0; x < frame.Width; x++)
        {
            var offset = (y * frame.Width + x) * 4;
            var blue = frame.Bgra32[offset];
            var green = frame.Bgra32[offset + 1];
            var red = frame.Bgra32[offset + 2];
            var alpha = frame.Bgra32[offset + 3];
            if (alpha >= 240 && red >= 240 && green <= 15 && blue <= 15)
            {
                redCount++;
                redX += x;
            }
            else if (alpha >= 240 && blue >= 240 && green <= 15 && red <= 15)
            {
                blueCount++;
                blueX += x;
            }
        }
    }

    return (
        redCount,
        redCount == 0 ? double.NaN : (double)redX / redCount,
        blueCount,
        blueCount == 0 ? double.NaN : (double)blueX / blueCount);
}

static void AssertCompactWindowShows(ViewerProjectStore store, AssetService assetService)
{
    Exception? failure = null;
    var thread = new Thread(() =>
    {
        try
        {
            var window = new MainWindow
            {
                DataContext = new ShellViewModel(
                    WorkspaceState.ReadyWithWarnings,
                    false,
                    store,
                    assetService: assetService)
            };
            window.Show();
            Assert(window.IsVisible, "Compact warning-state shell did not show.");
            window.Close();
        }
        catch (Exception exception)
        {
            failure = exception;
        }
        finally
        {
            Dispatcher.CurrentDispatcher.InvokeShutdown();
        }
    })
    {
        IsBackground = true
    };
    thread.SetApartmentState(ApartmentState.STA);
    thread.Start();
    Assert(thread.Join(TimeSpan.FromSeconds(10)), "Compact warning-state shell blocked during startup.");
    if (failure is not null)
        throw new InvalidOperationException("Compact warning-state shell failed during startup.", failure);
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
    public IRuntimeRenderSession OpenSession(string skeletonPath, string atlasPath, CancellationToken cancellationToken) =>
        throw new NotSupportedException();
}
