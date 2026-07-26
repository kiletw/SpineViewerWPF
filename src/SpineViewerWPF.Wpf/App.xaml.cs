using Microsoft.Win32;
using SpineRuntime.V41;
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

        string? ChooseAssetPath()
            => ChooseAssetPaths()?.FirstOrDefault();

        IReadOnlyList<string>? ChooseAssetPaths()
        {
            var dialog = new OpenFileDialog
            {
                Title = (string)FindResource("Text.OpenAssetTitle"),
                Filter = (string)FindResource("Text.AssetFilter"),
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                CheckFileExists = true,
                Multiselect = true
            };
            return dialog.ShowDialog() == true ? dialog.FileNames : null;
        }

        string? ChooseProjectPath(string suggestedPath)
        {
            var dialog = new SaveFileDialog
            {
                Title = (string)FindResource("Text.SaveProjectTitle"),
                Filter = (string)FindResource("Text.ProjectFilter"),
                DefaultExt = ViewerProjectStore.Extension,
                AddExtension = true,
                FileName = suggestedPath
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        string? ChooseScreenshotPath()
        {
            var dialog = new SaveFileDialog
            {
                Title = (string)FindResource("Text.ScreenshotTitle"),
                Filter = (string)FindResource("Text.PngFilter"),
                DefaultExt = ".png",
                AddExtension = true,
                FileName = "spine-frame.png"
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        string? ChooseExportPath()
        {
            var dialog = new SaveFileDialog
            {
                Title = (string)FindResource("Text.ExportTitle"),
                Filter = (string)FindResource("Text.ExportPngFilter"),
                DefaultExt = ".png",
                AddExtension = true,
                OverwritePrompt = true,
                FileName = "spine-animation.png"
            };
            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }

        bool ConfirmDiscardChanges() => MessageBox.Show(
            (string)FindResource("Text.DiscardPrompt"),
            (string)FindResource("Text.UnsavedTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning) == MessageBoxResult.Yes;

        MainWindow = new MainWindow
        {
            DataContext = new ShellViewModel(
                state,
                !e.Args.Contains("--compact", StringComparer.OrdinalIgnoreCase),
                new ViewerProjectStore(),
                ChooseProjectPath,
                new AssetService(new SpineV41Adapter()),
                ChooseAssetPath,
                ConfirmDiscardChanges,
                chooseScreenshotPath: ChooseScreenshotPath,
                chooseExportPath: ChooseExportPath,
                chooseAssetPaths: ChooseAssetPaths)
        };
        MainWindow.Show();
    }
}
