using SpineViewerWPF.Application;
using SpineViewerWPF.Core;
using SpineViewerWPF.Wpf;

// TASK-068: folder asset browser.
static class BrowserSmoke
{
    public static async Task RunAsync(
        AssetService service,
        ViewerProjectStore store,
        string skeletonPath,
        string atlasPath,
        string texturePath,
        string temporaryRoot)
    {
        var folder = Path.Combine(temporaryRoot, "browser");
        var sub = Path.Combine(folder, "sub");
        Directory.CreateDirectory(sub);
        foreach (var target in new[] { folder, sub })
        {
            File.Copy(atlasPath, Path.Combine(target, Path.GetFileName(atlasPath)));
            File.Copy(texturePath, Path.Combine(target, Path.GetFileName(texturePath)));
        }
        File.Copy(skeletonPath, Path.Combine(folder, "a.json"));
        File.Copy(skeletonPath, Path.Combine(folder, "b.json"));
        File.Copy(skeletonPath, Path.Combine(sub, "c.json"));
        File.WriteAllText(Path.Combine(folder, "scene" + ViewerProjectStore.Extension), "{ \"skeleton\": \"a.json\" }");
        File.WriteAllText(Path.Combine(folder, "notes.json"), "{ \"title\": \"not spine\" }");
        File.WriteAllText(Path.Combine(folder, "readme.txt"), "\"skeleton\"");
        File.WriteAllBytes(Path.Combine(folder, "d.skel"), [1, 2, 3]);

        var flat = AssetBrowser.Scan(folder, false);
        Check(flat.Files.Select(Path.GetFileName).SequenceEqual(["a.json", "b.json", "d.skel"]) && !flat.Truncated,
            $"Browser scan did not list exactly the top-level skeletons: {string.Join(", ", flat.Files.Select(Path.GetFileName))}");
        var deep = AssetBrowser.Scan(folder, true);
        Check(deep.Files.Count == 4 && deep.Files[^1] == Path.Combine(sub, "c.json"), "Browser scan did not include subfolders.");
        var limited = AssetBrowser.Scan(folder, true, 2);
        Check(limited.Files.Count == 2 && limited.Truncated, "Browser scan did not honor its limit.");
        Expect<DirectoryNotFoundException>(() => AssetBrowser.Scan(Path.Combine(folder, "missing"), false));
        using (var canceled = new CancellationTokenSource())
        {
            canceled.Cancel();
            Expect<OperationCanceledException>(() => AssetBrowser.Scan(folder, true, cancellationToken: canceled.Token));
        }

        // A junction back to the scanned folder must not be followed.
        var loop = Path.Combine(sub, "loop");
        if (CreateJunction(loop, folder))
        {
            try
            {
                var looped = AssetBrowser.Scan(folder, true);
                Check(looped.Files.Count == 4 && !looped.Truncated,
                    $"Browser scan followed a junction: {looped.Files.Count} files.");
            }
            finally
            {
                Directory.Delete(loop);
            }
        }
        else
        {
            Console.WriteLine("TASK-068: could not create a junction; the junction check was skipped.");
        }

        var settingsPath = Path.Combine(temporaryRoot, "browser-settings", "settings.json");
        var settings = UserSettingsStore.Load(settingsPath);
        settings.SetFfmpegPath(@"C:\tools\ffmpeg.exe");
        using var shell = new ShellViewModel(
            WorkspaceState.Empty,
            true,
            store,
            _ => null,
            service,
            confirmDiscardChanges: () => true,
            userSettings: settings,
            chooseBrowserFolder: () => folder);
        shell.SetViewportSize(256, 256);
        var browser = shell.AssetBrowser;
        Check(browser.Folder is null && browser.Items.Count == 0 && shell.IsLayersPanelActive, "The browser did not start empty on the Layers view.");
        shell.IsAssetBrowserActive = true;
        Check(!shell.IsLayersPanelActive, "Switching to the browser did not hide the Layers view.");

        await browser.SetFolderAsync(folder);
        var listedBeforeDetails = browser.FilteredItems;
        Check(browser.Items.Select(item => item.Name).SequenceEqual(["a.json", "b.json", "d.skel"]), "Browser items did not follow the scan.");
        // Details load only for rows that are shown (their Detail is read).
        await Task.Delay(200);
        Check(browser.Items.All(item => !item.HasDetail), "Browser details were loaded before any row was shown.");
        _ = browser.Items[0].Detail;
        _ = browser.Items[2].Detail;
        await DetailsOf(browser.Items[0], browser.Items[2]);
        Check(!browser.Items[1].HasDetail, "Browser details were loaded for a row that was not shown.");
        Check(ReferenceEquals(listedBeforeDetails, browser.FilteredItems), "Detail updates rebuilt the browser list.");
        var a = browser.Items[0];
        Check(a.Detail.StartsWith("4.1", StringComparison.Ordinal) && a.Detail.EndsWith(" · 1 animation", StringComparison.Ordinal) && !a.HasProblem,
            $"Browser details were not filled in: {a.Detail}");
        Check(browser.Items[2].HasProblem, "An unreadable skeleton was not marked as a problem.");
        Check(browser.CountLabel == "3 skeletons", $"Unexpected count label: {browser.CountLabel}");

        browser.Filter = "B.J";
        Check(browser.FilteredItems.Count == 1 && browser.FilteredItems[0].Name == "b.json" && browser.CountLabel == "1 of 3",
            "The browser filter did not match file names case-insensitively.");
        browser.Filter = "";

        browser.IncludeSubfolders = true;
        await browser.ScanAsync();
        Check(browser.Items.Count == 4 && browser.Items[3].Location == "sub", "Including subfolders did not rescan with relative locations.");
        Check(browser.Items[0].HasDetail && browser.Items[2].HasDetail && !browser.Items[1].HasDetail && !browser.Items[3].HasDetail,
            "A rescan did not reuse cached details.");
        File.SetLastWriteTimeUtc(browser.Items[0].Path, DateTime.UtcNow.AddMinutes(1));
        await browser.ScanAsync();
        Check(!browser.Items[0].HasDetail && browser.Items[2].HasDetail, "A changed file reused stale cached details.");
        _ = browser.Items[0].Detail;
        await DetailsOf(browser.Items[0]);
        var beforeRefresh = browser.Items;
        browser.RefreshCommand.Execute(null);
        for (var attempt = 0; attempt < 300 && (ReferenceEquals(beforeRefresh, browser.Items) || browser.IsScanning); attempt++)
            await Task.Delay(10);
        Check(browser.Items.Count == 4 && browser.Items.All(item => !item.HasDetail), "Refresh did not clear cached details.");

        var reloaded = UserSettingsStore.Load(settingsPath);
        Check(reloaded.BrowserFolder == Path.GetFullPath(folder) && reloaded.BrowserIncludeSubfolders
              && reloaded.FfmpegPath == @"C:\tools\ffmpeg.exe",
            "Browser folder and subfolder choice were not remembered beside the FFmpeg path.");

        Check(!browser.AddToSceneCommand.CanExecute(Array.Empty<AssetBrowserItem>()), "Add to scene was enabled without a selection.");
        Check(!browser.OpenCommand.CanExecute(new[] { browser.Items[0], browser.Items[1] }), "Open was enabled for several files.");
        browser.AddToSceneCommand.Execute(new[] { browser.Items[0], browser.Items[1] });
        for (var attempt = 0; attempt < 300 && shell.SceneLayers.Count < 2; attempt++)
            await Task.Delay(10);
        Check(shell.SceneLayers.Count == 2 && shell.State == WorkspaceState.Ready,
            "Adding two browser files with no open asset did not open one and add the other as a layer.");
        Check(shell.SceneLayers[0].SkeletonPath == browser.Items[0].Path && shell.SceneLayers[1].SkeletonPath == browser.Items[1].Path,
            "Browser files were not added in selection order.");

        browser.OpenCommand.Execute(browser.Items[3]);
        for (var attempt = 0; attempt < 300 && shell.SceneLayers.Count != 1; attempt++)
            await Task.Delay(10);
        Check(shell.SceneLayers.Count == 1 && shell.SceneLayers[0].SkeletonPath == browser.Items[3].Path,
            "Open did not replace the workspace with the chosen file.");

        using var restarted = new ShellViewModel(WorkspaceState.Empty, true, store, _ => null, service,
            userSettings: UserSettingsStore.Load(settingsPath));
        Check(restarted.AssetBrowser.Folder == Path.GetFullPath(folder) && restarted.AssetBrowser.IncludeSubfolders,
            "A new session did not restore the browser folder.");
        restarted.IsAssetBrowserActive = true;
        for (var attempt = 0; attempt < 300 && restarted.AssetBrowser.Items.Count < 4; attempt++)
            await Task.Delay(10);
        Check(restarted.AssetBrowser.Items.Count == 4, "Showing the browser did not scan the remembered folder.");
        Console.WriteLine("TASK-068: folder asset browser passed.");
    }

    // Waits until each item has details (requested earlier by reading Detail).
    private static async Task DetailsOf(params AssetBrowserItem[] items)
    {
        for (var attempt = 0; attempt < 1000 && !items.All(item => item.HasDetail); attempt++)
            await Task.Delay(10);
        Check(items.All(item => item.HasDetail), "Requested browser details did not load.");
    }

    private static bool CreateJunction(string link, string target)
    {
        try
        {
            using var process = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe")
            {
                ArgumentList = { "/c", "mklink", "/J", link, target },
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            })!;
            process.WaitForExit(10000);
            return process.ExitCode == 0 && Directory.Exists(link);
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }

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
}
