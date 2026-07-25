using Microsoft.Win32;
using SpineViewerWPF.Application;
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

        string? ChooseProjectPath()
        {
            var dialog = new SaveFileDialog
            {
                Title = (string)FindResource("Text.SaveProjectTitle"),
                Filter = (string)FindResource("Text.ProjectFilter"),
                DefaultExt = ViewerProjectStore.Extension,
                AddExtension = true,
                FileName = $"hero{ViewerProjectStore.Extension}"
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        MainWindow = new MainWindow
        {
            DataContext = new ShellViewModel(
                state,
                !e.Args.Contains("--compact", StringComparer.OrdinalIgnoreCase),
                new ViewerProjectStore(),
                ChooseProjectPath)
        };
        MainWindow.Show();
    }
}
