using System.Windows;
using System.Windows.Controls;

namespace SpineViewerWPF.Wpf;

// TASK-074: centers each child at its Fraction (0..1) of the panel width, for
// event markers along the playback timeline.
public sealed class FractionPanel : Panel
{
    public static readonly DependencyProperty FractionProperty = DependencyProperty.RegisterAttached(
        "Fraction",
        typeof(double),
        typeof(FractionPanel),
        new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsParentArrange));

    public static double GetFraction(DependencyObject element) => (double)element.GetValue(FractionProperty);

    public static void SetFraction(DependencyObject element, double value) => element.SetValue(FractionProperty, value);

    protected override Size MeasureOverride(Size availableSize)
    {
        var height = 0d;
        foreach (UIElement child in InternalChildren)
        {
            child.Measure(new Size(double.PositiveInfinity, availableSize.Height));
            height = Math.Max(height, child.DesiredSize.Height);
        }
        return new Size(double.IsInfinity(availableSize.Width) ? 0 : availableSize.Width, height);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        foreach (UIElement child in InternalChildren)
        {
            var fraction = Math.Clamp(GetFraction(child), 0, 1);
            var width = child.DesiredSize.Width;
            child.Arrange(new Rect(fraction * finalSize.Width - width / 2, 0, width, child.DesiredSize.Height));
        }
        return finalSize;
    }
}
