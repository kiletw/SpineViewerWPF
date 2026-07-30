using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace SpineViewerWPF.Wpf;

public partial class MainWindow : Window
{
    private Point? viewportDragStart;
    private FloatingPanelWindow? browsePanelWindow;
    private FloatingPanelWindow? inspectorPanelWindow;
    private bool isClosing;

    public MainWindow()
    {
        InitializeComponent();
        Closing += ConfirmUnsavedChanges;
        Closed += MainWindowClosed;
    }

    private void OpenWindowMenu(object sender, RoutedEventArgs e)
    {
        if (sender is not FrameworkElement element || element.ContextMenu is not { } menu) return;
        menu.PlacementTarget = element;
        menu.IsOpen = true;
    }

    private void ToggleBrowsePanel(object sender, RoutedEventArgs e)
    {
        if (browsePanelWindow is not null)
            DockBrowsePanel();

        if (DataContext is ShellViewModel viewModel)
            viewModel.ToggleBrowsePanelCommand.Execute(null);
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
            viewModel.IsBrowsePanelVisible = true;
            viewModel.IsRailExpanded = true;
        }

        DetachPanel(BrowsePanel);
        var window = CreatePanelWindow(GetResourceText("Text.FloatBrowse"), BrowsePanel, 320, 720);
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

        if (BrowsePanel.Parent is ContentControl content)
            content.Content = null;
        if (!WorkspaceGrid.Children.Contains(BrowsePanel))
            WorkspaceGrid.Children.Add(BrowsePanel);
        Grid.SetColumn(BrowsePanel, 0);
        if (DataContext is ShellViewModel viewModel)
            viewModel.IsBrowsePanelVisible = true;
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

public sealed class BooleanScaleConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true ? -1d : 1d;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
