using SpineViewerWPF.Core;

namespace SpineViewerWPF.Wpf;

// Framing math shared by the GPU preview and viewport commands, so the content
// center used for layer focus matches what the shader draws.
public static class ViewportMath
{
    // Large poses are fitted into the framebuffer around their bounds center;
    // small poses keep the skeleton origin at the layer anchor.
    public static (float CenterX, float CenterY, float FitScale) ComputeFit(
        PreviewSceneFrame scene,
        int framebufferWidth,
        int framebufferHeight)
    {
        if (!HasBounds(scene)) return (0, 0, 1);
        var shouldFit = scene.BoundsWidth > framebufferWidth / 4f || scene.BoundsHeight > framebufferHeight / 4f;
        if (!shouldFit) return (0, 0, 1);
        var fitScale = MathF.Min(1, MathF.Min(
            Math.Max(1, framebufferWidth - 32) / scene.BoundsWidth,
            Math.Max(1, framebufferHeight - 32) / scene.BoundsHeight));
        return (scene.BoundsX + scene.BoundsWidth / 2, scene.BoundsY + scene.BoundsHeight / 2, fitScale);
    }

    // TASK-065: the layer's content center in device-independent pixels relative
    // to the viewport center (x right, y down), excluding viewport pan. Mirrors
    // the vertex shader: fit, layer scale and flips, rotation, viewport zoom,
    // then layer translation.
    public static bool TryGetLayerContentCenter(
        PreviewSceneFrame scene,
        double modelX,
        double modelY,
        double modelScale,
        double modelRotation,
        bool flipX,
        bool flipY,
        int framebufferWidth,
        int framebufferHeight,
        double viewportZoom,
        double dpiScaleX,
        double dpiScaleY,
        out double centerX,
        out double centerY)
    {
        centerX = centerY = 0;
        if (!HasBounds(scene) || framebufferWidth < 1 || framebufferHeight < 1
            || dpiScaleX <= 0 || dpiScaleY <= 0)
            return false;

        var (fitCenterX, fitCenterY, fitScale) = ComputeFit(scene, framebufferWidth, framebufferHeight);
        var x = (scene.BoundsX + scene.BoundsWidth / 2d - fitCenterX) * fitScale * modelScale * (flipX ? -1 : 1);
        var y = (scene.BoundsY + scene.BoundsHeight / 2d - fitCenterY) * fitScale * modelScale * (flipY ? -1 : 1);
        var radians = -modelRotation * Math.PI / 180;
        var rotatedX = x * Math.Cos(radians) - y * Math.Sin(radians);
        var rotatedY = x * Math.Sin(radians) + y * Math.Cos(radians);
        var physicalX = rotatedX * viewportZoom + modelX * dpiScaleX;
        var physicalY = rotatedY * viewportZoom - modelY * dpiScaleY;
        centerX = physicalX / dpiScaleX;
        centerY = -physicalY / dpiScaleY;
        return double.IsFinite(centerX) && double.IsFinite(centerY);
    }

    private static bool HasBounds(PreviewSceneFrame scene) =>
        scene.BoundsWidth > 0 && scene.BoundsHeight > 0
        && float.IsFinite(scene.BoundsX) && float.IsFinite(scene.BoundsY)
        && float.IsFinite(scene.BoundsWidth) && float.IsFinite(scene.BoundsHeight);
}
