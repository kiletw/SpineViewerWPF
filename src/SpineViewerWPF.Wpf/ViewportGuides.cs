using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace SpineViewerWPF.Wpf;

// TASK-071: viewport guides drawn over the preview in scene space (SceneCamera):
// the scene X/Y axes through the scene origin, the selected layer's origin, and
// the fixed-size export frame centered on the scene origin. Guides are WPF
// visuals only; they are never part of Screenshot or export output.
public sealed class ViewportGuides : FrameworkElement
{
    public static readonly DependencyProperty ZoomProperty = Register(nameof(Zoom), 1d);
    public static readonly DependencyProperty PanXProperty = Register(nameof(PanX), 0d);
    public static readonly DependencyProperty PanYProperty = Register(nameof(PanY), 0d);
    public static readonly DependencyProperty ShowAxesProperty = Register(nameof(ShowAxes), false);
    public static readonly DependencyProperty ShowOriginProperty = Register(nameof(ShowOrigin), false);
    public static readonly DependencyProperty OriginXProperty = Register(nameof(OriginX), 0d);
    public static readonly DependencyProperty OriginYProperty = Register(nameof(OriginY), 0d);
    public static readonly DependencyProperty ShowFrameProperty = Register(nameof(ShowFrame), false);
    public static readonly DependencyProperty FrameWidthProperty = Register(nameof(FrameWidth), 0);
    public static readonly DependencyProperty FrameHeightProperty = Register(nameof(FrameHeight), 0);

    private static readonly Pen XAxisPen = FrozenPen(Color.FromArgb(170, 0xE0, 0x5A, 0x5A), 1);
    private static readonly Pen YAxisPen = FrozenPen(Color.FromArgb(170, 0x5A, 0xC8, 0x6A), 1);
    private static readonly Pen OriginPen = FrozenPen(Color.FromArgb(220, 0xF3, 0xC9, 0x69), 1.5);
    private static readonly Pen FramePen = FrozenPen(Color.FromArgb(230, 0x58, 0xC7, 0xAD), 1);
    private static readonly Brush FrameShade = Frozen(new SolidColorBrush(Color.FromArgb(90, 0, 0, 0)));
    private static readonly Brush LabelBrush = Frozen(new SolidColorBrush(Color.FromArgb(230, 0x58, 0xC7, 0xAD)));

    public ViewportGuides()
    {
        IsHitTestVisible = false;
        Focusable = false;
        SnapsToDevicePixels = true;
    }

    public double Zoom { get => (double)GetValue(ZoomProperty); set => SetValue(ZoomProperty, value); }
    public double PanX { get => (double)GetValue(PanXProperty); set => SetValue(PanXProperty, value); }
    public double PanY { get => (double)GetValue(PanYProperty); set => SetValue(PanYProperty, value); }
    public bool ShowAxes { get => (bool)GetValue(ShowAxesProperty); set => SetValue(ShowAxesProperty, value); }
    public bool ShowOrigin { get => (bool)GetValue(ShowOriginProperty); set => SetValue(ShowOriginProperty, value); }
    public double OriginX { get => (double)GetValue(OriginXProperty); set => SetValue(OriginXProperty, value); }
    public double OriginY { get => (double)GetValue(OriginYProperty); set => SetValue(OriginYProperty, value); }
    public bool ShowFrame { get => (bool)GetValue(ShowFrameProperty); set => SetValue(ShowFrameProperty, value); }
    public int FrameWidth { get => (int)GetValue(FrameWidthProperty); set => SetValue(FrameWidthProperty, value); }
    public int FrameHeight { get => (int)GetValue(FrameHeightProperty); set => SetValue(FrameHeightProperty, value); }

    // Scene point to element coordinates; the element covers the preview content area.
    public Point ToView(double sceneX, double sceneY) =>
        new(ActualWidth / 2 + PanX + sceneX * Zoom, ActualHeight / 2 + PanY + sceneY * Zoom);

    protected override void OnRender(DrawingContext context)
    {
        var width = ActualWidth;
        var height = ActualHeight;
        if (width <= 0 || height <= 0 || !double.IsFinite(Zoom) || Zoom <= 0) return;
        context.PushClip(new RectangleGeometry(new Rect(0, 0, width, height)));
        var origin = ToView(0, 0);

        if (ShowFrame && FrameWidth > 0 && FrameHeight > 0)
        {
            var frame = new Rect(
                origin.X - FrameWidth * Zoom / 2,
                origin.Y - FrameHeight * Zoom / 2,
                FrameWidth * Zoom,
                FrameHeight * Zoom);
            var outside = new CombinedGeometry(
                GeometryCombineMode.Exclude,
                new RectangleGeometry(new Rect(0, 0, width, height)),
                new RectangleGeometry(frame));
            context.DrawGeometry(FrameShade, null, outside);
            context.DrawRectangle(null, FramePen, frame);
            var label = new FormattedText(
                $"{FrameWidth} × {FrameHeight}",
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                11,
                LabelBrush,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);
            context.DrawText(label, new Point(frame.Left + 4, Math.Max(2, frame.Top - label.Height - 2)));
        }

        if (ShowAxes)
        {
            context.DrawLine(XAxisPen, new Point(0, origin.Y), new Point(width, origin.Y));
            context.DrawLine(YAxisPen, new Point(origin.X, 0), new Point(origin.X, height));
        }

        if (ShowOrigin)
        {
            var layer = ToView(OriginX, OriginY);
            context.DrawLine(OriginPen, new Point(layer.X - 6, layer.Y), new Point(layer.X + 6, layer.Y));
            context.DrawLine(OriginPen, new Point(layer.X, layer.Y - 6), new Point(layer.X, layer.Y + 6));
            context.DrawEllipse(null, OriginPen, layer, 3, 3);
        }
        context.Pop();
    }

    private static DependencyProperty Register<T>(string name, T defaultValue) =>
        DependencyProperty.Register(
            name,
            typeof(T),
            typeof(ViewportGuides),
            new FrameworkPropertyMetadata(defaultValue, FrameworkPropertyMetadataOptions.AffectsRender));

    private static Pen FrozenPen(Color color, double thickness)
    {
        var pen = new Pen(new SolidColorBrush(color), thickness);
        pen.Freeze();
        return pen;
    }

    private static Brush Frozen(Brush brush)
    {
        brush.Freeze();
        return brush;
    }
}
