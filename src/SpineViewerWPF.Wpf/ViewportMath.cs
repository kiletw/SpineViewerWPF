using SpineViewerWPF.Application;
using SpineViewerWPF.Core;

namespace SpineViewerWPF.Wpf;

// Viewport math shared by the GPU preview and viewport commands. TASK-071: the
// preview draws scene space (SceneCamera) at ViewportZoom DIPs per unit with no
// per-frame fit, so the content center used for layer focus matches the shader.
public static class ViewportMath
{
    // TASK-065: the layer's content center in device-independent pixels relative
    // to the viewport center (x right, y down), excluding viewport pan.
    public static bool TryGetLayerContentCenter(
        PreviewSceneFrame scene,
        double modelX,
        double modelY,
        double modelScale,
        double modelRotation,
        bool flipX,
        bool flipY,
        double viewportZoom,
        out double centerX,
        out double centerY)
    {
        centerX = centerY = 0;
        if (!HasBounds(scene) || !double.IsFinite(viewportZoom) || viewportZoom <= 0) return false;
        var (x, y) = SceneCamera.ToScene(
            modelX, modelY, modelScale, modelRotation, flipX, flipY,
            scene.BoundsX + scene.BoundsWidth / 2d,
            scene.BoundsY + scene.BoundsHeight / 2d);
        centerX = x * viewportZoom;
        centerY = y * viewportZoom;
        return double.IsFinite(centerX) && double.IsFinite(centerY);
    }

    public static bool HasBounds(PreviewSceneFrame scene) =>
        scene.BoundsWidth > 0 && scene.BoundsHeight > 0
        && float.IsFinite(scene.BoundsX) && float.IsFinite(scene.BoundsY)
        && float.IsFinite(scene.BoundsWidth) && float.IsFinite(scene.BoundsHeight);
}
