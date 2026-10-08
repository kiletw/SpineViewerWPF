using System.Security.Cryptography;
using System.Text.Json.Nodes;
using SpineViewerWPF.Application;
using SpineViewerWPF.Core;
using SpineViewerWPF.Wpf;

// TASK-072: export frame range and per-animation batch export.
static class BatchExportSmoke
{
    public static void Run(AssetService service, AnimationExportRequest sequenceRequest, string temporaryRoot)
    {
        var root = Path.Combine(temporaryRoot, "batch");
        Directory.CreateDirectory(root);

        // Frame range: frames start at StartSeconds and match the same times in a full export.
        var full = service.Export(sequenceRequest with { OutputDirectory = Path.Combine(root, "range-full") });
        var ranged = service.Export(sequenceRequest with
        {
            OutputDirectory = Path.Combine(root, "range-part"),
            StartSeconds = 0.5f
        });
        Check(full.FrameCount == 11 && ranged.FrameCount == 6, $"Range export frame count was {ranged.FrameCount}, expected 6.");
        Check(ranged.OutputPaths.Select((path, index) => (path, index)).All(item =>
                Hash(item.path).SequenceEqual(Hash(full.OutputPaths[item.index + 5]))),
            "Range export frames did not match the same times in a full export.");
        var framedFull = service.Export(sequenceRequest with
        {
            OutputDirectory = Path.Combine(root, "range-framed-full"),
            Framing = new ExportFraming(1, 2)
        });
        var framedRanged = service.Export(sequenceRequest with
        {
            OutputDirectory = Path.Combine(root, "range-framed-part"),
            Framing = new ExportFraming(1, 2),
            StartSeconds = 0.5f
        });
        Check(framedRanged.FrameCount == 6 && framedRanged.Width > 0 && framedRanged.Width <= framedFull.Width,
            "Auto-fit range export did not fit only the exported frames.");
        foreach (var start in new[] { -0.1f, 1.5f, float.NaN })
            Expect<ArgumentOutOfRangeException>(() => service.Export(sequenceRequest with
            {
                OutputDirectory = Path.Combine(root, "range-invalid"),
                StartSeconds = start
            }));
        Check(!Directory.Exists(Path.Combine(root, "range-invalid")), "Invalid range export created its output directory.");

        // Batch: a skeleton with three animations, one name needing sanitizing.
        var skeleton = Path.Combine(root, "batch.json");
        var node = JsonNode.Parse(File.ReadAllText(sequenceRequest.SkeletonPath))!;
        node["animations"]!["rise"] = JsonNode.Parse("""{"bones":{"sprite":{"translate":[{},{"time":0.5,"y":8}]}}}""");
        node["animations"]!["folder/hop"] = JsonNode.Parse("""{"bones":{"sprite":{"translate":[{},{"time":0.2,"x":-4}]}}}""");
        File.WriteAllText(skeleton, node.ToJsonString());
        var template = sequenceRequest with { SkeletonPath = skeleton };
        var output = Path.Combine(root, "out", "hero.png");

        var reports = new List<AnimationExportProgress>();
        var results = service.ExportBatch(
            new AnimationBatchExportRequest(template, ["move", "rise", "folder/hop"]),
            output,
            progress: new InlineProgress(reports.Add));
        Check(results.Count == 3
              && results[0].FrameCount == 11 && results[1].FrameCount == 6 && results[2].FrameCount == 3,
            "Batch export did not use each animation's own duration.");
        Check(Path.GetFileName(results[0].OutputPaths[0]) == "hero-move-0000.png"
              && Path.GetFileName(results[2].OutputPaths[0]) == "hero-folder_hop-0000.png",
            "Batch export naming was not stable or not sanitized.");
        Check(results.SelectMany(result => result.OutputPaths).All(File.Exists), "Batch export outputs are missing.");
        Check(reports.Count > 0 && reports[^1].CompletedFrames == 20 && reports.All(report => report.TotalFrames == 20),
            "Batch export progress did not cover every frame once.");
        Check(results[0].OutputPaths.Zip(full.OutputPaths).All(pair => Hash(pair.First).SequenceEqual(Hash(pair.Second))),
            "Batch export frames differ from a single-animation export.");

        // Existing targets fail before anything is written.
        var before = Directory.EnumerateFiles(Path.GetDirectoryName(output)!).Count();
        Expect<IOException>(() => service.ExportBatch(new AnimationBatchExportRequest(template, ["rise", "move"]), output));
        Check(Directory.EnumerateFiles(Path.GetDirectoryName(output)!).Count() == before, "A conflicting batch wrote files.");

        // Invalid batches.
        Expect<ArgumentException>(() => service.ExportBatch(new AnimationBatchExportRequest(template, []), output));
        Expect<ArgumentException>(() => service.ExportBatch(new AnimationBatchExportRequest(template, ["move", "move"]), output));
        Expect<InvalidDataException>(() => service.ExportBatch(
            new AnimationBatchExportRequest(template, ["missing"]), Path.Combine(root, "missing", "hero.png")));

        // Scene batch varies only the chosen layer; a cancellation removes partial output.
        SceneLayerDocument Layer(double x) => new(
            skeleton, sequenceRequest.AtlasPath, null, "move", "default", x, 0, 1, 0, false, false, true, 1, 0, 1, false, null);
        var scene = template with { SceneLayers = [Layer(-12), Layer(12)] };
        Expect<ArgumentException>(() => service.ExportBatch(new AnimationBatchExportRequest(scene, ["rise"]), output));
        var sceneResults = service.ExportBatch(
            new AnimationBatchExportRequest(scene, ["rise", "folder/hop"], 1),
            Path.Combine(root, "scene", "pair.png"));
        Check(sceneResults[0].FrameCount == 6 && sceneResults[1].FrameCount == 3, "Scene batch did not follow the varied layer's durations.");
        using (var canceled = new CancellationTokenSource())
        {
            var canceledOutput = Path.Combine(root, "canceled", "hero.png");
            Expect<OperationCanceledException>(() => service.ExportBatch(
                new AnimationBatchExportRequest(template, ["move", "rise"]),
                canceledOutput,
                progress: new InlineProgress(report =>
                {
                    if (report.CompletedFrames >= 12) canceled.Cancel();
                }),
                cancellationToken: canceled.Token));
            Check(!Directory.Exists(Path.GetDirectoryName(canceledOutput))
                  || !Directory.EnumerateFiles(Path.GetDirectoryName(canceledOutput)!).Any(),
                "A canceled batch left partial output.");
        }
        if (FfmpegLocator.Find(null) is { } ffmpeg)
        {
            var gifResults = service.ExportBatch(
                new AnimationBatchExportRequest(template with { Framing = new ExportFraming(1, 2) }, ["rise", "folder/hop"]),
                Path.Combine(root, "gif", "hero.gif"),
                new AnimationEncodeOptions(AnimationEncodeFormat.Gif, ffmpeg));
            Check(gifResults.Select(result => Path.GetFileName(result.OutputPaths.Single()))
                      .SequenceEqual(["hero-rise.gif", "hero-folder_hop.gif"])
                  && gifResults.All(result => new FileInfo(result.OutputPaths[0]).Length > 0),
                "Encoded batch export did not write one file per animation.");
        }
        else
            Console.WriteLine("TASK-072: FFmpeg was not found on PATH; encoded batch check skipped.");
        Console.WriteLine("TASK-072: range and batch export passed.");
    }

    // Physics warm-up changes 4.2/4.3 physics poses, keeps time 0 animation
    // poses, and is ignored by Runtimes without Physics.
    public static void RunPhysicsWarmup(AssetService service, string fixturesRoot, string temporaryRoot)
    {
        RenderedFrame Frame(string fixture, float time, int warmup)
        {
            var path = Path.Combine(fixturesRoot, fixture, "minimal.json");
            var opened = service.OpenRenderSession(path, Path.ChangeExtension(path, ".atlas"), null);
            using (opened.Session)
                return opened.Session.RenderFrame("move", time, 64, 64, false, ["default"], physicsWarmupLoops: warmup);
        }

        // Geometry, not pixels: the fixture's physics offset is sub-pixel at 64 px.
        float FirstX(string fixture, float time, int warmup)
        {
            var path = Path.Combine(fixturesRoot, fixture, "minimal.json");
            var opened = service.OpenRenderSession(path, Path.ChangeExtension(path, ".atlas"), null);
            using (opened.Session)
                return opened.Session.RenderScene("move", time, false, ["default"], physicsWarmupLoops: warmup)
                    .DrawCommands[0].Vertices[0].X;
        }

        foreach (var fixture in new[] { "v42-minimal", "v43-minimal" })
        {
            Check(Math.Abs(FirstX(fixture, 0, 0) - FirstX(fixture, 0, 1)) > 1e-4,
                $"{fixture}: physics warm-up did not change the first frame.");
            Check(Frame(fixture, 0.5f, 2).Bgra32.SequenceEqual(Frame(fixture, 0.5f, 2).Bgra32),
                $"{fixture}: physics warm-up was not deterministic.");
        }
        Check(Frame("v41-minimal", 0.5f, 0).Bgra32.SequenceEqual(Frame("v41-minimal", 0.5f, 3).Bgra32),
            "A Runtime without Physics changed output for a warm-up.");

        var path = Path.Combine(fixturesRoot, "v42-minimal", "minimal.json");
        var request = new AnimationExportRequest(path, Path.ChangeExtension(path, ".atlas"), null, "move", 1, 10, 64, 64,
            Path.Combine(temporaryRoot, "warmup-0"), "move.png", false, false, ["default"]);
        var cold = service.Export(request);
        var warm = service.Export(request with { OutputDirectory = Path.Combine(temporaryRoot, "warmup-1"), PhysicsWarmupLoops = 1 });
        Check(cold.FrameCount == warm.FrameCount && !Hash(cold.OutputPaths[0]).SequenceEqual(Hash(warm.OutputPaths[0])),
            "Export physics warm-up did not change the first frame or changed the frame count.");
        foreach (var loops in new[] { -1, 11 })
            Expect<ArgumentOutOfRangeException>(() => service.Export(request with
            {
                OutputDirectory = Path.Combine(temporaryRoot, "warmup-invalid"),
                PhysicsWarmupLoops = loops
            }));
        Console.WriteLine("TASK-072: physics warm-up passed.");
    }

    // Shell flow: All animations batches the selected layer; a custom range
    // exports part of the current animation; neither dirties the project.
    public static async Task RunShellAsync(AssetService service, ViewerProjectStore store, AnimationExportRequest sequenceRequest, string temporaryRoot)
    {
        var root = Path.Combine(temporaryRoot, "batch-shell");
        Directory.CreateDirectory(root);
        var skeleton = Path.Combine(root, "hero.json");
        var node = JsonNode.Parse(File.ReadAllText(sequenceRequest.SkeletonPath))!;
        node["animations"]!["rise"] = JsonNode.Parse("""{"bones":{"sprite":{"translate":[{},{"time":0.5,"y":8}]}}}""");
        File.WriteAllText(skeleton, node.ToJsonString());
        var atlasSource = sequenceRequest.AtlasPath!;
        File.Copy(atlasSource, Path.Combine(root, "hero.atlas"));
        foreach (var texture in Directory.EnumerateFiles(Path.GetDirectoryName(atlasSource)!, "png.png"))
            File.Copy(texture, Path.Combine(root, Path.GetFileName(texture)));

        string? target = null;
        using var shell = new ShellViewModel(
            WorkspaceState.Empty,
            true,
            store,
            _ => null,
            service,
            chooseAssetPath: () => skeleton,
            chooseExportPath: () => target);
        shell.SetViewportSize(256, 256);
        await shell.OpenAssetAsync(skeleton);
        Check(shell.State == WorkspaceState.Ready, $"The batch shell did not open its asset: {shell.State}.");
        shell.ExportFramesPerSecond = 10;

        async Task ExportAsync(string path)
        {
            target = path;
            shell.ExportCommand.Execute(null);
            for (var attempt = 0; attempt < 600 && shell.IsExporting; attempt++)
                await Task.Delay(20);
            await Task.Delay(20);
            Check(!shell.IsExporting, "Shell export did not finish.");
        }

        shell.ExportAnimationScope = "All animations";
        Check(shell.IsExportAllAnimations && !shell.IsExportRangeAvailable
              && shell.ExportSizeSummary.StartsWith("All animations · ", StringComparison.Ordinal),
            "All animations did not hide the range and update the summary.");
        await ExportAsync(Path.Combine(root, "all", "hero.png"));
        Check(File.Exists(Path.Combine(root, "all", "hero-move-0010.png")) && File.Exists(Path.Combine(root, "all", "hero-rise-0005.png"))
              && shell.LastAction == "Exported 2 animations (17 frames)",
            $"Shell batch export did not write every animation: {shell.LastAction}");

        shell.ExportAnimationScope = "Current";
        shell.ExportRangeMode = "Custom";
        Check(shell.IsExportCustomRange && shell.ExportRangeStart == 0 && Math.Abs(shell.ExportRangeEnd - shell.Duration) < 0.0001,
            "A new custom range did not start from the whole current animation.");
        shell.ExportRangeStart = shell.Duration / 2;
        shell.ExportRangeEnd = shell.Duration * 4;
        var expected = (int)Math.Floor((shell.Duration - shell.Duration / 2) * 10 + 1e-4) + 1;
        await ExportAsync(Path.Combine(root, "range", "part.png"));
        var written = Directory.EnumerateFiles(Path.Combine(root, "range"), "part-*.png").Count();
        Check(written == expected && shell.LastAction == $"Exported {expected} frames",
            $"Shell range export wrote {written} frames ({shell.LastAction}), expected {expected}.");
        shell.ExportPhysicsWarmupLoops = 99;
        Check(shell.ExportPhysicsWarmupLoops == 10, "Physics warm-up was not clamped to 10 loops.");
        shell.ExportPhysicsWarmupLoops = -3;
        Check(shell.ExportPhysicsWarmupLoops == 0, "Physics warm-up was not clamped to 0 loops.");
        Check(!shell.IsDirty, "Export scope, range, or warm-up dirtied the project.");
        Console.WriteLine("TASK-072: shell batch and range export passed.");
    }

    private static byte[] Hash(string path) => SHA256.HashData(File.ReadAllBytes(path));

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
