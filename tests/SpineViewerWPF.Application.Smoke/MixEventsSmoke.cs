using System.Text.Json.Nodes;
using SpineViewerWPF.Application;
using SpineViewerWPF.Core;

// TASK-074: animation mix and Spine event keys on every supported Runtime line.
static class MixEventsSmoke
{
    private static readonly string[] Lines =
    [
        "v21_08", "v21_25", "v31_07", "v32", "v34_02", "v35_51", "v36_32", "v36_39",
        "v36_53", "v37_94", "v38_95", "v40_31", "v40", "v41", "v42", "v43"
    ];

    public static void Run(AssetService service, string temporaryRoot)
    {
        var root = Path.Combine(temporaryRoot, "mix-events");
        var fixtures = Path.Combine(Directory.GetCurrentDirectory(), "tests", "fixtures");
        foreach (var line in Lines)
        {
            var directory = Path.Combine(root, line);
            Directory.CreateDirectory(directory);
            foreach (var file in Directory.EnumerateFiles(Path.Combine(fixtures, line + "-minimal")))
                File.Copy(file, Path.Combine(directory, Path.GetFileName(file)));
            var path = Path.Combine(directory, "minimal.json");

            // Events: a root event definition with defaults, keyed once in "move".
            var json = JsonNode.Parse(File.ReadAllText(path))!;
            json["events"] = JsonNode.Parse("""{ "step": { "int": 3, "string": "left" } }""");
            json["animations"]!["move"]!["events"] = JsonNode.Parse("""[ { "time": 0.5, "name": "step" } ]""");
            File.WriteAllText(path, json.ToJsonString());
            var inspection = service.Inspect(path, null, null);
            var events = inspection.Animations.Single(item => item.Name == "move").Events;
            Check(events is { Count: 1 } && Math.Abs(events[0].TimeSeconds - 0.5f) < 1e-4 && events[0].Name == "step"
                  && events[0].Int == 3 && events[0].String == "left",
                $"{line}: event keys were not reported ({events?.Count.ToString() ?? "null"}).");

            // Mix: target "move" at 0.5 (x = 4) mixing from "move" at 0.75 + 0.5 elapsed
            // (looped to 0.25, x = 2) halfway lands strictly between the two poses.
            using var session = service.OpenRenderSession(path, null, null).Session;
            float X(AnimationMix? mix) =>
                session.RenderScene("move", 0.5f, false, ["default"], mix: mix).DrawCommands[0].Vertices.Average(vertex => vertex.X);
            var plain = X(null);
            var mixed = X(new AnimationMix("move", 0.75f, 1, 0.5f));
            var source = session.RenderScene("move", 0.25f, false, ["default"]).DrawCommands[0].Vertices.Average(vertex => vertex.X);
            // The 4.2/4.3 fixtures put Physics on the moving bone, so the looping
            // source's jump overshoots; there the mix only has to change the pose.
            var physics = line is "v42" or "v43";
            Check(physics ? Math.Abs(mixed - plain) > 0.2f : mixed > source + 0.2f && mixed < plain - 0.2f,
                $"{line}: mixed x {mixed:0.###} was not between source {source:0.###} and target {plain:0.###}.");
            Check(X(new AnimationMix("move", 0.75f, 1, 1)) == plain && X(new AnimationMix("missing", 0.75f, 1, 0.5f)) == plain
                  && X(new AnimationMix("", 0.75f, 1, 0.5f)) == plain,
                $"{line}: a finished or invalid mix changed the pose.");
            // A fixed camera: the default per-frame fit would re-center both poses.
            var camera = new RenderCamera(0, 0, 1);
            var frame = session.RenderFrame("move", 0.5f, 64, 64, false, ["default"], camera: camera, mix: new AnimationMix("move", 0.75f, 1, 0.5f));
            Check(!frame.Bgra32.SequenceEqual(session.RenderFrame("move", 0.5f, 64, 64, false, ["default"], camera: camera).Bgra32),
                $"{line}: the CPU frame ignored the mix.");
        }
        Console.WriteLine($"TASK-074: mix and event keys passed on {Lines.Length} Runtime lines.");
    }

    // Shell: a switch during playback starts a preview mix; events show on the
    // timeline and when playback passes them.
    public static async Task RunShellAsync(AssetService service, SpineViewerWPF.Application.ViewerProjectStore store, AnimationExportRequest sequenceRequest, string temporaryRoot)
    {
        var root = Path.Combine(temporaryRoot, "mix-events-shell");
        Directory.CreateDirectory(root);
        var skeleton = Path.Combine(root, "hero.json");
        var json = JsonNode.Parse(File.ReadAllText(sequenceRequest.SkeletonPath))!;
        json["events"] = JsonNode.Parse("""{ "step": { "int": 3, "string": "left" } }""");
        json["animations"]!["move"]!["events"] = JsonNode.Parse("""[ { "time": 0.5, "name": "step" } ]""");
        json["animations"]!["rise"] = JsonNode.Parse("""{"bones":{"sprite":{"translate":[{},{"time":2,"y":8}]}}}""");
        File.WriteAllText(skeleton, json.ToJsonString());
        File.Copy(sequenceRequest.AtlasPath!, Path.Combine(root, "hero.atlas"));
        File.Copy(Path.Combine(Path.GetDirectoryName(sequenceRequest.AtlasPath!)!, "png.png"), Path.Combine(root, "png.png"));

        var settingsPath = Path.Combine(root, "settings.json");
        using var shell = new SpineViewerWPF.Wpf.ShellViewModel(
            SpineViewerWPF.Wpf.WorkspaceState.Empty, true, store, _ => null, service,
            chooseAssetPath: () => skeleton,
            userSettings: SpineViewerWPF.Wpf.UserSettingsStore.Load(settingsPath));
        shell.SetViewportSize(256, 256);
        await shell.OpenAssetAsync(skeleton);
        Check(shell.State == SpineViewerWPF.Wpf.WorkspaceState.Ready, $"The mix shell did not open: {shell.State}.");
        var layer = shell.SelectedSceneLayer!;
        Check(layer.Animation == "move", "The fixture did not start on move.");

        // Events on the timeline and as a passing label.
        Check(shell.TimelineEventMarkers is [{ } marker] && Math.Abs(marker.Fraction - 0.5) < 1e-6 && marker.Label.Contains("step"),
            "The timeline did not mark the event key.");
        shell.ReportCrossedEvent(0.4, 0.6, false);
        Check(shell.PlaybackEventLabel == "step (int 3, \"left\")" && shell.HasPlaybackEvent, $"Event label was '{shell.PlaybackEventLabel}'.");
        shell.ReportCrossedEvent(0.6, 0.7, false);
        Check(shell.PlaybackEventLabel == "step (int 3, \"left\")", "A tick without an event replaced the label.");

        // Mix: off by default; a paused switch never mixes.
        Check(shell.MixDuration == 0, "Mix was not off by default.");
        shell.MixDuration = 9;
        Check(shell.MixDuration == 5 && SpineViewerWPF.Wpf.UserSettingsStore.Load(settingsPath).MixDuration == 5, "Mix duration was not clamped and remembered.");
        shell.MixDuration = 0.4;
        shell.IsPlaying = false;
        layer.Animation = "rise";
        Check(shell.MixFor(layer) is null, "A paused animation switch started a mix.");
        layer.Animation = "move";

        shell.IsPlaying = true;
        shell.Position = 0.3;
        layer.Animation = "rise";
        var mix = shell.MixFor(layer);
        Check(mix is { FromAnimation: "move", ElapsedSeconds: 0 } && Math.Abs(mix.FromTimeSeconds - 0.3f) < 0.01f && Math.Abs(mix.DurationSeconds - 0.4f) < 1e-4,
            "A switch during playback did not start a mix from the previous animation.");
        shell.AdvanceMixes(0.25, false);
        Check(shell.MixFor(layer) is { ElapsedSeconds: > 0.24f and < 0.26f }, "The mix did not advance with playback.");
        shell.AdvanceMixes(0.25, false);
        Check(shell.MixFor(layer) is null, "A finished mix was not removed.");
        layer.Animation = "move";
        Check(shell.MixFor(layer) is not null, "Switching back did not mix.");
        shell.AdvanceMixes(0.01, true);
        Check(shell.MixFor(layer) is null, "Wrapping the timeline did not end the mix.");
        layer.Animation = "rise";
        shell.IsPlaying = false;
        Check(shell.MixFor(layer) is null, "Pausing did not end the mix.");
        Console.WriteLine("TASK-074: shell mix and event display passed.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
