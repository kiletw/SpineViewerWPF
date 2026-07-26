using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SpineViewerWPF.Wpf;

public partial class MainWindow : Window
{
    private Point? viewportDragStart;

    public MainWindow()
    {
        InitializeComponent();
        Closing += ConfirmUnsavedChanges;
        Closed += (_, _) => (DataContext as IDisposable)?.Dispose();
    }

    private void ViewportMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is not ShellViewModel viewModel || !Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) return;
        viewModel.ZoomViewport(e.Delta > 0 ? 1.1 : 1 / 1.1);
        e.Handled = true;
    }

    private void ViewportMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) return;
        viewportDragStart = e.GetPosition((IInputElement)sender);
        ((UIElement)sender).CaptureMouse();
        e.Handled = true;
    }

    private void ViewportMouseMove(object sender, MouseEventArgs e)
    {
        if (viewportDragStart is not Point start || e.LeftButton != MouseButtonState.Pressed ||
            DataContext is not ShellViewModel viewModel) return;
        var current = e.GetPosition((IInputElement)sender);
        viewModel.PanViewport(current.X - start.X, current.Y - start.Y);
        viewportDragStart = current;
        e.Handled = true;
    }

    private void ViewportMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (viewportDragStart is null) return;
        viewportDragStart = null;
        ((UIElement)sender).ReleaseMouseCapture();
        e.Handled = true;
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
