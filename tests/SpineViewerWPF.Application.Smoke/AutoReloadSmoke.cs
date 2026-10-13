using System.Collections.Concurrent;
using System.Text.Json.Nodes;
using SpineViewerWPF.Application;
using SpineViewerWPF.Core;
using SpineViewerWPF.Wpf;

// TASK-073: recent files, auto reload on source change.
static class AutoReloadSmoke
{
    public static async Task RunAsync(AssetService service, ViewerProjectStore store, AnimationExportRequest sequenceRequest, string temporaryRoot)
    {
        var root = Path.Combine(temporaryRoot, "auto-reload");
        Directory.CreateDirectory(root);
        var skeleton = Path.Combine(root, "hero.json");
        var atlas = Path.Combine(root, "hero.atlas");
        var texture = Path.Combine(root, "png.png");
        File.Copy(sequenceRequest.SkeletonPath, skeleton);
        File.Copy(sequenceRequest.AtlasPath!, atlas);
        File.Copy(Path.Combine(Path.GetDirectoryName(sequenceRequest.AtlasPath!)!, "png.png"), texture);

        // Settings: defaults, legacy files, de-duplicated and capped recent files.
        var settingsPath = Path.Combine(root, "settings", "settings.json");
        var settings = UserSettingsStore.Load(settingsPath);
        Check(settings.AutoReload && settings.RecentFiles.Count == 0, "New settings did not default to auto reload and no recent files.");
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        File.WriteAllText(settingsPath, """{ "SchemaVersion": 1, "FfmpegPath": "C:\\tools\\ffmpeg.exe" }""");
        settings = UserSettingsStore.Load(settingsPath);
        Check(settings.FfmpegPath == @"C:\tools\ffmpeg.exe" && settings.AutoReload && settings.RecentFiles.Count == 0,
            "A settings file from before TASK-073 did not load with defaults.");
        for (var index = 0; index < 12; index++) settings.AddRecentFile(Path.Combine(root, $"f{index}.json"));
        settings.AddRecentFile(Path.Combine(root, "F3.JSON"));
        var reloaded = UserSettingsStore.Load(settingsPath);
        Check(reloaded.RecentFiles.Count == UserSettingsStore.MaxRecentFiles
              && reloaded.RecentFiles[0] == Path.Combine(root, "F3.JSON")
              && reloaded.RecentFiles.Count(item => item.EndsWith("f3.json", StringComparison.OrdinalIgnoreCase)) == 1
              && reloaded.RecentFiles[1] == Path.Combine(root, "f11.json")
              && reloaded.FfmpegPath == @"C:\tools\ffmpeg.exe",
            "Recent files were not de-duplicated, ordered, capped, and persisted.");
        settings.SetAutoReload(false);
        Check(!UserSettingsStore.Load(settingsPath).AutoReload, "The auto-reload preference did not persist.");
        settings.SetAutoReload(true);
        settings.ClearRecentFiles();

        // Watcher: the skeleton, atlas, and atlas pages; one debounced batch.
        var sources = SourceFileWatcher.SourceFiles(skeleton, atlas).Select(Path.GetFullPath).ToArray();
        Check(sources.SequenceEqual([skeleton, atlas, texture], StringComparer.OrdinalIgnoreCase),
            $"Watched sources were {string.Join(", ", sources)}.");
        var batches = new ConcurrentQueue<IReadOnlyCollection<string>>();
        using (var watcher = new SourceFileWatcher(batches.Enqueue, TimeSpan.FromMilliseconds(200)))
        {
            watcher.Watch(sources);
            File.WriteAllText(Path.Combine(root, "unrelated.png"), "x");
            File.SetLastWriteTimeUtc(skeleton, DateTime.UtcNow);
            File.AppendAllText(atlas, "\n");
            for (var attempt = 0; attempt < 250 && batches.IsEmpty; attempt++) await Task.Delay(20);
            await Task.Delay(400);
            Check(batches.Count == 1, $"The watcher reported {batches.Count} batches instead of one.");
            var batch = batches.Single();
            Check(batch.Contains(atlas, StringComparer.OrdinalIgnoreCase)
                  && !batch.Any(path => path.EndsWith("unrelated.png", StringComparison.OrdinalIgnoreCase)),
                $"The watcher batch was {string.Join(", ", batch)}.");
        }

        // Shell: recent files and auto reload keep layer settings and selection.
        using var shell = new ShellViewModel(
            WorkspaceState.Empty,
            true,
            store,
            _ => null,
            service,
            chooseAssetPath: () => skeleton,
            userSettings: settings);
        shell.SetViewportSize(256, 256);
        await shell.OpenAssetAsync(skeleton);
        Check(shell.State == WorkspaceState.Ready, $"The auto-reload shell did not open its asset: {shell.State}.");
        Check(shell.HasRecentFiles && string.Equals(shell.RecentFiles[0], skeleton, StringComparison.OrdinalIgnoreCase),
            "Opening an asset did not record it as the most recent file.");
        Check(shell.WatchedSourceFiles.Count == 3 && shell.WatchedSourceFiles.Contains(texture, StringComparer.OrdinalIgnoreCase),
            "The shell did not watch the open layer's sources.");

        var layer = shell.SelectedSceneLayer!;
        layer.ModelX = 12;
        Check(!layer.Animations.Contains("rise"), "The fixture already had the added animation.");
        var node = JsonNode.Parse(File.ReadAllText(skeleton))!;
        node["animations"]!["rise"] = JsonNode.Parse("""{"bones":{"sprite":{"translate":[{},{"time":0.5,"y":8}]}}}""");
        File.WriteAllText(skeleton, node.ToJsonString());
        var action = shell.LastAction;
        await shell.ReloadChangedSourcesAsync([Path.Combine(root, "unrelated.png")]);
        Check(shell.LastAction == action && ReferenceEquals(shell.SelectedSceneLayer, layer), "An unrelated change reloaded a layer.");
        await shell.ReloadChangedSourcesAsync([skeleton]);
        var replacement = shell.SelectedSceneLayer!;
        Check(!ReferenceEquals(replacement, layer) && replacement.Animations.Contains("rise") && replacement.ModelX == 12
              && shell.LastAction.StartsWith("Auto-reloaded", StringComparison.Ordinal),
            $"Auto reload did not reopen the layer with its settings: {shell.LastAction}");

        shell.IsAutoReloadEnabled = false;
        Check(shell.WatchedSourceFiles.Count == 0 && !settings.AutoReload, "Turning auto reload off did not stop watching.");
        await shell.ReloadChangedSourcesAsync([skeleton]);
        Check(ReferenceEquals(shell.SelectedSceneLayer, replacement), "A change reloaded a layer while auto reload was off.");
        shell.IsAutoReloadEnabled = true;
        Check(shell.WatchedSourceFiles.Count == 3, "Turning auto reload on did not resume watching.");

        var missing = Path.Combine(root, "missing.json");
        settings.AddRecentFile(missing);
        await shell.OpenRecentAsync(missing);
        Check(!shell.RecentFiles.Contains(missing, StringComparer.OrdinalIgnoreCase)
              && shell.Diagnostics.Any(item => item.Code == "RECENT_FILE_MISSING"),
            "A missing recent file was not reported and removed.");
        shell.ClearRecentFilesCommand.Execute(null);
        Check(!shell.HasRecentFiles && UserSettingsStore.Load(settingsPath).RecentFiles.Count == 0, "Clear Recent Files did not clear the list.");
        Console.WriteLine("TASK-073: recent files and auto reload passed.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
