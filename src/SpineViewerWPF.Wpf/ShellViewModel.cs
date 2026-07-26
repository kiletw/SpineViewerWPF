using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Threading;
using SpineViewerWPF.Application;
using SpineViewerWPF.Core;

namespace SpineViewerWPF.Wpf;

public enum WorkspaceState
{
    Empty,
    Loading,
    Ready,
    ReadyWithWarnings,
    Unsupported,
    Failed,
    RendererUnavailable,
    Exporting
}

public sealed class ShellViewModel : INotifyPropertyChanged, IDisposable
{
    private static readonly string[] DefaultAnimations = ["idle", "walk", "attack", "victory"];
    private static readonly string[] DefaultSkins = ["default", "armor", "shadow"];
    private static readonly IReadOnlyDictionary<WorkspaceState, StateDefinition> Definitions =
        new Dictionary<WorkspaceState, StateDefinition>
        {
            [WorkspaceState.Empty] = new("No asset open", "Drop a skeleton here or choose Open asset.", false, false, false),
            [WorkspaceState.Loading] = new("Opening hero.json", "Resolving atlas, textures, and Runtime 4.1…", false, false, false),
            [WorkspaceState.Ready] = new("Ready", "hero.json · idle", true, true, true),
            [WorkspaceState.ReadyWithWarnings] = new("Ready with warnings", "Preview is available; one texture uses a fallback filter.", true, true, true),
            [WorkspaceState.Unsupported] = new("Unsupported export", "This prototype supports Runtime 4.1. Re-export or choose a supported asset.", true, false, false),
            [WorkspaceState.Failed] = new("Could not open asset", "The atlas references a missing texture: hero_2.png.", true, false, false),
            [WorkspaceState.RendererUnavailable] = new("Renderer unavailable", "Metadata loaded, but preview rendering is not available.", true, false, false),
            [WorkspaceState.Exporting] = new("Exporting frame", "Writing hero-idle.png without blocking the current preview.", true, true, false)
        };

    private readonly AssetService? assetService;
    private readonly ViewerProjectStore projectStore;
    private readonly Func<string?> chooseAssetPath;
    private readonly Func<string, string?> chooseProjectPath;
    private readonly Func<string?> chooseScreenshotPath;
    private readonly Func<bool> confirmDiscardChanges;
    private readonly Func<string> createPreviewPath;
    private readonly DispatcherTimer playbackTimer;
    private readonly CancellationTokenSource playbackCancellation = new();
    // ponytail: in-memory history is enough for one fake document; cap or persist it with multi-document editing.
    private readonly Stack<EditorSnapshot> undo = [];
    private readonly Stack<EditorSnapshot> redo = [];
    private WorkspaceState state;
    private EditorSnapshot savedSnapshot;
    private string[] animations = DefaultAnimations;
    private string[] skinNames = DefaultSkins;
    private Dictionary<string, double> animationDurations = new(StringComparer.Ordinal);
    private string skeletonPath = "hero.json";
    private string atlasPath = "hero.atlas";
    private string runtimeLine = "4.1";
    private IReadOnlyList<Diagnostic> diagnostics = [];
    private string? stateTitleOverride;
    private string? stateDetailOverride;
    private bool isRailExpanded;
    private bool isInspectorVisible;
    private bool isDiagnosticsVisible;
    private bool isPlaying;
    private DateTime lastPlaybackTick;
    private int previewRenderInProgress;
    private bool disposed;
    private string animationFilter = "";
    private string? selectedAnimation = DefaultAnimations[0];
    private string selectedSkin = DefaultSkins[0];
    private double position;
    private double modelX;
    private double modelY;
    private double modelScale = 1;
    private double modelRotation;
    private double viewportZoom = 1;
    private double viewportPanX;
    private double viewportPanY;
    private bool flipX;
    private bool flipY;
    private bool loop = true;
    private double playbackSpeed = 1;
    private double trackAlpha = 1;
    private string backgroundMode = "Checkerboard";
    private string? projectPath;
    private string? previewImagePath;
    private bool isPrototypePreview = true;
    private string lastAction = "Prototype ready";

    public ShellViewModel(
        WorkspaceState initialState,
        bool expandedWorkspace,
        ViewerProjectStore? projectStore = null,
        Func<string, string?>? chooseProjectPath = null,
        AssetService? assetService = null,
        Func<string?>? chooseAssetPath = null,
        Func<bool>? confirmDiscardChanges = null,
        Func<string>? createPreviewPath = null,
        Func<string?>? chooseScreenshotPath = null)
    {
        state = initialState;
        isRailExpanded = expandedWorkspace;
        isInspectorVisible = expandedWorkspace;
        isPlaying = initialState is WorkspaceState.Ready or WorkspaceState.ReadyWithWarnings;
        this.projectStore = projectStore ?? new ViewerProjectStore();
        this.chooseProjectPath = chooseProjectPath ?? (_ => null);
        this.chooseScreenshotPath = chooseScreenshotPath ?? (() => null);
        this.assetService = assetService;
        this.chooseAssetPath = chooseAssetPath ?? (() => null);
        this.confirmDiscardChanges = confirmDiscardChanges ?? (() => false);
        this.createPreviewPath = createPreviewPath ?? (() =>
            Path.Combine(Path.GetTempPath(), "SpineViewerWPF", $"{Guid.NewGuid():N}.png"));
        playbackTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) };
        playbackTimer.Tick += AdvancePlayback;
        savedSnapshot = Capture();

        OpenAssetCommand = new RelayCommand(async () => await OpenAssetAsync(), () => !IsLoading);
        ReloadCommand = new RelayCommand(async () =>
        {
            if (assetService is null)
                LastAction = "Fake asset reloaded";
            else
                await OpenAssetAsync(skeletonPath);
        }, () => HasAsset && !IsLoading);
        ExportCommand = new RelayCommand(() => State = WorkspaceState.Exporting, () => CanPlay);
        ScreenshotCommand = new RelayCommand(CaptureScreenshot, () => HasRenderedPreview && CanPlay);
        TogglePlayCommand = new RelayCommand(() => IsPlaying = !IsPlaying, () => CanPlay);
        StopCommand = new RelayCommand(() =>
        {
            IsPlaying = false;
            Position = 0;
        }, () => CanPlay);
        FitCommand = new RelayCommand(FitViewport, () => HasPreview);
        DiagnosticsCommand = new RelayCommand(ToggleDiagnostics);
        ToggleRailCommand = new RelayCommand(() => IsRailExpanded = !IsRailExpanded);
        ToggleInspectorCommand = new RelayCommand(() => IsInspectorVisible = !IsInspectorVisible);
        SaveCommand = new RelayCommand(() => TrySave(), () => HasAsset && (IsDirty || ProjectPath is null));
        SaveAsCommand = new RelayCommand(() => TrySaveAs(), () => HasAsset);
        UndoCommand = new RelayCommand(Undo, () => undo.Count > 0);
        RedoCommand = new RelayCommand(Redo, () => redo.Count > 0);
        CycleStateCommand = new RelayCommand(() =>
            State = (WorkspaceState)(((int)State + 1) % Enum.GetValues<WorkspaceState>().Length));
        UpdatePlaybackTimer();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<string> FilteredAnimations =>
        animations.Where(x => x.Contains(AnimationFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
    public IReadOnlyList<string> Skins => skinNames;
    public IReadOnlyList<string> BackgroundModes { get; } = ["Checkerboard", "Dark", "Light"];

    public ICommand OpenAssetCommand { get; }
    public ICommand ReloadCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ScreenshotCommand { get; }
    public ICommand TogglePlayCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand FitCommand { get; }
    public ICommand DiagnosticsCommand { get; }
    public ICommand ToggleRailCommand { get; }
    public ICommand ToggleInspectorCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SaveAsCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }
    public ICommand CycleStateCommand { get; }

    public WorkspaceState State
    {
        get => state;
        set
        {
            if (state == value) return;
            state = value;
            stateTitleOverride = null;
            stateDetailOverride = null;
            isPlaying = value is WorkspaceState.Ready or WorkspaceState.ReadyWithWarnings;
            isDiagnosticsVisible = false;
            lastPlaybackTick = DateTime.UtcNow;
            position = 0;
            Changed(string.Empty);
            UpdatePlaybackTimer();
            RefreshCommands();
        }
    }

    public bool IsRailExpanded
    {
        get => isRailExpanded;
        set
        {
            if (isRailExpanded == value) return;
            isRailExpanded = value;
            Changed();
        }
    }

    public bool IsInspectorVisible
    {
        get => isInspectorVisible;
        set
        {
            if (isInspectorVisible == value) return;
            isInspectorVisible = value;
            Changed();
        }
    }

    public bool IsPlaying
    {
        get => isPlaying;
        set
        {
            if (isPlaying == value) return;
            isPlaying = value;
            lastPlaybackTick = DateTime.UtcNow;
            UpdatePlaybackTimer();
            Changed();
            Changed(nameof(PlaybackLabel));
        }
    }

    public string AnimationFilter
    {
        get => animationFilter;
        set
        {
            if (animationFilter == value) return;
            animationFilter = value;
            Changed();
            Changed(nameof(FilteredAnimations));
        }
    }

    public string? SelectedAnimation
    {
        get => selectedAnimation;
        set
        {
            if (selectedAnimation == value) return;
            Edit(ref selectedAnimation, value, nameof(SelectedAnimation), nameof(Duration), nameof(PlaybackTimeLabel), nameof(StateDetail));
            Position = 0;
            QueuePreviewRender();
        }
    }

    public string SelectedSkin
    {
        get => selectedSkin;
        set
        {
            var next = value ?? DefaultSkins[0];
            if (selectedSkin == next) return;
            Edit(ref selectedSkin, next, nameof(SelectedSkin));
            QueuePreviewRender();
        }
    }

    public double Position
    {
        get => position;
        set
        {
            if (Math.Abs(position - value) < 0.001) return;
            position = Duration > 0 ? Math.Clamp(value, 0, Duration) : Math.Max(0, value);
            Changed();
            Changed(nameof(PlaybackTimeLabel));
            QueuePreviewRender();
        }
    }

    public double ModelX
    {
        get => modelX;
        set => Edit(ref modelX, value, nameof(ModelX), nameof(PreviewX));
    }

    public double ModelY
    {
        get => modelY;
        set => Edit(ref modelY, value, nameof(ModelY), nameof(PreviewY));
    }

    public double ModelScale
    {
        get => modelScale;
        set => Edit(ref modelScale, value, nameof(ModelScale), nameof(PreviewScaleX), nameof(PreviewScaleY));
    }

    public double ModelRotation
    {
        get => modelRotation;
        set => Edit(ref modelRotation, value, nameof(ModelRotation));
    }

    public bool FlipX
    {
        get => flipX;
        set => Edit(ref flipX, value, nameof(FlipX), nameof(PreviewScaleX));
    }

    public bool FlipY
    {
        get => flipY;
        set => Edit(ref flipY, value, nameof(FlipY), nameof(PreviewScaleY));
    }

    public bool Loop
    {
        get => loop;
        set => Edit(ref loop, value, nameof(Loop));
    }

    public double PlaybackSpeed
    {
        get => playbackSpeed;
        set => Edit(ref playbackSpeed, value, nameof(PlaybackSpeed));
    }

    public double TrackAlpha
    {
        get => trackAlpha;
        set => Edit(ref trackAlpha, value, nameof(TrackAlpha));
    }

    public string BackgroundMode
    {
        get => backgroundMode;
        set => Edit(ref backgroundMode, value ?? "Checkerboard", nameof(BackgroundMode));
    }

    public string LastAction
    {
        get => lastAction;
        set
        {
            if (lastAction == value) return;
            lastAction = value;
            Changed();
        }
    }

    public bool IsEmpty => State == WorkspaceState.Empty;
    public bool IsLoading => State == WorkspaceState.Loading;
    public bool IsWarning => State == WorkspaceState.ReadyWithWarnings;
    public bool IsExporting => State == WorkspaceState.Exporting;
    public bool HasAsset => Definition.HasAsset;
    public bool HasPreview => Definition.HasPreview;
    public bool HasRenderedPreview => PreviewImagePath is not null;
    public bool HasPrototypePreview => HasPreview && isPrototypePreview;
    public bool CanPlay => Definition.CanPlay && SelectedAnimation is not null;
    public bool IsDirty => Capture() != savedSnapshot;
    public bool HasBlockingOverlay => State is WorkspaceState.Unsupported or WorkspaceState.Failed or WorkspaceState.RendererUnavailable;
    public string StateTitle => stateTitleOverride ?? Definition.Title;
    public string StateDetail => stateDetailOverride
        ?? (State is WorkspaceState.Ready or WorkspaceState.ReadyWithWarnings
            ? $"{SkeletonFileName} · {SelectedAnimation ?? "setup pose"}"
            : Definition.Detail);
    public string RuntimeLabel => HasAsset ? $"Runtime {runtimeLine}" : "Runtime —";
    public string DiagnosticLabel => IsWarning
        ? $"{diagnostics.Count} warning{(diagnostics.Count == 1 ? "" : "s")}"
        : State is WorkspaceState.Unsupported or WorkspaceState.Failed or WorkspaceState.RendererUnavailable
            ? $"{Math.Max(1, diagnostics.Count)} error{(diagnostics.Count == 1 ? "" : "s")}"
            : diagnostics.Count == 0 ? "No issues" : $"{diagnostics.Count} issue{(diagnostics.Count == 1 ? "" : "s")}";
    public IReadOnlyList<Diagnostic> Diagnostics => diagnostics;
    public bool IsDiagnosticsVisible => isDiagnosticsVisible;
    public string DiagnosticsSummary => diagnostics.Count == 0
        ? "No diagnostics."
        : string.Join(Environment.NewLine, diagnostics.Select(d =>
            $"{d.Severity.ToUpperInvariant()} {d.Code}: {d.Message}{(d.Path is null ? "" : $" ({d.Path})")}"));
    public string PlaybackLabel => IsPlaying ? "Pause" : "Play";
    public string SkeletonFileName => Path.GetFileName(skeletonPath);
    public string AssetSummary => $"{Path.GetFileName(atlasPath)} · Runtime {runtimeLine}";
    public string DocumentTitle => HasAsset ? $"{SkeletonFileName}{(IsDirty ? " *" : "")}" : "No document";
    public string WindowTitle => $"Spine Viewer · {DocumentTitle}";
    public string ProjectPathLabel => ProjectPath ?? "Not saved";
    public string? ProjectPath => projectPath;
    public string? PreviewImagePath => previewImagePath;
    public double PreviewX => ModelX * ViewportZoom + ViewportPanX;
    public double PreviewY => ModelY * ViewportZoom + ViewportPanY;
    public double ViewportZoom => viewportZoom;
    public double ViewportPanX => viewportPanX;
    public double ViewportPanY => viewportPanY;
    public double PreviewScaleX => ModelScale * ViewportZoom * (FlipX ? -1 : 1);
    public double PreviewScaleY => ModelScale * ViewportZoom * (FlipY ? -1 : 1);
    public double Duration => SelectedAnimation is not null && animationDurations.TryGetValue(SelectedAnimation, out var duration)
        ? duration
        : 0;
    public string PlaybackTimeLabel => $"{FormatTime(Position)} / {FormatTime(Duration)}";

    public void ZoomViewport(double factor)
    {
        if (!double.IsFinite(factor) || factor <= 0) return;
        var next = Math.Clamp(viewportZoom * factor, 0.25, 4);
        if (Math.Abs(viewportZoom - next) < 0.001) return;
        viewportZoom = next;
        Changed(string.Empty);
    }

    public void PanViewport(double deltaX, double deltaY)
    {
        if (!double.IsFinite(deltaX) || !double.IsFinite(deltaY)) return;
        var nextX = Math.Clamp(viewportPanX + deltaX, -5000, 5000);
        var nextY = Math.Clamp(viewportPanY + deltaY, -5000, 5000);
        var changed = Math.Abs(viewportPanX - nextX) >= 0.001 || Math.Abs(viewportPanY - nextY) >= 0.001;
        viewportPanX = nextX;
        viewportPanY = nextY;
        if (changed) Changed(string.Empty);
    }

    private void FitViewport()
    {
        var changed = Math.Abs(viewportZoom - 1) >= 0.001 || Math.Abs(viewportPanX) >= 0.001 || Math.Abs(viewportPanY) >= 0.001;
        viewportZoom = 1;
        viewportPanX = viewportPanY = 0;
        if (changed) Changed(string.Empty);
        LastAction = "Viewport fitted";
    }

    private void ToggleDiagnostics()
    {
        isDiagnosticsVisible = !isDiagnosticsVisible;
        Changed(nameof(IsDiagnosticsVisible));
    }

    private void SetDiagnostics(IReadOnlyList<Diagnostic> next)
    {
        diagnostics = next.ToArray();
        Changed(nameof(Diagnostics));
        Changed(nameof(DiagnosticsSummary));
        Changed(nameof(DiagnosticLabel));
    }

    public bool TrySave()
    {
        var path = ProjectPath ?? chooseProjectPath(SuggestedProjectPath());
        return path is not null && SaveTo(path);
    }

    public bool TrySaveAs()
    {
        var path = chooseProjectPath(SuggestedProjectPath());
        return path is not null && SaveTo(path);
    }

    private void CaptureScreenshot()
    {
        var source = PreviewImagePath;
        var target = chooseScreenshotPath();
        if (source is null || target is null) return;

        try
        {
            var output = Path.GetFullPath(target);
            if (string.Equals(Path.GetFullPath(source), output, StringComparison.OrdinalIgnoreCase))
                throw new IOException("Screenshot output must differ from the active preview.");
            Directory.CreateDirectory(Path.GetDirectoryName(output) ?? ".");
            File.Copy(source, output, true);
            LastAction = $"Captured {Path.GetFileName(output)}";
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            stateDetailOverride = exception.Message;
            Changed(nameof(StateDetail));
            LastAction = "Screenshot failed";
        }
    }

    private StateDefinition Definition => Definitions[State];

    private string SuggestedProjectPath() => Path.Combine(
        Path.GetDirectoryName(skeletonPath) ?? "",
        $"{Path.GetFileNameWithoutExtension(SkeletonFileName)}{ViewerProjectStore.Extension}");

    public async Task OpenAssetAsync(string? path = null)
    {
        if (assetService is null)
        {
            ApplyFakeAsset();
            return;
        }

        path ??= chooseAssetPath();
        if (path is null || IsDirty && !confirmDiscardChanges()) return;

        State = WorkspaceState.Loading;
        stateTitleOverride = $"Opening {Path.GetFileName(path)}";
        stateDetailOverride = "Resolving atlas, textures, and Runtime 4.1…";
        Changed(nameof(StateTitle));
        Changed(nameof(StateDetail));

        try
        {
            var opened = await Task.Run(() => InspectAndRender(path));
            ApplyAsset(opened.Result, opened.PreviewPath, opened.RenderError);
        }
        catch (NotSupportedException exception)
        {
            SetDiagnostics([new Diagnostic("error", "UNSUPPORTED_ASSET", exception.Message, path)]);
            State = WorkspaceState.Unsupported;
            stateDetailOverride = exception.Message;
            LastAction = "Asset is not supported";
            Changed(nameof(StateDetail));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            SetDiagnostics([new Diagnostic("error", "OPEN_FAILED", exception.Message, path)]);
            State = WorkspaceState.Failed;
            stateDetailOverride = exception.Message;
            LastAction = "Asset open failed";
            Changed(nameof(StateDetail));
        }
    }

    private void ApplyFakeAsset()
    {
        ReplacePreview(null);
        isPrototypePreview = true;
        animations = DefaultAnimations;
        skinNames = DefaultSkins;
        animationDurations = animations.ToDictionary(x => x, _ => 1.8, StringComparer.Ordinal);
        skeletonPath = "hero.json";
        atlasPath = "hero.atlas";
        runtimeLine = "4.1";
        SetDiagnostics([]);
        selectedAnimation = animations[0];
        selectedSkin = skinNames[0];
        ResetEditor();
        State = WorkspaceState.Ready;
        LastAction = "Preview opened in 1 interaction";
        Changed(string.Empty);
        RefreshCommands();
    }

    private (InspectResult Result, string? PreviewPath, string? RenderError) InspectAndRender(string path)
    {
        var result = assetService!.Inspect(path, null, null);
        var animation = result.Animations.FirstOrDefault();
        if (animation is null)
            return (result, null, "The current renderer requires an animation.");

        var previewPath = createPreviewPath();
        try
        {
            assetService.Render(
                result.Asset.SkeletonPath,
                result.Asset.AtlasPath,
                result.Runtime.SelectedLine,
                animation.Name,
                animation.DurationSeconds / 2,
                64,
                64,
                previewPath,
                true,
                false,
                result.Skins.Take(1).ToArray());
            return (result, previewPath, null);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            DeletePreview(previewPath);
            return (result, null, exception.Message);
        }
    }

    private void ApplyAsset(InspectResult result, string? previewPath, string? renderError)
    {
        ReplacePreview(previewPath);
        isPrototypePreview = false;
        animations = result.Animations.Select(x => x.Name).ToArray();
        skinNames = result.Skins.Count == 0 ? [DefaultSkins[0]] : result.Skins.ToArray();
        animationDurations = result.Animations.ToDictionary(x => x.Name, x => (double)x.DurationSeconds, StringComparer.Ordinal);
        skeletonPath = result.Asset.SkeletonPath;
        atlasPath = result.Asset.AtlasPath;
        runtimeLine = result.Runtime.SelectedLine;
        SetDiagnostics(renderError is null
            ? result.Diagnostics
            : [.. result.Diagnostics, new Diagnostic("error", "RENDER_FAILED", renderError, result.Asset.SkeletonPath)]);
        selectedAnimation = animations.FirstOrDefault();
        selectedSkin = skinNames[0];
        ResetEditor();
        State = renderError is not null
            ? WorkspaceState.RendererUnavailable
            : diagnostics.Count == 0 ? WorkspaceState.Ready : WorkspaceState.ReadyWithWarnings;
        if (renderError is not null)
        {
            stateDetailOverride = renderError;
            Changed(nameof(StateDetail));
        }
        LastAction = renderError is null ? $"Rendered {SkeletonFileName}" : $"Opened metadata for {SkeletonFileName}";
        Changed(string.Empty);
        RefreshCommands();
    }

    private void ReplacePreview(string? path)
    {
        if (previewImagePath == path) return;
        DeletePreview(previewImagePath);
        previewImagePath = path;
        Changed(nameof(PreviewImagePath));
        Changed(nameof(HasRenderedPreview));
        Changed(nameof(HasPrototypePreview));
    }

    private void AdvancePlayback(object? sender, EventArgs e)
    {
        if (!IsPlaying || !CanPlay) return;
        var duration = Duration;
        if (duration <= 0)
        {
            IsPlaying = false;
            return;
        }

        var now = DateTime.UtcNow;
        var elapsed = Math.Clamp((now - lastPlaybackTick).TotalSeconds, 0, 0.25);
        lastPlaybackTick = now;
        var next = Position + elapsed * Math.Max(0.01, PlaybackSpeed);
        if (next >= duration)
        {
            if (Loop)
                next %= duration;
            else
            {
                next = duration;
                IsPlaying = false;
            }
        }
        Position = next;
    }

    private void UpdatePlaybackTimer()
    {
        if (assetService is not null && IsPlaying && CanPlay)
            playbackTimer.Start();
        else
            playbackTimer.Stop();
    }

    private void QueuePreviewRender()
    {
        if (disposed || assetService is null || !HasRenderedPreview || !CanPlay) return;
        if (Interlocked.Exchange(ref previewRenderInProgress, 1) != 0) return;
        _ = RenderPreviewAsync();
    }

    private async Task RenderPreviewAsync()
    {
        try
        {
            var preview = previewImagePath;
            var animation = SelectedAnimation;
            if (preview is null || animation is null) return;
            var skeleton = skeletonPath;
            var atlas = atlasPath;
            var runtime = runtimeLine;
            var time = (float)Position;
            var skin = SelectedSkin;
            await Task.Run(() => assetService!.Render(
                skeleton,
                atlas,
                runtime,
                animation,
                time,
                64,
                64,
                preview,
                true,
                false,
                string.IsNullOrWhiteSpace(skin) ? [] : [skin],
                playbackCancellation.Token), playbackCancellation.Token);
            if (preview == previewImagePath && !playbackCancellation.IsCancellationRequested)
                Changed(nameof(PreviewImagePath));
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (disposed) return;
            SetDiagnostics([new Diagnostic("error", "RENDER_FAILED", exception.Message, skeletonPath)]);
            IsPlaying = false;
            State = WorkspaceState.RendererUnavailable;
            stateDetailOverride = exception.Message;
            Changed(nameof(StateDetail));
        }
        finally
        {
            Interlocked.Exchange(ref previewRenderInProgress, 0);
        }
    }

    private static string FormatTime(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0) seconds = 0;
        var time = TimeSpan.FromSeconds(seconds);
        return $"{(int)time.TotalMinutes}:{time.Seconds:00}.{time.Milliseconds / 100}";
    }

    private static void DeletePreview(string? path)
    {
        if (path is null) return;
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        playbackTimer.Stop();
        playbackCancellation.Cancel();
        playbackCancellation.Dispose();
        ReplacePreview(null);
    }

    private void ResetEditor()
    {
        modelX = modelY = modelRotation = 0;
        modelScale = playbackSpeed = trackAlpha = 1;
        viewportZoom = 1;
        viewportPanX = viewportPanY = 0;
        position = 0;
        flipX = flipY = false;
        loop = true;
        backgroundMode = "Checkerboard";
        projectPath = null;
        undo.Clear();
        redo.Clear();
        savedSnapshot = Capture();
    }

    private bool SaveTo(string path)
    {
        try
        {
            projectPath = projectStore.Save(path, new ViewerProjectDocument(
                ViewerProjectStore.CurrentSchemaVersion,
                skeletonPath,
                atlasPath,
                SelectedAnimation,
                SelectedSkin,
                ModelX,
                ModelY,
                ModelScale,
                ModelRotation,
                FlipX,
                FlipY,
                Loop,
                PlaybackSpeed,
                TrackAlpha,
                BackgroundMode));
            savedSnapshot = Capture();
            LastAction = $"Saved {Path.GetFileName(projectPath)}";
            Changed(nameof(ProjectPath));
            Changed(nameof(ProjectPathLabel));
            Changed(nameof(IsDirty));
            Changed(nameof(DocumentTitle));
            Changed(nameof(WindowTitle));
            RefreshCommands();
            return true;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or ArgumentException or InvalidDataException)
        {
            LastAction = $"Save failed: {exception.Message}";
            return false;
        }
    }

    private void Undo()
    {
        redo.Push(Capture());
        Restore(undo.Pop());
    }

    private void Redo()
    {
        undo.Push(Capture());
        Restore(redo.Pop());
    }

    private void Restore(EditorSnapshot snapshot)
    {
        (selectedAnimation, selectedSkin, modelX, modelY, modelScale, modelRotation, flipX, flipY, loop, playbackSpeed, trackAlpha, backgroundMode) =
            (snapshot.SelectedAnimation, snapshot.SelectedSkin, snapshot.ModelX, snapshot.ModelY, snapshot.ModelScale, snapshot.ModelRotation,
                snapshot.FlipX, snapshot.FlipY, snapshot.Loop, snapshot.PlaybackSpeed, snapshot.TrackAlpha, snapshot.BackgroundMode);
        Changed(string.Empty);
        RefreshCommands();
    }

    private void Edit<T>(ref T field, T value, string propertyName, params string[] dependentProperties)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        undo.Push(Capture());
        redo.Clear();
        field = value;
        Changed(propertyName);
        foreach (var dependentProperty in dependentProperties) Changed(dependentProperty);
        Changed(nameof(IsDirty));
        Changed(nameof(DocumentTitle));
        Changed(nameof(WindowTitle));
        RefreshCommands();
    }

    private EditorSnapshot Capture() => new(
        SelectedAnimation,
        SelectedSkin,
        ModelX,
        ModelY,
        ModelScale,
        ModelRotation,
        FlipX,
        FlipY,
        Loop,
        PlaybackSpeed,
        TrackAlpha,
        BackgroundMode);

    private void RefreshCommands()
    {
        foreach (var command in new[]
                 {
                     OpenAssetCommand, ReloadCommand, ExportCommand, ScreenshotCommand, TogglePlayCommand, StopCommand, FitCommand,
                     SaveCommand, SaveAsCommand, UndoCommand, RedoCommand
                 })
            ((RelayCommand)command).Refresh();
    }

    private void Changed([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed record StateDefinition(string Title, string Detail, bool HasAsset, bool HasPreview, bool CanPlay);

    private sealed record EditorSnapshot(
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
}

internal sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => execute();
    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
