using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using SpineViewerWPF.Core;

namespace SpineViewerWPF.Wpf;

public partial class MainWindow : Window
{
    public static RoutedUICommand FloatInspectorCommand { get; } = new(
        "Float Inspector",
        nameof(FloatInspectorCommand),
        typeof(MainWindow));

    private Point? viewportDragStart;
    private FloatingPanelWindow? browsePanelWindow;
    private FloatingPanelWindow? inspectorPanelWindow;
    private bool isClosing;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += MainWindowDataContextChanged;
        Closing += ConfirmUnsavedChanges;
        Closed += MainWindowClosed;
    }

    private void ExecuteFloatInspectorCommand(object sender, ExecutedRoutedEventArgs e) =>
        FloatInspectorPanel(sender, e);

    private void MainWindowDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is ShellViewModel oldViewModel)
            oldViewModel.PropertyChanged -= ViewModelPropertyChanged;
        if (e.NewValue is ShellViewModel viewModel)
        {
            viewModel.PropertyChanged += ViewModelPropertyChanged;
            ApplyTheme(viewModel.ThemeMode);
        }
    }

    private void ViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ShellViewModel.ThemeMode) or "")
            ApplyTheme((sender as ShellViewModel)?.ThemeMode ?? "Dark");
    }

    private void ApplyTheme(string mode)
    {
        var light = string.Equals(mode, "Light", StringComparison.OrdinalIgnoreCase);
        SetBrush("WindowBrush", light ? "#F5F6F8" : "#101217");
        SetBrush("TextBrush", light ? "#1F2937" : "#E8ECF3");
        SetBrush("ControlBrush", light ? "#FFFFFF" : "#232832");
        SetBrush("ControlHoverBrush", light ? "#E9EDF2" : "#2D3541");
        SetBrush("ControlPressedBrush", light ? "#DCE2E8" : "#374250");
        SetBrush("ControlBorderBrush", light ? "#B6C0CB" : "#3B4654");
        SetBrush("PanelBrush", light ? "#ECEFF3" : "#171A20");
        SetBrush("PanelRaisedBrush", light ? "#E3E7EC" : "#1C2027");
        SetBrush("BorderBrush", light ? "#C9D0D8" : "#2A313B");
        SetBrush("MutedBrush", light ? "#596676" : "#929BAD");
        SetBrush("AccentBrush", light ? "#197E6B" : "#58C7AD");
        SetBrush("SelectionBrush", light ? "#D3E7E1" : "#25483F");
        SetBrush("SelectionHoverBrush", light ? "#C4DED7" : "#2E5A4F");
        SetBrush("SelectionTextBrush", light ? "#12332D" : "#FFFFFF");
        SetBrush("StatusBrush", light ? "#E1E7ED" : "#101218");
        Background = TryFindResource("WindowBrush") as Brush;
        Foreground = TryFindResource("TextBrush") as Brush;
    }

    private void SetBrush(string key, string color)
    {
        var value = (Color)ColorConverter.ConvertFromString(color);
        if (TryFindResource(key) is not SolidColorBrush brush) return;
        if (brush.IsFrozen)
        {
            Resources[key] = new SolidColorBrush(value);
            return;
        }
        brush.Color = value;
    }

    private void WindowLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ShellViewModel viewModel)
        {
            ApplyTheme(viewModel.ThemeMode);
            Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
            {
                if (ViewportSurface.ActualWidth > 0 && ViewportSurface.ActualHeight > 0)
                {
                    var dpi = VisualTreeHelper.GetDpi(ViewportSurface);
                    viewModel.SetViewportSize(
                        ViewportSurface.ActualWidth * dpi.DpiScaleX,
                        ViewportSurface.ActualHeight * dpi.DpiScaleY);
                }
            }));
        }
    }

    private void WindowDragOver(object sender, DragEventArgs e)
    {
        var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
        var canOpen = paths?.Any(path => IsSkeletonPath(path)) == true;
        e.Effects = canOpen ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void WindowDrop(object sender, DragEventArgs e)
    {
        var paths = e.Data.GetData(DataFormats.FileDrop) as string[];
        var path = paths?.FirstOrDefault(IsSkeletonPath);
        var atlas = paths?.FirstOrDefault(IsAtlasPath);
        if (path is not null && DataContext is ShellViewModel viewModel)
            await viewModel.OpenAssetAsync(path, atlas);
        e.Handled = true;
    }

    private static bool IsSkeletonPath(string path) =>
        string.Equals(Path.GetExtension(path), ".json", StringComparison.OrdinalIgnoreCase)
        || string.Equals(Path.GetExtension(path), ".skel", StringComparison.OrdinalIgnoreCase);

    private static bool IsAtlasPath(string path) =>
        string.Equals(Path.GetExtension(path), ".atlas", StringComparison.OrdinalIgnoreCase);

    private void SceneLayerListPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox list || e.OriginalSource is not DependencyObject source) return;
        if (ItemsControl.ContainerFromElement(list, source) is ListBoxItem item)
            item.IsSelected = true;
    }

    private void NumericTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || sender is not TextBox textBox) return;
        textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
        e.Handled = true;
    }

    private void ToggleBrowsePanel(object sender, RoutedEventArgs e)
    {
        if (browsePanelWindow is not null)
            DockBrowsePanel();

        if (DataContext is not ShellViewModel viewModel) return;
        var show = !viewModel.IsBrowsePanelVisible || !viewModel.IsRailExpanded;
        viewModel.IsBrowsePanelVisible = show;
        viewModel.IsRailExpanded = show;
    }

    private void ToggleLayersPanel(object sender, RoutedEventArgs e)
    {
        if (browsePanelWindow is { } floating)
        {
            floating.Activate();
            return;
        }

        if (DataContext is not ShellViewModel viewModel) return;
        var show = !viewModel.IsBrowsePanelVisible || !viewModel.IsRailExpanded;
        viewModel.IsBrowsePanelVisible = show;
        viewModel.IsRailExpanded = show;
    }

    private void ToggleInspectorPanel(object sender, RoutedEventArgs e)
    {
        if (inspectorPanelWindow is not null)
            DockInspectorPanel();

        if (DataContext is ShellViewModel viewModel)
            viewModel.ToggleInspectorCommand.Execute(null);
    }

    private void FloatBrowsePanel(object sender, RoutedEventArgs e)
    {
        if (browsePanelWindow is { } existing)
        {
            existing.Activate();
            return;
        }

        if (DataContext is ShellViewModel viewModel)
        {
            viewModel.IsBrowsePanelVisible = false;
            viewModel.IsRailExpanded = false;
        }

        DetachPanel(BrowsePanelContent);
        var window = CreatePanelWindow(GetResourceText("Text.FloatBrowse"), BrowsePanelContent, 320, 720);
        browsePanelWindow = window;
        window.Closed += (_, _) =>
        {
            if (!ReferenceEquals(browsePanelWindow, window)) return;
            browsePanelWindow = null;
            if (!isClosing) DockBrowsePanel();
        };
        window.Show();
    }

    private void FloatInspectorPanel(object sender, RoutedEventArgs e)
    {
        if (inspectorPanelWindow is { } existing)
        {
            existing.Activate();
            return;
        }

        if (DataContext is ShellViewModel viewModel)
            viewModel.IsInspectorVisible = true;

        DetachPanel(InspectorPanel);
        var window = CreatePanelWindow(GetResourceText("Text.FloatInspector"), InspectorPanel, 320, 720);
        inspectorPanelWindow = window;
        window.Closed += (_, _) =>
        {
            if (!ReferenceEquals(inspectorPanelWindow, window)) return;
            inspectorPanelWindow = null;
            if (!isClosing) DockInspectorPanel();
        };
        window.Show();
    }

    private void RedockPanels(object sender, RoutedEventArgs e)
    {
        DockBrowsePanel();
        DockInspectorPanel();
    }

    private void ResetWorkspaceLayout(object sender, RoutedEventArgs e)
    {
        DockBrowsePanel();
        DockInspectorPanel();
        if (DataContext is ShellViewModel viewModel)
        {
            viewModel.IsBrowsePanelVisible = true;
            viewModel.IsRailExpanded = true;
            viewModel.IsInspectorVisible = true;
        }
    }

    private void DockBrowsePanel()
    {
        if (browsePanelWindow is { } window)
        {
            browsePanelWindow = null;
            window.Content = null;
            window.Close();
        }

        if (BrowsePanelContent.Parent is ContentControl content)
            content.Content = null;
        if (!BrowsePanelGrid.Children.Contains(BrowsePanelContent))
            BrowsePanelGrid.Children.Add(BrowsePanelContent);
        Grid.SetColumn(BrowsePanelContent, 1);
        if (DataContext is ShellViewModel viewModel)
        {
            viewModel.IsBrowsePanelVisible = true;
            viewModel.IsRailExpanded = true;
        }
    }

    private void DockInspectorPanel()
    {
        if (inspectorPanelWindow is { } window)
        {
            inspectorPanelWindow = null;
            window.Content = null;
            window.Close();
        }

        if (InspectorPanel.Parent is ContentControl content)
            content.Content = null;
        if (!WorkspaceGrid.Children.Contains(InspectorPanel))
            WorkspaceGrid.Children.Add(InspectorPanel);
        Grid.SetColumn(InspectorPanel, 2);
        if (DataContext is ShellViewModel viewModel)
            viewModel.IsInspectorVisible = true;
    }

    private static void DetachPanel(FrameworkElement panel)
    {
        if (panel.Parent is Panel parent)
            parent.Children.Remove(panel);
        else if (panel.Parent is ContentControl content)
            content.Content = null;
    }

    private FloatingPanelWindow CreatePanelWindow(string title, UIElement content, double width, double height) =>
        new(title, content, this, width, height);

    private string GetResourceText(string key) =>
        TryFindResource(key) as string ?? key;

    private void MainWindowClosed(object? sender, EventArgs e)
    {
        isClosing = true;
        browsePanelWindow?.Close();
        inspectorPanelWindow?.Close();
        (DataContext as IDisposable)?.Dispose();
    }

    private void ViewportMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is not ShellViewModel viewModel || !Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) return;
        viewModel.ZoomViewport(e.Delta > 0 ? 1.1 : 1 / 1.1);
        e.Handled = true;
    }

    private void ViewportSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not Visual visual || DataContext is not ShellViewModel viewModel) return;
        var dpi = VisualTreeHelper.GetDpi(visual);
        viewModel.SetViewportSize(e.NewSize.Width * dpi.DpiScaleX, e.NewSize.Height * dpi.DpiScaleY);
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

internal sealed class FloatingPanelWindow : Window
{
    public FloatingPanelWindow(string title, UIElement content, Window owner, double width, double height)
    {
        Title = title;
        Content = content;
        Owner = owner;
        DataContext = owner.DataContext;
        Width = width;
        Height = height;
        MinWidth = 260;
        MinHeight = 360;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ResizeMode = ResizeMode.CanResizeWithGrip;
        ShowInTaskbar = false;
        Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(13, 15, 20));
    }
}

public sealed class PreviewFrameConverter : IMultiValueConverter
{
    public object? Convert(object[] values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.FirstOrDefault() is not RenderedFrame frame) return null;
        return PreviewFrameBitmap.Create(frame, values.ElementAtOrDefault(1) as string ?? "RGBA");
    }

    public object[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

internal static class PreviewFrameBitmap
{
    public static BitmapSource Create(RenderedFrame frame, string channel)
    {
        var expectedLength = checked(frame.Width * frame.Height * 4);
        if (frame.Width < 1 || frame.Height < 1 || frame.Bgra32.Length != expectedLength)
            throw new InvalidDataException("The interactive frame has invalid BGRA dimensions.");

        var pixels = frame.Bgra32;
        if (!string.Equals(channel, "RGBA", StringComparison.Ordinal))
        {
            pixels = (byte[])pixels.Clone();
            for (var offset = 0; offset < pixels.Length; offset += 4)
            {
                var alpha = pixels[offset + 3];
                if (string.Equals(channel, "Alpha", StringComparison.Ordinal))
                    pixels[offset] = pixels[offset + 1] = pixels[offset + 2] = alpha;
                pixels[offset + 3] = 255;
            }
        }

        var bitmap = BitmapSource.Create(
            frame.Width,
            frame.Height,
            96,
            96,
            PixelFormats.Bgra32,
            null,
            pixels,
            frame.Width * 4);
        bitmap.Freeze();
        return bitmap;
    }

    public static void SavePng(RenderedFrame frame, string channel, string path)
    {
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(Create(frame, channel)));
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        encoder.Save(stream);
    }
}

public sealed class BooleanScaleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? -1d : 1d;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
