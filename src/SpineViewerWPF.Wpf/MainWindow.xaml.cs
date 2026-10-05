using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
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

    private const double PanStartThreshold = 3;

    private Point? viewportPressPoint;
    private MouseButton viewportPressButton;
    private bool viewportPanning;
    private string? appliedTheme;
    private FloatingPanelWindow? browsePanelWindow;
    private FloatingPanelWindow? inspectorPanelWindow;
    private bool isClosing;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += MainWindowDataContextChanged;
        SourceInitialized += (_, _) => ApplyNativeTitleBar(this);
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
        if (e.PropertyName is not (nameof(ShellViewModel.ThemeMode) or "" or null)) return;
        var mode = (sender as ShellViewModel)?.ThemeMode ?? "Dark";
        // Blanket notifications arrive often; re-theme only when the mode changed.
        if (string.Equals(mode, appliedTheme, StringComparison.Ordinal)) return;
        ApplyTheme(mode);
    }

    private void ApplyTheme(string mode)
    {
        appliedTheme = mode;
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
        SetBrush("AccentForegroundBrush", light ? "#FFFFFF" : "#07120F");
        SetBrush("SelectionBrush", light ? "#D3E7E1" : "#25483F");
        SetBrush("SelectionHoverBrush", light ? "#C4DED7" : "#2E5A4F");
        SetBrush("SelectionTextBrush", light ? "#12332D" : "#FFFFFF");
        SetBrush("StatusBrush", light ? "#E1E7ED" : "#101218");
        Background = TryFindResource("WindowBrush") as Brush;
        Foreground = TryFindResource("TextBrush") as Brush;
        ApplyNativeTitleBar(this);
        if (browsePanelWindow is not null) ApplyNativeTitleBar(browsePanelWindow);
        if (inspectorPanelWindow is not null) ApplyNativeTitleBar(inspectorPanelWindow);
    }

    // TASK-060: keep the native caption but match it to the active theme.
    private void ApplyNativeTitleBar(Window window)
    {
        var light = string.Equals(appliedTheme, "Light", StringComparison.OrdinalIgnoreCase);
        var caption = (TryFindResource("PanelRaisedBrush") as SolidColorBrush)?.Color;
        var text = (TryFindResource("TextBrush") as SolidColorBrush)?.Color;
        NativeTitleBar.Apply(window, !light, caption, text);
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
        window.SourceInitialized += (_, _) => ApplyNativeTitleBar(window);
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
        window.SourceInitialized += (_, _) => ApplyNativeTitleBar(window);
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

    // TASK-060: unmodified Space/F must not fire while the user types into a
    // text field, so they are handled here instead of as window KeyBindings.
    private void WindowKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Handled || Keyboard.Modifiers != ModifierKeys.None || DataContext is not ShellViewModel viewModel) return;
        if (IsTextEntry(Keyboard.FocusedElement as DependencyObject)) return;
        ICommand? command = e.Key switch
        {
            Key.Space => viewModel.TogglePlayCommand,
            Key.F => viewModel.FitCommand,
            _ => null
        };
        if (command is null || !command.CanExecute(null)) return;
        command.Execute(null);
        e.Handled = true;
    }

    // TASK-062: View menu radio-style choices for channel, background, and theme.
    private void SelectViewOption(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { Tag: string tag } || DataContext is not ShellViewModel viewModel) return;
        var separator = tag.IndexOf(':');
        if (separator <= 0) return;
        var value = tag[(separator + 1)..];
        switch (tag[..separator])
        {
            case "Channel":
                viewModel.PreviewChannel = value;
                break;
            case "Background":
                viewModel.BackgroundMode = value;
                break;
            case "Theme":
                viewModel.ThemeMode = value;
                break;
        }
    }

    private static bool IsTextEntry(DependencyObject? element) =>
        element is TextBoxBase or PasswordBox || element is ComboBox { IsEditable: true };

    private void ViewportMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (DataContext is not ShellViewModel viewModel || sender is not FrameworkElement surface ||
            IsWithinViewportControl(e.OriginalSource as DependencyObject, surface)) return;
        var position = e.GetPosition(surface);
        viewModel.ZoomViewportAt(
            Math.Pow(1.1, e.Delta / 120.0),
            position.X - surface.ActualWidth / 2,
            position.Y - surface.ActualHeight / 2);
        e.Handled = true;
    }

    private void ViewportSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is not Visual visual || DataContext is not ShellViewModel viewModel) return;
        var dpi = VisualTreeHelper.GetDpi(visual);
        viewModel.SetViewportSize(e.NewSize.Width * dpi.DpiScaleX, e.NewSize.Height * dpi.DpiScaleY);
    }

    // TASK-060: left-drag or middle-drag pans without a modifier; a left
    // double-click fits. Presses that start on overlay controls are ignored.
    private void ViewportMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton is not (MouseButton.Left or MouseButton.Middle)
            || sender is not FrameworkElement surface
            || IsWithinViewportControl(e.OriginalSource as DependencyObject, surface))
            return;

        if (e.ChangedButton == MouseButton.Left && e.ClickCount == 2)
        {
            EndViewportPan(surface);
            if (DataContext is ShellViewModel viewModel && viewModel.FitCommand.CanExecute(null))
                viewModel.FitCommand.Execute(null);
            e.Handled = true;
            return;
        }

        viewportPressPoint = e.GetPosition(surface);
        viewportPressButton = e.ChangedButton;
        viewportPanning = false;
        if (e.ChangedButton == MouseButton.Middle)
        {
            BeginViewportPan(surface);
            e.Handled = true;
        }
    }

    private void ViewportMouseMove(object sender, MouseEventArgs e)
    {
        if (viewportPressPoint is not Point start || sender is not FrameworkElement surface ||
            DataContext is not ShellViewModel viewModel) return;
        var state = viewportPressButton == MouseButton.Middle ? e.MiddleButton : e.LeftButton;
        if (state != MouseButtonState.Pressed)
        {
            EndViewportPan(surface);
            return;
        }

        var current = e.GetPosition(surface);
        if (!viewportPanning)
        {
            if (Math.Abs(current.X - start.X) < PanStartThreshold &&
                Math.Abs(current.Y - start.Y) < PanStartThreshold) return;
            BeginViewportPan(surface);
        }
        viewModel.PanViewport(current.X - start.X, current.Y - start.Y);
        viewportPressPoint = current;
        e.Handled = true;
    }

    private void ViewportMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (viewportPressPoint is null || e.ChangedButton != viewportPressButton ||
            sender is not FrameworkElement surface) return;
        var wasPanning = viewportPanning;
        EndViewportPan(surface);
        if (wasPanning) e.Handled = true;
    }

    private void BeginViewportPan(FrameworkElement surface)
    {
        viewportPanning = true;
        surface.CaptureMouse();
        surface.Cursor = Cursors.SizeAll;
    }

    private void EndViewportPan(FrameworkElement surface)
    {
        viewportPressPoint = null;
        if (!viewportPanning) return;
        viewportPanning = false;
        surface.ReleaseMouseCapture();
        surface.ClearValue(CursorProperty);
    }

    private static bool IsWithinViewportControl(DependencyObject? source, DependencyObject surface)
    {
        for (var node = source; node is not null; node = GetParent(node))
        {
            if (ReferenceEquals(node, surface)) return false;
            if (node is ButtonBase or TextBoxBase or Thumb or ScrollBar or ComboBox or ComboBoxItem)
                return true;
        }
        // Combo box drop-downs live in a popup outside the viewport's visual tree.
        return true;
    }

    private static DependencyObject? GetParent(DependencyObject node) =>
        node is Visual or System.Windows.Media.Media3D.Visual3D
            ? VisualTreeHelper.GetParent(node)
            : LogicalTreeHelper.GetParent(node);

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
        // Share the owner's live theme brush so Light/Dark switches reach floating panels.
        Background = owner.TryFindResource("WindowBrush") as Brush
            ?? new SolidColorBrush(Color.FromRgb(13, 15, 20));
    }
}

// TASK-060: native title-bar theming through DWM. Unsupported Windows builds
// ignore the attributes and keep the default caption.
internal static class NativeTitleBar
{
    private const int UseImmersiveDarkModeBefore20H1 = 19;
    private const int UseImmersiveDarkMode = 20;
    private const int CaptionColor = 35;
    private const int TextColor = 36;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    public static void Apply(Window window, bool dark, Color? caption, Color? text)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero) return;
        try
        {
            var enabled = dark ? 1 : 0;
            if (DwmSetWindowAttribute(handle, UseImmersiveDarkMode, ref enabled, sizeof(int)) != 0)
                DwmSetWindowAttribute(handle, UseImmersiveDarkModeBefore20H1, ref enabled, sizeof(int));
            if (caption is { } captionColor)
            {
                var value = ToColorRef(captionColor);
                DwmSetWindowAttribute(handle, CaptionColor, ref value, sizeof(int));
            }
            if (text is { } textColor)
            {
                var value = ToColorRef(textColor);
                DwmSetWindowAttribute(handle, TextColor, ref value, sizeof(int));
            }
        }
        catch (DllNotFoundException)
        {
        }
        catch (EntryPointNotFoundException)
        {
        }
    }

    private static int ToColorRef(Color color) => color.R | color.G << 8 | color.B << 16;
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

public sealed class StringEqualsConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.Equals(value as string, parameter as string, StringComparison.Ordinal);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class BooleanScaleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? -1d : 1d;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
