namespace SpineViewerWPF.Core;

public sealed record Diagnostic(string Severity, string Code, string Message, string? Path = null);

public sealed record AnimationDescriptor(string Name, float DurationSeconds);

public sealed record SlotDescriptor(
    string Name,
    string? SetupAttachment,
    IReadOnlyList<string> Attachments);

public sealed record AssetDescriptor(string SkeletonPath, string AtlasPath, IReadOnlyList<string> Textures);

public sealed record RuntimeDescriptor(
    string? DetectedExportVersion,
    string SelectedLine,
    string RuntimeVersion,
    bool Overridden);

public sealed record InspectResult(
    bool Success,
    AssetDescriptor Asset,
    RuntimeDescriptor Runtime,
    IReadOnlyList<AnimationDescriptor> Animations,
    IReadOnlyList<string> Skins,
    IReadOnlyList<Diagnostic> Diagnostics,
    IReadOnlyList<SlotDescriptor>? Slots = null);

// Render and scene-layer Animation values use an empty string for setup pose.
// Named animations must exist; null is reserved for the Shell/project selection.
public sealed record RenderRequest(
    string SkeletonPath,
    string AtlasPath,
    string Animation,
    float TimeSeconds,
    int Width,
    int Height,
    string OutputPath,
    bool Overwrite,
    bool Pma,
    IReadOnlyList<string> Skins,
    float TrackAlpha = 1,
    IReadOnlyList<SlotDisplayDocument>? Slots = null);

// Fixed framing for one rendered frame: the skeleton-space point drawn at the
// frame center and the output pixels per skeleton unit. Null keeps the renderer's
// automatic bounds fit.
public sealed record RenderCamera(float CenterX, float CenterY, float Scale);

public sealed record FrameRenderRequest(
    string Animation,
    float TimeSeconds,
    int Width,
    int Height,
    bool Pma,
    IReadOnlyList<string> Skins,
    float TrackAlpha = 1,
    bool LinearFiltering = true,
    IReadOnlyList<SlotDisplayDocument>? Slots = null,
    RenderCamera? Camera = null);

public sealed record RenderedFrame(int Width, int Height, byte[] Bgra32);

public enum PreviewBlendMode
{
    Normal,
    Additive,
    Multiply,
    Screen
}

public readonly record struct PreviewVertex(float X, float Y, float U, float V);

public sealed record PreviewTexture(string Key, int Width, int Height, byte[] Rgba32);

public sealed record PreviewDrawCommand(
    PreviewTexture Texture,
    PreviewVertex[] Vertices,
    int[] Indices,
    float Red,
    float Green,
    float Blue,
    float Alpha,
    PreviewBlendMode BlendMode,
    bool Pma,
    string SlotName = "");

public sealed record PreviewSceneFrame(
    float BoundsX,
    float BoundsY,
    float BoundsWidth,
    float BoundsHeight,
    IReadOnlyList<PreviewDrawCommand> DrawCommands);

public sealed record PreviewSceneRequest(
    string Animation,
    float TimeSeconds,
    bool Pma,
    IReadOnlyList<string> Skins,
    float TrackAlpha = 1,
    IReadOnlyList<SlotDisplayDocument>? Slots = null);

public sealed record SceneLayerRenderRequest(
    string SkeletonPath,
    string? AtlasPath,
    string? RuntimeOverride,
    string Animation,
    float TimeSeconds,
    string OutputPath,
    bool Overwrite,
    bool Pma,
    IReadOnlyList<string> Skins,
    float TrackAlpha = 1,
    IReadOnlyList<SlotDisplayDocument>? Slots = null);

public sealed record SceneLayerOpenResult(
    InspectResult Inspection,
    string Animation,
    string SelectedSkin,
    string PreviewPath);

public sealed record SlotDisplayDocument(
    string Name,
    bool IsVisible,
    double Opacity,
    string? AttachmentName = null);

public sealed record SceneLayerDocument(
    string SkeletonPath,
    string? AtlasPath,
    string? RuntimeOverride,
    string Animation,
    string SelectedSkin,
    double ModelX,
    double ModelY,
    double ModelScale,
    double ModelRotation,
    bool FlipX,
    bool FlipY,
    bool IsVisible,
    double Opacity,
    int ZIndex,
    double? TrackAlpha = null,
    bool? Pma = null,
    IReadOnlyList<SlotDisplayDocument>? Slots = null);

public sealed record AnimationExportRequest(
    string SkeletonPath,
    string? AtlasPath,
    string? RuntimeOverride,
    string Animation,
    float DurationSeconds,
    float FramesPerSecond,
    int Width,
    int Height,
    string OutputDirectory,
    string FilePrefix,
    bool Overwrite,
    bool Pma,
    IReadOnlyList<string> Skins,
    float TrackAlpha = 1,
    IReadOnlyList<SlotDisplayDocument>? Slots = null,
    IReadOnlyList<SceneLayerDocument>? SceneLayers = null,
    ExportFraming? Framing = null);

// Auto-fit export framing: the output size follows the union of the visible
// content bounds across every exported frame, rendered at Scale output pixels per
// skeleton unit, plus Margin transparent pixels on each side.
public sealed record ExportFraming(float Scale = 1, int Margin = 0);

public sealed record AnimationExportResult(
    IReadOnlyList<string> OutputPaths,
    int FrameCount,
    float DurationSeconds,
    int Width = 0,
    int Height = 0);

public readonly record struct AnimationExportProgress(int CompletedFrames, int TotalFrames);

public sealed record ViewerProjectDocument(
    int SchemaVersion,
    string SkeletonPath,
    string? AtlasPath,
    string? SelectedAnimation,
    string SelectedSkin,
    double ModelX,
    double ModelY,
    double ModelScale,
    double ModelRotation,
    bool FlipX,
    bool FlipY,
    bool Loop,
    double PlaybackSpeed,
    double TrackAlpha,
    string BackgroundMode,
    IReadOnlyList<SceneLayerDocument>? SceneLayers = null);
