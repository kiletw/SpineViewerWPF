using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace SpineViewerWPF.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += ConfirmUnsavedChanges;
        Closed += (_, _) => (DataContext as IDisposable)?.Dispose();
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

public sealed class PreviewImageConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || !File.Exists(path)) return null;
        using var stream = File.OpenRead(path);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.StreamSource = stream;
        image.EndInit();
        image.Freeze();
        return image;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
