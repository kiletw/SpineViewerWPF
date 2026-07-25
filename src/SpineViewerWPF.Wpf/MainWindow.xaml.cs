using System.ComponentModel;
using System.Windows;

namespace SpineViewerWPF.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += ConfirmUnsavedChanges;
    }

    private void ConfirmUnsavedChanges(object? sender, CancelEventArgs e)
    {
        if (DataContext is not ShellViewModel { IsDirty: true } viewModel) return;

        var result = MessageBox.Show(
            (string)System.Windows.Application.Current.FindResource("Text.UnsavedPrompt"),
            (string)System.Windows.Application.Current.FindResource("Text.UnsavedTitle"),
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Warning);
        if (result == MessageBoxResult.Cancel || result == MessageBoxResult.Yes && !viewModel.TrySave())
            e.Cancel = true;
    }
}
