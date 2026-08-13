using System.Numerics;
using SpineViewerWPF.Core;

namespace SpineViewerWPF.Application;

public sealed record SceneFrameLayer(RenderedFrame Frame, SceneLayerDocument Layer);

public static class SceneFrameCompositor
{
    public static RenderedFrame Compose(
        IReadOnlyList<SceneFrameLayer> layers,
        int width,
        int height)
    {
        ArgumentNullException.ThrowIfNull(layers);
        if (layers.Count > 8)
            throw new ArgumentOutOfRangeException(nameof(layers), "A scene is limited to eight layers.");
        if (width is < 1 or > 4096 || height is < 1 or > 4096)
            throw new ArgumentOutOfRangeException(nameof(width), "Composite dimensions must be between 1 and 4096.");

        var pixelCount = checked(width * height);
        var ordered = layers
            .Select((item, index) => (Item: Validate(item, width, height), Index: index))
            .Where(item => item.Item.Layer.IsVisible && item.Item.Layer.Opacity > 0)
            .OrderBy(item => item.Item.Layer.ZIndex)
            .ThenBy(item => item.Index)
            .Select(item => item.Item)
            .ToArray();
        if (ordered.Length == 0)
            return new RenderedFrame(width, height, new byte[checked(pixelCount * 4)]);

        var output = new Vector4[pixelCount];
        var centerX = width / 2d;
        var centerY = height / 2d;
        foreach (var item in ordered)
        {
            var layer = item.Layer;
            var radians = layer.ModelRotation % 360d * Math.PI / 180d;
            var cosine = Math.Cos(radians);
            var sine = Math.Sin(radians);
            var scaleX = layer.ModelScale * (layer.FlipX ? -1 : 1);
            var scaleY = layer.ModelScale * (layer.FlipY ? -1 : 1);
            // Model translations are defined as 96-DPI output pixels.
            var translationX = layer.ModelX;
            var translationY = layer.ModelY;
            var opacity = (float)layer.Opacity;

            for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var translatedX = x + 0.5f - centerX - translationX;
                var translatedY = y + 0.5f - centerY - translationY;
                var sourceX = (cosine * translatedX + sine * translatedY) / scaleX + centerX - 0.5f;
                var sourceY = (-sine * translatedX + cosine * translatedY) / scaleY + centerY - 0.5f;
                if (sourceX <= -1 || sourceX >= width || sourceY <= -1 || sourceY >= height) continue;

                var source = Sample(item.Frame, (float)sourceX, (float)sourceY) * opacity;
                if (source.W <= 0) continue;
                var index = y * width + x;
                var destination = output[index];
                var remaining = 1 - source.W;
                output[index] = new Vector4(
                    source.X + destination.X * remaining,
                    source.Y + destination.Y * remaining,
                    source.Z + destination.Z * remaining,
                    source.W + destination.W * remaining);
            }
        }

        var bgra = new byte[checked(pixelCount * 4)];
        for (var index = 0; index < output.Length; index++)
        {
            var pixel = output[index];
            var alpha = Math.Clamp(pixel.W, 0, 1);
            var offset = index * 4;
            if (alpha > 0)
            {
                bgra[offset] = ToByte(pixel.X / alpha);
                bgra[offset + 1] = ToByte(pixel.Y / alpha);
                bgra[offset + 2] = ToByte(pixel.Z / alpha);
            }
            bgra[offset + 3] = ToByte(alpha);
        }
        return new RenderedFrame(width, height, bgra);
    }

    private static SceneFrameLayer Validate(SceneFrameLayer item, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(item);
        ArgumentNullException.ThrowIfNull(item.Frame);
        ArgumentNullException.ThrowIfNull(item.Layer);
        if (item.Frame.Width != width || item.Frame.Height != height
            || item.Frame.Bgra32.Length != checked(width * height * 4))
            throw new ArgumentException("Every scene frame must match the composite dimensions.", nameof(item));
        if (!double.IsFinite(item.Layer.ModelX) || !double.IsFinite(item.Layer.ModelY)
            || !double.IsFinite(item.Layer.ModelRotation)
            || !double.IsFinite(item.Layer.ModelScale) || item.Layer.ModelScale <= 0)
            throw new ArgumentOutOfRangeException(nameof(item), "Layer transforms must be finite and scale must be positive.");
        if (!double.IsFinite(item.Layer.Opacity) || item.Layer.Opacity is < 0 or > 1)
            throw new ArgumentOutOfRangeException(nameof(item), "Layer opacity must be between 0 and 1.");
        return item;
    }

    private static Vector4 Sample(RenderedFrame frame, float x, float y)
    {
        var left = (int)MathF.Floor(x);
        var top = (int)MathF.Floor(y);
        var horizontal = x - left;
        var vertical = y - top;
        return Pixel(frame, left, top) * ((1 - horizontal) * (1 - vertical))
            + Pixel(frame, left + 1, top) * (horizontal * (1 - vertical))
            + Pixel(frame, left, top + 1) * ((1 - horizontal) * vertical)
            + Pixel(frame, left + 1, top + 1) * (horizontal * vertical);
    }

    private static Vector4 Pixel(RenderedFrame frame, int x, int y)
    {
        if ((uint)x >= (uint)frame.Width || (uint)y >= (uint)frame.Height) return Vector4.Zero;
        var offset = (y * frame.Width + x) * 4;
        var alpha = frame.Bgra32[offset + 3] / 255f;
        return new Vector4(
            frame.Bgra32[offset] / 255f * alpha,
            frame.Bgra32[offset + 1] / 255f * alpha,
            frame.Bgra32[offset + 2] / 255f * alpha,
            alpha);
    }

    private static byte ToByte(float value) =>
        (byte)Math.Clamp(MathF.Round(Math.Clamp(value, 0, 1) * 255), 0, 255);
}
