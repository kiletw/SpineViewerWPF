using SpineViewerWPF.Core;

namespace SpineViewerWPF.Application;

// TASK-071: the one scene camera shared by the GPU and CPU preview, Screenshot,
// and fixed-size export. Scene space has its origin at the scene center, x right
// and y down, in output pixels at zoom 1 (device-independent pixels in the WPF
// preview). A layer puts its skeleton origin at (ModelX, ModelY), scales by
// ModelScale, flips, then rotates clockwise by ModelRotation degrees; skeleton y
// points up. Nothing is re-fitted per frame.
public sealed record SceneLayerPlacement(RenderCamera Camera, SceneLayerDocument Layer, int FrameWidth, int FrameHeight);

public static class SceneCamera
{
    // Frames a layer so that, once SceneFrameCompositor (or an equivalent view
    // transform) applies the returned layer's flips and rotation around the frame
    // center, scene point (viewCenterX, viewCenterY) lands on the canvas center at
    // pixelsPerUnit output pixels per scene unit. The returned layer has no
    // translation or scale; both are folded into the camera. Rotations that are
    // not a multiple of 90 degrees get a square frame covering the canvas diagonal.
    public static SceneLayerPlacement Place(
        SceneLayerDocument layer,
        double viewCenterX,
        double viewCenterY,
        double pixelsPerUnit,
        int canvasWidth,
        int canvasHeight)
    {
        ArgumentNullException.ThrowIfNull(layer);
        if (!double.IsFinite(pixelsPerUnit) || pixelsPerUnit <= 0)
            throw new ArgumentOutOfRangeException(nameof(pixelsPerUnit), "Pixels per unit must be positive.");
        if (!double.IsFinite(viewCenterX) || !double.IsFinite(viewCenterY))
            throw new ArgumentOutOfRangeException(nameof(viewCenterX), "The view center must be finite.");
        if (canvasWidth is < 1 or > 4096 || canvasHeight is < 1 or > 4096)
            throw new ArgumentOutOfRangeException(nameof(canvasWidth), "Canvas dimensions must be between 1 and 4096.");
        if (!double.IsFinite(layer.ModelScale) || layer.ModelScale <= 0)
            throw new ArgumentOutOfRangeException(nameof(layer), "Layer scale must be positive.");

        var (cosine, sine) = Rotation(layer.ModelRotation);
        var deltaX = viewCenterX - layer.ModelX;
        var deltaY = viewCenterY - layer.ModelY;
        // Undo the clockwise rotation, then the flips and scale (flips are their own inverse).
        var unrotatedX = cosine * deltaX + sine * deltaY;
        var unrotatedY = -sine * deltaX + cosine * deltaY;
        var localX = unrotatedX * (layer.FlipX ? -1 : 1) / layer.ModelScale;
        var localY = unrotatedY * (layer.FlipY ? -1 : 1) / layer.ModelScale;
        var camera = new RenderCamera((float)localX, (float)-localY, (float)(layer.ModelScale * pixelsPerUnit));

        var quarterTurns = layer.ModelRotation / 90d;
        var axisAligned = Math.Abs(quarterTurns - Math.Round(quarterTurns)) < 1e-9;
        var (frameWidth, frameHeight) = !axisAligned
            ? Square((int)Math.Ceiling(Math.Sqrt((double)canvasWidth * canvasWidth + (double)canvasHeight * canvasHeight)))
            : ((long)Math.Round(quarterTurns) % 2 == 0 ? (canvasWidth, canvasHeight) : (canvasHeight, canvasWidth));
        return new SceneLayerPlacement(camera, layer with { ModelX = 0, ModelY = 0, ModelScale = 1 }, frameWidth, frameHeight);

        static (int, int) Square(int side) => (Math.Min(4096, side), Math.Min(4096, side));
    }

    // Scene position of a skeleton-space point (y up) of the layer.
    public static (double X, double Y) ToScene(SceneLayerDocument layer, double skeletonX, double skeletonY)
    {
        ArgumentNullException.ThrowIfNull(layer);
        return ToScene(layer.ModelX, layer.ModelY, layer.ModelScale, layer.ModelRotation, layer.FlipX, layer.FlipY, skeletonX, skeletonY);
    }

    public static (double X, double Y) ToScene(
        double modelX,
        double modelY,
        double modelScale,
        double modelRotation,
        bool flipX,
        bool flipY,
        double skeletonX,
        double skeletonY)
    {
        var (cosine, sine) = Rotation(modelRotation);
        var x = skeletonX * modelScale * (flipX ? -1 : 1);
        var y = -skeletonY * modelScale * (flipY ? -1 : 1);
        return (modelX + cosine * x - sine * y, modelY + sine * x + cosine * y);
    }

    // Scene-space bounding box of a skeleton-space box (y up) after the layer
    // transform; empty boxes return null.
    public static (double MinX, double MinY, double MaxX, double MaxY)? BoundsToScene(
        SceneLayerDocument layer,
        double x,
        double y,
        double width,
        double height)
    {
        if (!(width > 0) || !(height > 0) || !double.IsFinite(x) || !double.IsFinite(y)
            || !double.IsFinite(width) || !double.IsFinite(height))
            return null;
        var corners = new[]
        {
            ToScene(layer, x, y), ToScene(layer, x + width, y),
            ToScene(layer, x, y + height), ToScene(layer, x + width, y + height)
        };
        return (corners.Min(item => item.X), corners.Min(item => item.Y), corners.Max(item => item.X), corners.Max(item => item.Y));
    }

    private static (double Cosine, double Sine) Rotation(double degrees)
    {
        var radians = degrees % 360d * Math.PI / 180d;
        return (Math.Cos(radians), Math.Sin(radians));
    }
}
