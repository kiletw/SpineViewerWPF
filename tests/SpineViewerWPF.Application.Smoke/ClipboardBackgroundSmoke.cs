using SpineViewerWPF.Application;
using SpineViewerWPF.Core;
using SpineViewerWPF.Wpf;

// TASK-075: Copy Screenshot and the custom background color.
static class ClipboardBackgroundSmoke
{
    public static async Task RunAsync(AssetService service, ViewerProjectStore store, AnimationExportRequest sequenceRequest, string temporaryRoot)
    {
        var root = Path.Combine(temporaryRoot, "clipboard-background");
        Directory.CreateDirectory(root);
        var project = Path.Combine(root, "scene.spineviewer.json");
        var copies = new List<(RenderedFrame Frame, string Channel)>();

        using (var noClipboard = new ShellViewModel(WorkspaceState.Empty, true, store, _ => null, service))
            Check(!noClipboard.CopyScreenshotCommand.CanExecute(null), "Copy Screenshot was available without a clipboard.");

        using var shell = new ShellViewModel(
            WorkspaceState.Empty,
            true,
            store,
            _ => project,
            service,
            chooseAssetPath: () => sequenceRequest.SkeletonPath,
            copyImageToClipboard: (frame, channel) => copies.Add((frame, channel)));
        shell.SetViewportSize(160, 120);
        await shell.OpenAssetAsync(sequenceRequest.SkeletonPath);
        for (var attempt = 0; attempt < 200 && !shell.CopyScreenshotCommand.CanExecute(null); attempt++)
            await Task.Delay(10);
        Check(shell.CopyScreenshotCommand.CanExecute(null), "Copy Screenshot was not available for an open asset.");

        shell.PreviewChannel = "Alpha";
        shell.CopyScreenshotCommand.Execute(null);
        for (var attempt = 0; attempt < 300 && copies.Count == 0; attempt++)
            await Task.Delay(10);
        Check(copies.Count == 1
              && copies[0].Frame.Width == shell.PreviewPixelWidth && copies[0].Frame.Height == shell.PreviewPixelHeight
              && copies[0].Channel == "Alpha"
              && Enumerable.Range(0, copies[0].Frame.Width * copies[0].Frame.Height).Any(index => copies[0].Frame.Bgra32[index * 4 + 3] != 0),
            "Copy Screenshot did not deliver the viewport capture and channel.");
        Check(shell.LastAction == "Copied screenshot to clipboard" && !shell.IsDirty, $"Copy Screenshot reported {shell.LastAction}.");
        for (var attempt = 0; attempt < 200 && !shell.ScreenshotCommand.CanExecute(null); attempt++)
            await Task.Delay(10);
        Check(shell.ScreenshotCommand.CanExecute(null), "Screenshot was not re-enabled after Copy Screenshot.");
        shell.PreviewChannel = "RGBA";

        // Custom background: normalized, undoable, saved, and reloaded.
        Check(shell.BackgroundModes.Contains("Custom") && !shell.IsCustomBackground, "Custom background mode was not offered.");
        shell.BackgroundMode = "Custom";
        shell.BackgroundColor = "12ab34";
        Check(shell.IsCustomBackground && shell.BackgroundColor == "#12AB34" && shell.IsDirty, "A custom background color was not normalized.");
        shell.BackgroundColor = "green";
        Check(shell.BackgroundColor == "#12AB34", "An invalid background color was accepted.");
        shell.UndoCommand.Execute(null);
        Check(shell.BackgroundColor == "#808080" && shell.IsCustomBackground, "Undo did not restore the previous background color.");
        shell.RedoCommand.Execute(null);
        shell.SaveCommand.Execute(null);
        var saved = store.Load(project);
        Check(saved.BackgroundMode == "Custom" && saved.BackgroundColor == "#12AB34", "The custom background was not saved.");

        using (var reopened = new ShellViewModel(WorkspaceState.Empty, true, store, _ => null, service))
        {
            await reopened.OpenProjectAsync(project);
            Check(reopened.BackgroundMode == "Custom" && reopened.BackgroundColor == "#12AB34" && !reopened.IsDirty,
                "A reopened project did not restore the custom background.");
        }

        shell.BackgroundMode = "Dark";
        shell.SaveCommand.Execute(null);
        Check(store.Load(project).BackgroundColor is null, "A non-custom background saved a color.");

        Expect<InvalidDataException>(() => store.Save(project, saved with { BackgroundColor = null }));
        Expect<InvalidDataException>(() => store.Save(project, saved with { BackgroundColor = "#12AB3" }));
        Console.WriteLine("TASK-075: Copy Screenshot and custom background passed.");
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
