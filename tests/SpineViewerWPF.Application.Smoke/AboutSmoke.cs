using SpineViewerWPF.Application;
using SpineViewerWPF.Wpf;

// TASK-076: Help > About facts.
static class AboutSmoke
{
    public static void Run(AssetService service, ViewerProjectStore store)
    {
        Check(AboutInfo.DisplayVersion("3.0.0-alpha.1+0123abcd") == "3.0.0-alpha.1"
              && AboutInfo.DisplayVersion("3.0.0-dev") == "3.0.0-dev"
              && AboutInfo.DisplayVersion(null) == "unknown",
            "About did not strip build metadata from the version.");

        using var shell = new ShellViewModel(WorkspaceState.Empty, true, store, _ => null, service);
        var info = shell.CreateAboutInfo();
        Check(info.RuntimeLines.Count == 16, $"About listed {info.RuntimeLines.Count} Runtime lines, expected 16.");
        Check(info.RuntimeLines.First() == "2.1.08" && info.RuntimeLines.Last() == "4.3"
              && info.RuntimeLines.ToList().IndexOf("4.0.31") < info.RuntimeLines.ToList().IndexOf("4.0")
              && info.RuntimeLines.ToList().IndexOf("3.2.xx") < info.RuntimeLines.ToList().IndexOf("3.4.02"),
            $"About Runtime lines were not in version order: {string.Join(", ", info.RuntimeLines)}");
        Check(!string.IsNullOrWhiteSpace(info.Version) && !info.Version.Contains('+'), $"About version was '{info.Version}'.");
        Check(info.CopyText.StartsWith($"SpineViewerWPF {info.Version}", StringComparison.Ordinal)
              && info.CopyText.Contains("Spine Runtimes: 2.1.08", StringComparison.Ordinal)
              && info.CopyText.Contains("Renderer: ", StringComparison.Ordinal),
            "About copy text was incomplete.");
        Console.WriteLine("TASK-076: About information passed.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
