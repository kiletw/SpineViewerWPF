namespace SpineViewerWPF.Core;

public sealed record Diagnostic(string Severity, string Code, string Message, string? Path = null);

public sealed record AnimationDescriptor(string Name, float DurationSeconds);

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
    IReadOnlyList<Diagnostic> Diagnostics);

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
    IReadOnlyList<string> Skins);

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
    IReadOnlyList<string> Skins);

public sealed record AnimationExportResult(
    IReadOnlyList<string> OutputPaths,
    int FrameCount,
    float DurationSeconds);

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
    string BackgroundMode);
