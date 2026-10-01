using System.Text.Json.Nodes;
using SpineViewerWPF.Application;
using SpineViewerWPF.Core;
using SpineViewerWPF.Wpf;

internal static class SetupPoseSmoke
{
    public static async Task RunAsync(AssetService service, string temporaryRoot)
    {
        var root = Path.Combine(temporaryRoot, "setup-pose");
        Directory.CreateDirectory(root);
        var fixtures = Path.Combine(Directory.GetCurrentDirectory(), "tests", "fixtures");
        foreach (var name in new[] { "v21_08", "v21_25", "v31_07", "v32", "v34_02", "v35_51",
                     "v36_32", "v36_39", "v36_53", "v37_94", "v38_95", "v40_31", "v40", "v41", "v42", "v43" })
        {
            var path = CopyFixture(Path.Combine(fixtures, name + "-minimal"), Path.Combine(root, name));
            var json = JsonNode.Parse(File.ReadAllText(path))!;
            json["animations"] = new JsonObject();
            File.WriteAllText(path, json.ToJsonString());
            var inspection = service.Inspect(path, null, null);
            Check(inspection.Success && inspection.Animations.Count == 0, name + " setup inspection failed.");
            using var session = service.OpenRenderSession(path, null, inspection.Runtime.SelectedLine).Session;
            var setup = session.RenderFrame("", 0, 64, 64, false, ["default"]);
            Check(Visible(setup), name + " setup frame is blank.");
            Check(setup.Bgra32.SequenceEqual(session.RenderFrame("", 7, 64, 64, false, ["default"]).Bgra32),
                name + " setup pose advanced with time/Physics.");
            Check(session.RenderScene("", 0, false, ["default"]).DrawCommands.Count > 0,
                name + " setup GPU-neutral scene is empty.");
            var layer = service.OpenSceneLayer(path, null, null, 64, 64, Path.Combine(root, name + ".png"));
            Check(layer.Animation == "" && File.Exists(layer.PreviewPath), name + " setup layer failed.");

            // Reusing an animated session must not leak its last pose into setup pose.
            using var animated = service.OpenRenderSession(Path.Combine(fixtures, name + "-minimal", "minimal.json"), null, null).Session;
            var first = animated.RenderFrame("", 0, 64, 64, false, ["default"]);
            animated.RenderFrame("move", 0.5f, 64, 64, false, ["default"]);
            Check(first.Bgra32.SequenceEqual(animated.RenderFrame("", 0, 64, 64, false, ["default"]).Bgra32),
                name + " animated-to-setup session retained pose state.");
        }

        var setupPath = CopyFixture(Path.Combine(fixtures, "v41-setup-pose"), Path.Combine(root, "static"));
        var sourceBytes = File.ReadAllBytes(setupPath);
        var projectPath = Path.Combine(root, "static.spineviewer.json");
        var screenshotPath = Path.Combine(root, "static-capture.png");
        var exportPath = Path.Combine(root, "static-export", "pose.png");
        var store = new ViewerProjectStore();
        using (var shell = new ShellViewModel(WorkspaceState.Empty, true, store, _ => projectPath,
                   service, confirmDiscardChanges: () => true, chooseScreenshotPath: () => screenshotPath,
                   chooseExportPath: () => exportPath, chooseAssetPaths: () => [setupPath]))
        {
            shell.SetViewportSize(64, 64);
            await shell.OpenAssetAsync(setupPath);
            Check(shell.State == WorkspaceState.Ready && shell.HasNoAnimations && shell.SelectedAnimation is null,
                "Static asset did not reach Ready with empty animation UI.");
            Check(!shell.IsPlaying && !shell.TogglePlayCommand.CanExecute(null) && shell.SceneLayers.Count == 1,
                "Static asset playback/layer state is invalid.");
            var initial = shell.PreviewFrame!.Bgra32.ToArray();
            Check(Visible(shell.PreviewFrame), "WPF setup preview is blank.");
            shell.SelectedSceneLayer!.SelectedSkin = "faded";
            await Until(() => !initial.SequenceEqual(shell.PreviewFrame!.Bgra32), "Static skin edit did not change pixels.");
            var faded = shell.PreviewFrame!.Bgra32.ToArray();
            Check(shell.TrySave(), "Static sidecar save failed.");
            var saved = store.Load(projectPath);
            Check(saved.SceneLayers![0].Animation == "" && saved.SceneLayers[0].SelectedSkin == "faded",
                "Sidecar did not persist setup pose/skin.");
            await shell.OpenProjectAsync(projectPath);
            Check(shell.State == WorkspaceState.Ready && shell.SceneLayers[0].Animation == ""
                && shell.PreviewFrame!.Bgra32.SequenceEqual(faded), "Static sidecar restore changed the frame.");
            Check(shell.ScreenshotCommand.CanExecute(null), "Static screenshot is disabled.");
            shell.ScreenshotCommand.Execute(null);
            await Until(() => shell.LastAction == "Captured static-capture.png", "Static screenshot did not finish.");
            Check(File.Exists(screenshotPath), "Static screenshot missing.");
            Check(shell.ExportCommand.CanExecute(null), "Static export is disabled.");
            shell.ExportCommand.Execute(null);
            await Until(() => shell.LastAction == "Exported 1 frames", "Static export did not produce one frame.");
            Check(Directory.GetFiles(Path.GetDirectoryName(exportPath)!, "*.png").Length == 1,
                "Static export produced more than one frame.");
            shell.AddLayerCommand.Execute(null);
            await Until(() => shell.SceneLayers.Count == 2 && shell.CanAddLayer, "Static layer add failed.");
            shell.DuplicateLayerCommand.Execute(null);
            await Until(() => shell.SceneLayers.Count == 3 && shell.CanAddLayer, "Static layer duplicate failed.");
            var beforeReload = shell.SelectedSceneLayer;
            shell.ReloadLayerCommand.Execute(null);
            await Until(() => shell.SelectedSceneLayer != beforeReload && shell.CanReloadLayer, "Static layer reload failed.");
            Check(shell.TrySave(), "Static multi-layer save failed.");
            await shell.OpenProjectAsync(projectPath);
            Check(shell.State == WorkspaceState.Ready && shell.SceneLayers.Count == 3
                && shell.SceneLayers.All(layer => layer.Animation == "" && Visible(layer.PreviewFrame!)),
                "Static multi-layer sidecar did not reopen.");
        }
        Check(sourceBytes.SequenceEqual(File.ReadAllBytes(setupPath)), "Static source was modified.");

        var animatedPath = CopyFixture(Path.Combine(fixtures, "v41-minimal"), Path.Combine(root, "animated"));
        var staticDocument = store.Load(projectPath).SceneLayers![0] with { ModelX = -10, ModelY = 0 };
        var movingDocument = staticDocument with { SkeletonPath = animatedPath,
            AtlasPath = Path.ChangeExtension(animatedPath, ".atlas"), Animation = "move", SelectedSkin = "default", ModelX = 10, ZIndex = 1 };
        var mixed = service.Export(new AnimationExportRequest(animatedPath, null, "4.1", "move", 1, 2,
            64, 64, Path.Combine(root, "mixed"), "mixed", false, false, ["default"],
            SceneLayers: [staticDocument, movingDocument]));
        Check(mixed.FrameCount == 3 && mixed.OutputPaths.All(File.Exists), "Static/animated composition failed.");

        await SelectionMemoryAsync(service, root, setupPath, animatedPath);
        Console.WriteLine("TASK-059: 16 Runtime setup poses, static layer/sidecar/skin/export and asset selection memory passed.");
    }

    private static async Task SelectionMemoryAsync(AssetService service, string root, string setupPath, string otherPath)
    {
        var asset = CopyFixture(Path.GetDirectoryName(setupPath)!, Path.Combine(root, "memory"));
        var json = JsonNode.Parse(File.ReadAllText(asset))!;
        var move = JsonNode.Parse(File.ReadAllText(otherPath))!["animations"]!["move"]!;
        json["animations"] = new JsonObject { ["first"] = move.DeepClone(), ["second"] = move.DeepClone() };
        File.WriteAllText(asset, json.ToJsonString());
        var sidecar = Path.Combine(root, "memory.spineviewer.json");
        using var shell = new ShellViewModel(WorkspaceState.Empty, true, chooseProjectPath: _ => sidecar,
            assetService: service, confirmDiscardChanges: () => true);
        shell.SetViewportSize(64, 64);
        await shell.OpenAssetAsync(asset);
        shell.IsPlaying = false;
        Check(shell.TrySave(), "Explicit selection sidecar save failed.");
        shell.SelectedSceneLayer!.Animation = "second";
        shell.SelectedSceneLayer.SelectedSkin = "faded";
        await shell.OpenAssetAsync(otherPath);
        await shell.OpenAssetAsync(Path.Combine(Path.GetDirectoryName(asset)!, ".", "minimal.json").ToUpperInvariant());
        Check(shell.SelectedSceneLayer!.Animation == "second" && shell.SelectedSceneLayer.SelectedSkin == "faded",
            "A-B-A / full path alias did not restore animation and skin.");
        shell.ReloadCommand.Execute(null);
        await Until(() => shell.State == WorkspaceState.Ready, "Reload failed.");
        Check(shell.SelectedSceneLayer!.Animation == "second" && shell.SelectedSceneLayer.SelectedSkin == "faded",
            "Reload lost selection memory.");
        await shell.OpenProjectAsync(sidecar);
        Check(shell.SelectedSceneLayer!.Animation == "first" && shell.SelectedSceneLayer.SelectedSkin == "default",
            "Memory overrode explicit sidecar selection.");
        shell.SelectedSceneLayer.Animation = "second";
        shell.SelectedSceneLayer.SelectedSkin = "faded";
        await shell.OpenAssetAsync(otherPath);
        json["animations"]!.AsObject().Remove("second");
        json["skins"]!.AsArray().RemoveAt(1);
        File.WriteAllText(asset, json.ToJsonString());
        await shell.OpenAssetAsync(asset);
        Check(shell.SelectedSceneLayer!.Animation == "first" && shell.SelectedSceneLayer.SelectedSkin == "default",
            "Removed remembered values did not fall back to available values.");
        await shell.OpenAssetAsync(otherPath);
        json["animations"] = new JsonObject();
        File.WriteAllText(asset, json.ToJsonString());
        await shell.OpenAssetAsync(asset);
        Check(shell.State == WorkspaceState.Ready && shell.SelectedSceneLayer!.Animation == "",
            "Changed inspection did not fall back to setup pose.");
        await shell.OpenAssetAsync(setupPath);
        shell.SelectedSceneLayer!.SelectedSkin = "faded";
        await shell.OpenAssetAsync(otherPath);
        await shell.OpenAssetAsync(setupPath);
        Check(shell.HasNoAnimations && shell.SelectedSceneLayer!.SelectedSkin == "faded", "Static selection memory failed.");

        var gate = new GatedAdapter(new SpineRuntime.V41.SpineV41Adapter(), setupPath);
        using var race = new ShellViewModel(WorkspaceState.Empty, true, assetService: new AssetService(gate),
            confirmDiscardChanges: () => true);
        race.SetViewportSize(64, 64);
        await race.OpenAssetAsync(setupPath);
        race.SelectedSceneLayer!.SelectedSkin = "faded";
        await race.OpenAssetAsync(otherPath);
        gate.BlockNext = true;
        var slow = race.OpenAssetAsync(setupPath);
        await Until(() => gate.Blocked, "Delayed load did not reach gate.");
        try
        {
            await race.OpenAssetAsync(otherPath);
            Check(race.SelectedSceneLayer!.SkeletonPath == otherPath && race.SelectedSceneLayer.Animation == "move",
                "Latest asset did not apply while older load waited.");
        }
        finally { gate.Release.Set(); }
        await slow;
        Check(race.SelectedSceneLayer!.SkeletonPath == otherPath && race.SelectedSceneLayer.Animation == "move",
            "Late load replaced active selection.");
        Check(gate.DisposedSessions >= 2, "Replaced/stale sessions were not disposed.");
        await race.OpenAssetAsync(setupPath);
        Check(race.SelectedSceneLayer!.SelectedSkin == "faded", "Stale completion corrupted asset memory.");
    }

    private static string CopyFixture(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source)) File.Copy(file, Path.Combine(target, Path.GetFileName(file)), true);
        return Path.Combine(target, "minimal.json");
    }

    private static bool Visible(RenderedFrame frame) =>
        Enumerable.Range(0, frame.Width * frame.Height).Any(pixel => frame.Bgra32[pixel * 4 + 3] > 0);

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static async Task Until(Func<bool> condition, string message)
    {
        var timeout = DateTime.UtcNow.AddSeconds(20);
        while (!condition() && DateTime.UtcNow < timeout) await Task.Delay(10);
        Check(condition(), message);
    }

    private sealed class GatedAdapter(IRuntimeAdapter inner, string path) : IRuntimeAdapter
    {
        public string RuntimeLine => inner.RuntimeLine;
        public bool BlockNext;
        public volatile bool Blocked;
        public ManualResetEventSlim Release { get; } = new(false);
        public int DisposedSessions;
        public InspectResult Inspect(string skeletonPath, string atlasPath, bool overridden, CancellationToken cancellationToken) =>
            inner.Inspect(skeletonPath, atlasPath, overridden, cancellationToken);
        public IRuntimeRenderSession OpenSession(string skeletonPath, string atlasPath, CancellationToken cancellationToken)
        {
            var session = inner.OpenSession(skeletonPath, atlasPath, cancellationToken);
            if (BlockNext && string.Equals(path, skeletonPath, StringComparison.OrdinalIgnoreCase))
            {
                BlockNext = false;
                Blocked = true;
                if (!Release.Wait(TimeSpan.FromSeconds(20))) { session.Dispose(); throw new TimeoutException("Load gate timed out."); }
            }
            return new CountedSession(session, () => Interlocked.Increment(ref DisposedSessions));
        }
    }

    private sealed class CountedSession(IRuntimeRenderSession inner, Action disposed) : IRuntimeRenderSession
    {
        public void Render(RenderRequest request, CancellationToken token) => inner.Render(request, token);
        public RenderedFrame RenderFrame(FrameRenderRequest request, CancellationToken token) => inner.RenderFrame(request, token);
        public PreviewSceneFrame RenderScene(PreviewSceneRequest request, CancellationToken token) => inner.RenderScene(request, token);
        public void Dispose() { inner.Dispose(); disposed(); }
    }
}
