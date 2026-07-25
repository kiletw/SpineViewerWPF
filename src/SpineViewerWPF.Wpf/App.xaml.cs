using System.Windows;

namespace SpineViewerWPF.Wpf;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var state = WorkspaceState.Empty;
        var stateArgument = e.Args.FirstOrDefault(x => x.StartsWith("--state=", StringComparison.OrdinalIgnoreCase));
        if (stateArgument is not null)
            Enum.TryParse(stateArgument["--state=".Length..], true, out state);

        MainWindow = new MainWindow
        {
            DataContext = new ShellViewModel(state, !e.Args.Contains("--compact", StringComparer.OrdinalIgnoreCase))
        };
        MainWindow.Show();
    }
}
