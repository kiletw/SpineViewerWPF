using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Diagnostics;
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
    private const int ExportSize = 512;
    private const int MaxPreviewWidth = 1536;
    private const int MaxPreviewHeight = 1024;
    private const double MinViewportZoom = 0.01;
    // Inset of the preview surfaces inside the viewport (XAML Margin="14").
    private const double ViewportContentInset = 14;
    private const double FitPadding = 16;
    private const double MaxViewportZoom = 8;
    private const double MaxViewportPan = 20000;
    private const int FastStepFrames = 10;
    private const string PngSequenceFormat = "PNG sequence";
    private const string CurrentAnimationScope = "Current";
    private const string AllAnimationsScope = "All animations";
    private const string FullRangeMode = "Full";
    private const string CustomRangeMode = "Custom";
    private static readonly string[] DefaultAnimations = ["idle", "walk", "attack", "victory"];
    private static readonly string[] DefaultSkins = ["default", "armor", "shadow"];
    private static readonly IReadOnlyDictionary<WorkspaceState, StateDefinition> Definitions =
        new Dictionary<WorkspaceState, StateDefinition>
        {
            [WorkspaceState.Empty] = new("No asset open", "Drop a skeleton here or choose Open asset.", false, false, false),
            [WorkspaceState.Loading] = new("Opening asset", "Resolving atlas, textures, and Runtime…", false, false, false),
            [WorkspaceState.Ready] = new("Ready", "Viewport ready.", true, true, true),
            [WorkspaceState.ReadyWithWarnings] = new("Ready with warnings", "Viewport is available; one texture uses a fallback filter.", true, true, true),
            [WorkspaceState.Unsupported] = new("Unsupported export", "No compatible Runtime could open this export.", true, false, false),
            [WorkspaceState.Failed] = new("Could not open asset", "Check the skeleton, atlas, and texture paths.", true, false, false),
            [WorkspaceState.RendererUnavailable] = new("Renderer unavailable", "Metadata loaded, but viewport rendering is not available.", true, false, false),
            [WorkspaceState.Exporting] = new("Exporting frame", "Writing PNG frames without blocking the current viewport.", true, true, false)
        };

    private readonly AssetService? assetService;
    private readonly ViewerProjectStore projectStore;
    private readonly Func<string?> chooseAssetPath;
    private readonly Func<string?> chooseAtlasPath;
    private readonly Func<string, string?> chooseProjectPath;
    private readonly Func<string?> chooseProjectPathToOpen;
    private readonly Func<string?> chooseScreenshotPath;
    private readonly Func<string?> chooseExportPath;
    private readonly Func<string, string?> chooseEncodedExportPath;
    private readonly Func<string?> chooseFfmpegPath;
    private readonly UserSettingsStore userSettings;
    private readonly Action<RenderedFrame, string>? copyImageToClipboard;
    private readonly Dispatcher dispatcher;
    private readonly SourceFileWatcher sourceWatcher;
    private readonly DispatcherTimer deferredReloadTimer;
    private readonly HashSet<string> deferredReloadPaths = new(StringComparer.OrdinalIgnoreCase);
    // TASK-074: animation mixes in progress (preview only) and the last event label.
    private readonly Dictionary<SceneLayerViewModel, LayerMix> layerMixes = [];
    private readonly DispatcherTimer eventLabelTimer;
    private string playbackEventLabel = "";
    private readonly Func<IReadOnlyList<string>?> chooseAssetPaths;
    private readonly Func<bool> confirmDiscardChanges;
    private readonly DispatcherTimer playbackTimer;
    private readonly CancellationTokenSource playbackCancellation = new();
    private readonly ObservableCollection<SceneLayerViewModel> sceneLayers = [];
    // ponytail: in-memory history is enough for one fake document; cap or persist it with multi-document editing.
    private readonly Stack<UndoEntry> undo = [];
    private readonly Stack<UndoEntry> redo = [];
    private LayerParameterClipboard? layerParameterClipboard;
    private WorkspaceState state;
    private EditorSnapshot savedSnapshot;
    private string[] animations = DefaultAnimations;
    private string[] skinNames = DefaultSkins;
    private Dictionary<string, double> animationDurations = DefaultAnimations.ToDictionary(x => x, _ => 1.8, StringComparer.Ordinal);
    private string skeletonPath = "hero.json";
    private string atlasPath = "hero.atlas";
    private string runtimeLine = "4.1";
    private IReadOnlyList<Diagnostic> diagnostics = [];
    private string? stateTitleOverride;
    private string? stateDetailOverride;
    private bool isRailExpanded;
    private bool isBrowsePanelVisible = true;
    private bool isInspectorVisible;
    private bool isDiagnosticsVisible;
    private bool isPlaying;
    private DateTime lastPlaybackTick;
    private int previewRenderInProgress;
    private int previewRenderPending;
    private long previewMetricWindowStarted = Stopwatch.GetTimestamp();
    private int previewMetricPublishedFrames;
    private int previewMetricCoalescedFrames;
    private double previewMetricWorkMilliseconds;
    private string previewPerformanceLabel = "Paused";
    private bool gpuPreviewAvailable;
    private string? gpuPreviewFailure;
    private int screenshotInProgress;
    private int exportInProgress;
    private int exportCompletedFrames;
    private bool exportEncoding;
    private int exportTotalFrames;
    private int sceneLayerOperationInProgress;
    private int loadGeneration;
    private readonly Dictionary<string, (string Animation, string Skin)> assetSelections = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? exportCancellation;
    private bool disposed;
    private bool sceneDirty;
    private SceneLayerViewModel? selectedSceneLayer;
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
    private double previewFramesPerSecond = 30;
    private double exportFramesPerSecond = 30;
    private string exportSizeMode = "Auto fit";
    private string exportFormat = PngSequenceFormat;
    private string exportAnimationScope = CurrentAnimationScope;
    private string exportRangeMode = FullRangeMode;
    private double exportRangeStart;
    private double exportRangeEnd;
    private int exportPhysicsWarmupLoops;
    private string exportVideoBackground = "#000000";
    private int exportWidth = ExportSize;
    private int exportHeight = ExportSize;
    private double exportScale = 1;
    private int exportMargin = 16;
    private string lastExportSize = "";
    private double trackAlpha = 1;
    private string backgroundMode = "Checkerboard";
    private const string DefaultBackgroundColor = "#808080";
    private string backgroundColor = DefaultBackgroundColor;
    private string themeMode = "Dark";
    private string previewChannel = "RGBA";
    private int previewPixelWidth = 768;
    private int previewPixelHeight = 768;
    // TASK-071: the preview content area in DIPs, and whether the next published
    // scene should fit the view (set when an asset or project opens).
    private double viewportContentWidth = 768 - ViewportContentInset * 2;
    private double viewportContentHeight = 768 - ViewportContentInset * 2;
    private bool fitPending;
    private string? projectPath;
    private bool isPrototypePreview = true;
    private string lastAction = "Ready";

    public ShellViewModel(
        WorkspaceState initialState,
        bool expandedWorkspace,
        ViewerProjectStore? projectStore = null,
        Func<string, string?>? chooseProjectPath = null,
        AssetService? assetService = null,
        Func<string?>? chooseAssetPath = null,
        Func<string?>? chooseAtlasPath = null,
        Func<bool>? confirmDiscardChanges = null,
        Func<string?>? chooseScreenshotPath = null,
        Func<string?>? chooseExportPath = null,
        Func<IReadOnlyList<string>?>? chooseAssetPaths = null,
        Func<string?>? chooseProjectPathToOpen = null,
        Func<string, string?>? chooseEncodedExportPath = null,
        Func<string?>? chooseFfmpegPath = null,
        UserSettingsStore? userSettings = null,
        Action<RenderedFrame, string>? copyImageToClipboard = null)
    {
        this.copyImageToClipboard = copyImageToClipboard;
        state = initialState;
        if (initialState == WorkspaceState.Empty)
        {
            animations = [];
            skinNames = [];
            animationDurations.Clear();
            skeletonPath = "";
            atlasPath = "";
            runtimeLine = "";
            selectedAnimation = null;
            selectedSkin = "default";
            isPrototypePreview = false;
            lastAction = "Ready";
        }
        isRailExpanded = expandedWorkspace;
        isInspectorVisible = expandedWorkspace;
        isPlaying = initialState is WorkspaceState.Ready or WorkspaceState.ReadyWithWarnings;
        this.projectStore = projectStore ?? new ViewerProjectStore();
        this.chooseProjectPath = chooseProjectPath ?? (_ => null);
        this.chooseProjectPathToOpen = chooseProjectPathToOpen ?? (() => null);
        this.chooseScreenshotPath = chooseScreenshotPath ?? (() => null);
        this.chooseExportPath = chooseExportPath ?? (() => null);
        this.chooseEncodedExportPath = chooseEncodedExportPath ?? (_ => null);
        this.chooseFfmpegPath = chooseFfmpegPath ?? (() => null);
        this.userSettings = userSettings ?? UserSettingsStore.InMemory();
        this.chooseAssetPaths = chooseAssetPaths ?? (() =>
        {
            var path = chooseAssetPath ?? (() => null);
            return path() is { } single ? [single] : null;
        });
        this.assetService = assetService;
        this.chooseAssetPath = chooseAssetPath ?? (() => null);
        this.chooseAtlasPath = chooseAtlasPath ?? (() => null);
        this.confirmDiscardChanges = confirmDiscardChanges ?? (() => false);
        playbackTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1d / previewFramesPerSecond) };
        playbackTimer.Tick += AdvancePlayback;
        savedSnapshot = Capture();
        // TASK-073: source changes reload the affected layers once writes settle.
        dispatcher = Dispatcher.CurrentDispatcher;
        sourceWatcher = new SourceFileWatcher(OnSourcesChanged, TimeSpan.FromMilliseconds(600));
        deferredReloadTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        deferredReloadTimer.Tick += (_, _) =>
        {
            deferredReloadTimer.Stop();
            var paths = deferredReloadPaths.ToArray();
            deferredReloadPaths.Clear();
            _ = ReloadChangedSourcesAsync(paths);
        };
        sceneLayers.CollectionChanged += (_, _) => RefreshSourceWatch();
        eventLabelTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(0.9) };
        eventLabelTimer.Tick += (_, _) =>
        {
            eventLabelTimer.Stop();
            PlaybackEventLabel = "";
        };

        OpenAssetCommand = new RelayCommand(
            async () => await OpenAssetAsync(),
            () => !IsLoading && Volatile.Read(ref screenshotInProgress) == 0);
        OpenProjectCommand = new RelayCommand(
            async () => await OpenProjectAsync(),
            () => !IsLoading && Volatile.Read(ref screenshotInProgress) == 0);
        ReloadCommand = new RelayCommand(async () =>
        {
            if (assetService is null)
                LastAction = "Fake asset reloaded";
            else
                await OpenAssetAsync(skeletonPath);
        }, () => HasAsset && !IsLoading && Volatile.Read(ref screenshotInProgress) == 0);
        OpenRecentCommand = new ParameterCommand(
            parameter => _ = OpenRecentAsync(parameter as string),
            () => !IsLoading && Volatile.Read(ref screenshotInProgress) == 0);
        ClearRecentFilesCommand = new RelayCommand(ClearRecentFiles, () => HasRecentFiles);
        ExportCommand = new RelayCommand(StartExport, () => CanExport);
        BrowseFfmpegCommand = new RelayCommand(BrowseFfmpeg);
        UseFfmpegFromPathCommand = new RelayCommand(() => SetFfmpegPath(null), () => HasCustomFfmpegPath);
        CancelExportCommand = new RelayCommand(CancelExport, () => IsExporting);
        AddLayerCommand = new RelayCommand(StartAddLayer, () => CanAddLayer);
        AutoLayoutCommand = new RelayCommand(AutoLayoutLayers, () => sceneLayers.Count > 1 && !IsLoading);
        RemoveLayerCommand = new RelayCommand(RemoveSelectedLayer, () => CanRemoveLayer);
        MoveLayerUpCommand = new RelayCommand(() => MoveSelectedLayer(-1), () => CanMoveSelectedLayer(-1));
        MoveLayerDownCommand = new RelayCommand(() => MoveSelectedLayer(1), () => CanMoveSelectedLayer(1));
        DuplicateLayerCommand = new RelayCommand(StartDuplicateLayer, () => CanDuplicateLayer);
        ReloadLayerCommand = new RelayCommand(StartReloadLayer, () => CanReloadLayer);
        CopyAllLayerParametersCommand = new RelayCommand(() => CopyLayerParameters(LayerParameterScope.All), () => HasSelectedSceneLayer);
        CopyTransformParametersCommand = new RelayCommand(() => CopyLayerParameters(LayerParameterScope.Transform), () => HasSelectedSceneLayer);
        CopyRenderParametersCommand = new RelayCommand(() => CopyLayerParameters(LayerParameterScope.Render), () => HasSelectedSceneLayer);
        CopyAppearanceParametersCommand = new RelayCommand(() => CopyLayerParameters(LayerParameterScope.Appearance), () => HasSelectedSceneLayer);
        CopySlotParametersCommand = new RelayCommand(() => CopyLayerParameters(LayerParameterScope.Slots), () => HasSelectedSceneLayer);
        ShowFilteredSlotsCommand = new RelayCommand(() => ApplySlotBatch(SlotBatchAction.Show), () => HasSelectedSceneLayer);
        HideFilteredSlotsCommand = new RelayCommand(() => ApplySlotBatch(SlotBatchAction.Hide), () => HasSelectedSceneLayer);
        ClearFilteredSlotAttachmentsCommand = new RelayCommand(() => ApplySlotBatch(SlotBatchAction.ClearAttachments), () => HasSelectedSceneLayer);
        PasteLayerParametersCommand = new RelayCommand(PasteLayerParameters, () => HasSelectedSceneLayer && layerParameterClipboard is not null);
        ScreenshotCommand = new RelayCommand(
            CaptureScreenshot,
            () => HasRenderedPreview
                && Definition.CanPlay
                && !IsExporting
                && Volatile.Read(ref screenshotInProgress) == 0
                && Volatile.Read(ref exportInProgress) == 0
                && Volatile.Read(ref sceneLayerOperationInProgress) == 0);
        CopyScreenshotCommand = new RelayCommand(
            CopyScreenshot,
            () => this.copyImageToClipboard is not null && ScreenshotCommand.CanExecute(null));
        TogglePlayCommand = new RelayCommand(() => IsPlaying = !IsPlaying, () => CanPlay);
        StopCommand = new RelayCommand(() =>
        {
            IsPlaying = false;
            Position = 0;
        }, () => CanPlay);
        RestartCommand = new RelayCommand(() =>
        {
            Position = 0;
            IsPlaying = true;
        }, () => CanPlay);
        PreviousFrameCommand = new RelayCommand(() => StepFrames(-1), () => CanPlay);
        NextFrameCommand = new RelayCommand(() => StepFrames(1), () => CanPlay);
        BackTenFramesCommand = new RelayCommand(() => StepFrames(-FastStepFrames), () => CanPlay);
        ForwardTenFramesCommand = new RelayCommand(() => StepFrames(FastStepFrames), () => CanPlay);
        FitCommand = new RelayCommand(FitViewport, () => HasPreview);
        DiagnosticsCommand = new RelayCommand(ToggleDiagnostics);
        ToggleRailCommand = new RelayCommand(() => IsRailExpanded = !IsRailExpanded);
        ToggleBrowsePanelCommand = new RelayCommand(() => IsBrowsePanelVisible = !IsBrowsePanelVisible);
        ToggleInspectorCommand = new RelayCommand(() => IsInspectorVisible = !IsInspectorVisible);
        SaveCommand = new RelayCommand(() => TrySave(), () => HasAsset && (IsDirty || ProjectPath is null));
        SaveAsCommand = new RelayCommand(() => TrySaveAs(), () => HasAsset);
        UndoCommand = new RelayCommand(Undo, () => undo.Count > 0);
        RedoCommand = new RelayCommand(Redo, () => redo.Count > 0);
        UpdatePlaybackTimer();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<string> FilteredAnimations =>
        (selectedSceneLayer?.Animations ?? animations)
        .Where(x => x.Contains(AnimationFilter, StringComparison.OrdinalIgnoreCase))
        .ToArray();
    public IReadOnlyList<string> Skins => skinNames;
    public ObservableCollection<SceneLayerViewModel> SceneLayers => sceneLayers;
    public SceneLayerViewModel? SelectedSceneLayer
    {
        get => selectedSceneLayer;
        set
        {
            if (ReferenceEquals(selectedSceneLayer, value)) return;
            selectedSceneLayer = value;
            Changed();
            Changed(nameof(HasSelectedSceneLayer));
            Changed(nameof(TimelineEventMarkers));
            Changed(nameof(FilteredAnimations));
            Changed(nameof(HasNoAnimations));
            Changed(nameof(Duration));
            Changed(nameof(PlaybackTimeLabel));
            RefreshCommands();
        }
    }
    public bool HasSelectedSceneLayer => selectedSceneLayer is not null;
    public bool HasNoAnimations => HasSelectedSceneLayer && selectedSceneLayer!.Animations.Count == 0;
    public IReadOnlyList<string> BackgroundModes { get; } = ["Checkerboard", "Dark", "Light", "Custom"];
    public IReadOnlyList<string> ThemeModes { get; } = ["Dark", "Light"];
    public IReadOnlyList<string> PreviewChannels { get; } = ["RGBA", "RGB", "Alpha"];

    public ICommand OpenAssetCommand { get; }
    public ICommand OpenProjectCommand { get; }
    public ICommand ReloadCommand { get; }
    public ICommand OpenRecentCommand { get; }
    public ICommand ClearRecentFilesCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand BrowseFfmpegCommand { get; }
    public ICommand UseFfmpegFromPathCommand { get; }
    public ICommand CancelExportCommand { get; }
    public ICommand AddLayerCommand { get; }
    public ICommand AutoLayoutCommand { get; }
    public ICommand RemoveLayerCommand { get; }
    public ICommand MoveLayerUpCommand { get; }
    public ICommand MoveLayerDownCommand { get; }
    public ICommand DuplicateLayerCommand { get; }
    public ICommand ReloadLayerCommand { get; }
    public ICommand CopyAllLayerParametersCommand { get; }
    public ICommand CopyTransformParametersCommand { get; }
    public ICommand CopyRenderParametersCommand { get; }
    public ICommand CopyAppearanceParametersCommand { get; }
    public ICommand CopySlotParametersCommand { get; }
    public ICommand PasteLayerParametersCommand { get; }
    public ICommand ShowFilteredSlotsCommand { get; }
    public ICommand HideFilteredSlotsCommand { get; }
    public ICommand ClearFilteredSlotAttachmentsCommand { get; }
    public ICommand ScreenshotCommand { get; }
    public ICommand CopyScreenshotCommand { get; }
    public ICommand TogglePlayCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand RestartCommand { get; }
    public ICommand PreviousFrameCommand { get; }
    public ICommand NextFrameCommand { get; }
    public ICommand BackTenFramesCommand { get; }
    public ICommand ForwardTenFramesCommand { get; }
    public ICommand FitCommand { get; }
    public ICommand DiagnosticsCommand { get; }
    public ICommand ToggleRailCommand { get; }
    public ICommand ToggleBrowsePanelCommand { get; }
    public ICommand ToggleInspectorCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand SaveAsCommand { get; }
    public ICommand UndoCommand { get; }
    public ICommand RedoCommand { get; }

    public WorkspaceState State
    {
        get => state;
        set
        {
            if (state == value) return;
            state = value;
            stateTitleOverride = null;
            stateDetailOverride = null;
            isPlaying = (value is WorkspaceState.Ready or WorkspaceState.ReadyWithWarnings) && Duration > 0;
            isDiagnosticsVisible = false;
            lastPlaybackTick = DateTime.UtcNow;
            position = 0;
            ResetPreviewMetrics();
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

    public bool IsBrowsePanelVisible
    {
        get => isBrowsePanelVisible;
        set
        {
            if (isBrowsePanelVisible == value) return;
            isBrowsePanelVisible = value;
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
            if (!value) layerMixes.Clear();
            lastPlaybackTick = DateTime.UtcNow;
            ResetPreviewMetrics();
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
            if (value is null || !animationDurations.ContainsKey(value) || selectedAnimation == value) return;
            var primary = sceneLayers.FirstOrDefault();
            var (mixFrom, mixFromTime) = (primary?.Animation, position);
            Edit(ref selectedAnimation, value, nameof(SelectedAnimation), nameof(Duration), nameof(PlaybackTimeLabel), nameof(StateDetail));
            SyncPrimaryLayerPlayback();
            if (primary is not null && mixFrom is not null) StartMix(primary, mixFrom, mixFromTime);
            Position = 0;
            QueuePreviewRender();
        }
    }

    public string SelectedSkin
    {
        get => selectedSkin;
        set
        {
            if (value is null || !skinNames.Contains(value, StringComparer.Ordinal) || selectedSkin == value) return;
            Edit(ref selectedSkin, value, nameof(SelectedSkin));
            SyncPrimaryLayerPlayback();
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
        set
        {
            Edit(ref modelX, value, nameof(ModelX), nameof(PreviewX));
            SyncPrimaryLayerTransform();
        }
    }

    public double ModelY
    {
        get => modelY;
        set
        {
            Edit(ref modelY, value, nameof(ModelY), nameof(PreviewY));
            SyncPrimaryLayerTransform();
        }
    }

    public double ModelScale
    {
        get => modelScale;
        set
        {
            Edit(ref modelScale, value, nameof(ModelScale), nameof(PreviewScaleX), nameof(PreviewScaleY));
            SyncPrimaryLayerTransform();
        }
    }

    public double ModelRotation
    {
        get => modelRotation;
        set
        {
            Edit(ref modelRotation, value, nameof(ModelRotation));
            SyncPrimaryLayerTransform();
        }
    }

    public bool FlipX
    {
        get => flipX;
        set
        {
            Edit(ref flipX, value, nameof(FlipX), nameof(PreviewScaleX));
            SyncPrimaryLayerTransform();
        }
    }

    public bool FlipY
    {
        get => flipY;
        set
        {
            Edit(ref flipY, value, nameof(FlipY), nameof(PreviewScaleY));
            SyncPrimaryLayerTransform();
        }
    }

    public bool Loop
    {
        get => loop;
        set => Edit(ref loop, value, nameof(Loop));
    }

    public double PlaybackSpeed
    {
        get => playbackSpeed;
        set
        {
            var next = Math.Clamp(double.IsFinite(value) ? value : 1, 0.01, 10);
            Edit(ref playbackSpeed, next, nameof(PlaybackSpeed));
        }
    }

    public double ExportFramesPerSecond
    {
        get => exportFramesPerSecond;
        set
        {
            var next = Math.Clamp(double.IsFinite(value) ? value : 30, 1, 240);
            if (Math.Abs(exportFramesPerSecond - next) < 0.001) return;
            exportFramesPerSecond = next;
            Changed(nameof(ExportFramesPerSecond));
        }
    }

    // TASK-063: export framing is a session preference like Export FPS; it does
    // not dirty the Viewer project or enter Undo/Redo history.
    public IReadOnlyList<string> ExportSizeModes { get; } = ["Auto fit", "Fixed size"];

    public string ExportSizeMode
    {
        get => exportSizeMode;
        set
        {
            var next = ExportSizeModes.Contains(value, StringComparer.Ordinal) ? value : "Auto fit";
            if (exportSizeMode == next) return;
            exportSizeMode = next;
            Changed();
            Changed(nameof(IsExportAutoFit));
            Changed(nameof(IsExportFixedSize));
            Changed(nameof(IsExportFrameVisible));
            Changed(nameof(ExportSizeSummary));
        }
    }

    public bool IsExportAutoFit => exportSizeMode == "Auto fit";
    public bool IsExportFixedSize => !IsExportAutoFit;

    public int ExportWidth
    {
        get => exportWidth;
        set
        {
            var next = Math.Clamp(value, 1, 4096);
            if (exportWidth == next) return;
            exportWidth = next;
            Changed();
            Changed(nameof(ExportSizeSummary));
        }
    }

    public int ExportHeight
    {
        get => exportHeight;
        set
        {
            var next = Math.Clamp(value, 1, 4096);
            if (exportHeight == next) return;
            exportHeight = next;
            Changed();
            Changed(nameof(ExportSizeSummary));
        }
    }

    public double ExportScale
    {
        get => exportScale;
        set
        {
            var next = Math.Clamp(double.IsFinite(value) ? value : 1, 0.01, 16);
            if (Math.Abs(exportScale - next) < 0.0001) return;
            exportScale = next;
            Changed();
            Changed(nameof(ExportSizeSummary));
        }
    }

    public int ExportMargin
    {
        get => exportMargin;
        set
        {
            var next = Math.Clamp(value, 0, 1024);
            if (exportMargin == next) return;
            exportMargin = next;
            Changed();
            Changed(nameof(ExportSizeSummary));
        }
    }

    // TASK-072: animation scope and frame range are session preferences like the
    // other export settings. A custom range applies to the current animation only;
    // All animations exports each animation of the selected layer in full.
    public IReadOnlyList<string> ExportAnimationScopes { get; } = [CurrentAnimationScope, AllAnimationsScope];

    public string ExportAnimationScope
    {
        get => exportAnimationScope;
        set
        {
            var next = ExportAnimationScopes.Contains(value, StringComparer.Ordinal) ? value : CurrentAnimationScope;
            if (exportAnimationScope == next) return;
            exportAnimationScope = next;
            Changed();
            Changed(nameof(IsExportAllAnimations));
            Changed(nameof(IsExportRangeAvailable));
            Changed(nameof(IsExportCustomRange));
            Changed(nameof(ExportSizeSummary));
        }
    }

    public bool IsExportAllAnimations => exportAnimationScope == AllAnimationsScope;
    public bool IsExportRangeAvailable => !IsExportAllAnimations;

    public IReadOnlyList<string> ExportRangeModes { get; } = [FullRangeMode, CustomRangeMode];

    public string ExportRangeMode
    {
        get => exportRangeMode;
        set
        {
            var next = ExportRangeModes.Contains(value, StringComparer.Ordinal) ? value : FullRangeMode;
            if (exportRangeMode == next) return;
            exportRangeMode = next;
            // Start a new custom range from the whole current animation.
            if (next == CustomRangeMode && exportRangeEnd <= exportRangeStart)
            {
                exportRangeStart = 0;
                exportRangeEnd = Duration;
                Changed(nameof(ExportRangeStart));
                Changed(nameof(ExportRangeEnd));
            }
            Changed();
            Changed(nameof(IsExportCustomRange));
            Changed(nameof(ExportSizeSummary));
        }
    }

    public bool IsExportCustomRange => IsExportRangeAvailable && exportRangeMode == CustomRangeMode;

    public double ExportRangeStart
    {
        get => exportRangeStart;
        set
        {
            var next = Math.Max(0, double.IsFinite(value) ? value : 0);
            if (Math.Abs(exportRangeStart - next) < 0.0001) return;
            exportRangeStart = next;
            Changed();
            Changed(nameof(ExportSizeSummary));
        }
    }

    public double ExportRangeEnd
    {
        get => exportRangeEnd;
        set
        {
            var next = Math.Max(0, double.IsFinite(value) ? value : 0);
            if (Math.Abs(exportRangeEnd - next) < 0.0001) return;
            exportRangeEnd = next;
            Changed();
            Changed(nameof(ExportSizeSummary));
        }
    }

    // Loops of the animation played before frame 0 so Physics (Spine 4.2+)
    // settles; assets without Physics ignore it.
    public int ExportPhysicsWarmupLoops
    {
        get => exportPhysicsWarmupLoops;
        set
        {
            var next = Math.Clamp(value, 0, 10);
            if (exportPhysicsWarmupLoops == next) return;
            exportPhysicsWarmupLoops = next;
            Changed();
        }
    }

    // The custom range clamped to the current animation: end within the duration,
    // start no later than the end.
    private (double Start, double End) EffectiveExportRange(double duration)
    {
        if (!IsExportCustomRange) return (0, duration);
        var end = Math.Clamp(exportRangeEnd, 0, duration);
        return (Math.Clamp(exportRangeStart, 0, end), end);
    }

    public string LastExportSize
    {
        get => lastExportSize;
        private set
        {
            if (lastExportSize == value) return;
            lastExportSize = value;
            Changed();
        }
    }

    public string ExportSizeSummary => (IsEncodedExport ? $"{exportFormat} · " : "")
        + (IsExportAllAnimations ? "All animations · " : "")
        + (IsExportCustomRange ? $"{exportRangeStart:0.###}–{exportRangeEnd:0.###}s · " : "")
        + (IsExportAutoFit
            ? $"Auto fit · {exportScale:0.##}× · {exportMargin}px margin"
            : $"{exportWidth} × {exportHeight}");

    // TASK-066: output format and MP4 background are session preferences; the
    // FFmpeg path is a user setting remembered across sessions (ADR-010).
    public IReadOnlyList<string> ExportFormats { get; } = [PngSequenceFormat, "GIF", "WebP", "APNG", "MP4"];

    public string ExportFormat
    {
        get => exportFormat;
        set
        {
            var next = ExportFormats.Contains(value, StringComparer.Ordinal) ? value : PngSequenceFormat;
            if (exportFormat == next) return;
            exportFormat = next;
            Changed();
            Changed(nameof(IsEncodedExport));
            Changed(nameof(IsVideoExport));
            Changed(nameof(ExportSizeSummary));
            Changed(nameof(FfmpegStatus));
        }
    }

    public bool IsEncodedExport => EncodeFormat(exportFormat) is not null;
    public bool IsVideoExport => EncodeFormat(exportFormat) == AnimationEncodeFormat.Mp4;

    public string ExportVideoBackground
    {
        get => exportVideoBackground;
        set
        {
            var next = (value ?? "").Trim();
            if (!next.StartsWith('#')) next = "#" + next;
            if (next.Length != 7 || !next.Skip(1).All(Uri.IsHexDigit) || exportVideoBackground == next.ToUpperInvariant())
            {
                Changed();
                return;
            }
            exportVideoBackground = next.ToUpperInvariant();
            Changed();
        }
    }

    public string? CustomFfmpegPath => userSettings.FfmpegPath;
    public string? ResolvedFfmpegPath => FfmpegLocator.Find(userSettings.FfmpegPath);
    public bool HasCustomFfmpegPath => !string.IsNullOrWhiteSpace(userSettings.FfmpegPath);

    public string FfmpegStatus => ResolvedFfmpegPath is not { } resolved
        ? HasCustomFfmpegPath ? $"Not found: {userSettings.FfmpegPath}" : "Not found on PATH"
        : HasCustomFfmpegPath && string.Equals(resolved, Path.GetFullPath(userSettings.FfmpegPath!), StringComparison.OrdinalIgnoreCase)
            ? resolved
            : $"PATH: {resolved}";

    private static AnimationEncodeFormat? EncodeFormat(string format) => format switch
    {
        "GIF" => AnimationEncodeFormat.Gif,
        "WebP" => AnimationEncodeFormat.WebP,
        "APNG" => AnimationEncodeFormat.Apng,
        "MP4" => AnimationEncodeFormat.Mp4,
        _ => null
    };

    private void BrowseFfmpeg()
    {
        if (chooseFfmpegPath() is not { } path) return;
        SetFfmpegPath(path);
    }

    private void SetFfmpegPath(string? path)
    {
        if (!userSettings.SetFfmpegPath(path))
            LastAction = "FFmpeg path could not be saved; using it for this session";
        else
            LastAction = path is null ? "Using FFmpeg from PATH" : "FFmpeg path saved";
        Changed(nameof(CustomFfmpegPath));
        Changed(nameof(ResolvedFfmpegPath));
        Changed(nameof(HasCustomFfmpegPath));
        Changed(nameof(FfmpegStatus));
        RefreshCommands();
    }

    public double PreviewFramesPerSecond
    {
        get => previewFramesPerSecond;
        set
        {
            var next = Math.Clamp(double.IsFinite(value) ? value : 30, 1, 240);
            if (Math.Abs(previewFramesPerSecond - next) < 0.001) return;
            previewFramesPerSecond = next;
            UpdatePlaybackTimer();
            Changed(nameof(PreviewFramesPerSecond));
        }
    }

    public double TrackAlpha
    {
        get => trackAlpha;
        set => Edit(ref trackAlpha, value, nameof(TrackAlpha));
    }

    public string BackgroundMode
    {
        get => backgroundMode;
        set => Edit(ref backgroundMode, value ?? "Checkerboard", nameof(BackgroundMode), nameof(IsCustomBackground));
    }

    // TASK-075: #RRGGBB used when BackgroundMode is "Custom"; an invalid entry
    // is ignored and the field shows the previous color again.
    public string BackgroundColor
    {
        get => backgroundColor;
        set
        {
            var next = NormalizeHexColor(value);
            if (next is null)
            {
                Changed();
                return;
            }
            Edit(ref backgroundColor, next, nameof(BackgroundColor));
        }
    }

    public bool IsCustomBackground => backgroundMode == "Custom";

    private static string? NormalizeHexColor(string? value)
    {
        var text = (value ?? "").Trim();
        if (!text.StartsWith('#')) text = "#" + text;
        return text.Length == 7 && text.Skip(1).All(Uri.IsHexDigit) ? text.ToUpperInvariant() : null;
    }

    public string ThemeMode
    {
        get => themeMode;
        set
        {
            var next = ThemeModes.Contains(value, StringComparer.Ordinal) ? value : "Dark";
            if (themeMode == next) return;
            themeMode = next;
            Changed();
        }
    }

    public string PreviewChannel
    {
        get => previewChannel;
        set
        {
            var next = PreviewChannels.Contains(value, StringComparer.Ordinal) ? value : "RGBA";
            if (previewChannel == next) return;
            previewChannel = next;
            Changed();
            Changed(nameof(UseGpuPreview));
            Changed(nameof(HasGpuPreview));
            Changed(nameof(IsCpuPreviewVisible));
            UpdatePlaybackTimer();
            QueuePreviewRender();
        }
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
    public bool HasRenderedPreview => sceneLayers.Any(layer => layer.PreviewFrame is not null);
    public bool UseGpuPreview => gpuPreviewAvailable && PreviewChannel == "RGBA";
    public bool HasGpuPreview => UseGpuPreview && sceneLayers.Any(layer => layer.PreviewScene is not null);
    public bool IsCpuPreviewVisible => !HasGpuPreview;
    public string PreviewBackendLabel => UseGpuPreview ? "GPU" : "CPU fallback";
    public string PreviewPerformanceLabel => previewPerformanceLabel;
    public bool HasPrototypePreview => HasPreview && isPrototypePreview;
    public bool CanPlay => Definition.CanPlay && sceneLayers.Count > 0 && Duration > 0;
    public bool CanExport => assetService is not null
        && HasRenderedPreview && Definition.CanPlay
        && !IsExporting
        && Volatile.Read(ref screenshotInProgress) == 0
        && Volatile.Read(ref exportInProgress) == 0;
    public bool CanAddLayer => assetService is not null && sceneLayers.Count < 8 && !IsLoading && Volatile.Read(ref sceneLayerOperationInProgress) == 0;
    public bool CanRemoveLayer => selectedSceneLayer is not null
        && sceneLayers.Count > 1
        && !IsLoading
        && Volatile.Read(ref screenshotInProgress) == 0;
    public bool CanDuplicateLayer => CanAddLayer && selectedSceneLayer is not null;
    public bool CanReloadLayer => assetService is not null
        && selectedSceneLayer is not null
        && !IsLoading
        && Volatile.Read(ref screenshotInProgress) == 0
        && Volatile.Read(ref sceneLayerOperationInProgress) == 0;
    public double ExportProgress => exportTotalFrames == 0 ? 0 : (double)exportCompletedFrames / exportTotalFrames;
    public string ExportProgressLabel => exportTotalFrames == 0
        ? "Exporting…"
        : !exportEncoding
            ? $"Exporting {exportCompletedFrames}/{exportTotalFrames}"
            : exportCompletedFrames < exportTotalFrames / 2
                ? $"Rendering {exportCompletedFrames}/{exportTotalFrames / 2}"
                : $"Encoding {exportCompletedFrames - exportTotalFrames / 2}/{exportTotalFrames / 2}";
    public bool IsDirty => sceneDirty || Capture() != savedSnapshot;
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
    public RenderedFrame? PreviewFrame => sceneLayers.FirstOrDefault()?.PreviewFrame;
    public int PreviewPixelWidth => previewPixelWidth;
    public int PreviewPixelHeight => previewPixelHeight;
    public string PreviewResolutionLabel => HasRenderedPreview ? $"{previewPixelWidth} × {previewPixelHeight}" : "";
    public double PreviewX => ModelX * ViewportZoom + ViewportPanX;
    public double PreviewY => ModelY * ViewportZoom + ViewportPanY;
    public double ViewportZoom => viewportZoom;
    public string ViewportZoomLabel => $"{Math.Round(viewportZoom * 100):0}%";
    public double ViewportPanX => viewportPanX;
    public double ViewportPanY => viewportPanY;
    public double PreviewScaleX => ModelScale * ViewportZoom * (FlipX ? -1 : 1);
    public double PreviewScaleY => ModelScale * ViewportZoom * (FlipY ? -1 : 1);
    public double Duration => SelectedSceneLayer?.Duration
        ?? (SelectedAnimation is not null && animationDurations.TryGetValue(SelectedAnimation, out var duration) ? duration : 0);
    public string PlaybackTimeLabel => $"{FormatTime(Position)} / {FormatTime(Duration)}";

    public void SetGpuPreviewAvailable(bool available, string? failure = null)
    {
        if (gpuPreviewAvailable == available && failure is null) return;
        gpuPreviewAvailable = available;
        gpuPreviewFailure = available ? null : failure ?? gpuPreviewFailure;
        SetDiagnostics(diagnostics.Where(item => item.Code != "GPU_FALLBACK").ToArray());
        if (!available && !string.IsNullOrWhiteSpace(gpuPreviewFailure))
        {
            LastAction = "GPU unavailable; using CPU preview";
        }
        Changed(nameof(UseGpuPreview));
        Changed(nameof(HasGpuPreview));
        Changed(nameof(IsCpuPreviewVisible));
        Changed(nameof(PreviewBackendLabel));
        ResetPreviewMetrics();
        UpdatePlaybackTimer();
        QueuePreviewRender();
    }

    public void SetViewportSize(double physicalWidth, double physicalHeight, double dpiScale = 1)
    {
        if (!double.IsFinite(physicalWidth) || !double.IsFinite(physicalHeight)
            || physicalWidth < 1 || physicalHeight < 1)
            return;

        if (double.IsFinite(dpiScale) && dpiScale > 0)
        {
            viewportContentWidth = Math.Max(1, physicalWidth / dpiScale - ViewportContentInset * 2);
            viewportContentHeight = Math.Max(1, physicalHeight / dpiScale - ViewportContentInset * 2);
        }
        var scale = Math.Min(1, Math.Min(MaxPreviewWidth / physicalWidth, MaxPreviewHeight / physicalHeight));
        var width = Math.Clamp((int)Math.Round(physicalWidth * scale), 256, MaxPreviewWidth);
        var height = Math.Clamp((int)Math.Round(physicalHeight * scale), 256, MaxPreviewHeight);
        if (previewPixelWidth == width && previewPixelHeight == height) return;

        previewPixelWidth = width;
        previewPixelHeight = height;
        Changed(nameof(PreviewPixelWidth));
        Changed(nameof(PreviewPixelHeight));
        Changed(nameof(PreviewResolutionLabel));
        QueuePreviewRender();
    }

    public void ZoomViewport(double factor)
    {
        if (!double.IsFinite(factor) || factor <= 0) return;
        var next = Math.Clamp(viewportZoom * factor, MinViewportZoom, MaxViewportZoom);
        if (Math.Abs(viewportZoom - next) < 0.001) return;
        viewportZoom = next;
        NotifyViewportChanged();
    }

    // TASK-060: zoom around a point given in device-independent pixels relative
    // to the viewport center, so the content under the cursor stays in place.
    public void ZoomViewportAt(double factor, double anchorX, double anchorY)
    {
        if (!double.IsFinite(factor) || factor <= 0
            || !double.IsFinite(anchorX) || !double.IsFinite(anchorY)) return;
        var next = Math.Clamp(viewportZoom * factor, MinViewportZoom, MaxViewportZoom);
        if (Math.Abs(viewportZoom - next) < 0.001) return;
        var applied = next / viewportZoom;
        viewportZoom = next;
        viewportPanX = Math.Clamp(anchorX - (anchorX - viewportPanX) * applied, -MaxViewportPan, MaxViewportPan);
        viewportPanY = Math.Clamp(anchorY - (anchorY - viewportPanY) * applied, -MaxViewportPan, MaxViewportPan);
        NotifyViewportChanged();
    }

    public void PanViewport(double deltaX, double deltaY)
    {
        if (!double.IsFinite(deltaX) || !double.IsFinite(deltaY)) return;
        var nextX = Math.Clamp(viewportPanX + deltaX, -MaxViewportPan, MaxViewportPan);
        var nextY = Math.Clamp(viewportPanY + deltaY, -MaxViewportPan, MaxViewportPan);
        var changed = Math.Abs(viewportPanX - nextX) >= 0.001 || Math.Abs(viewportPanY - nextY) >= 0.001;
        viewportPanX = nextX;
        viewportPanY = nextY;
        if (changed) NotifyViewportChanged();
    }

    // TASK-065: pan so the located content center of the layer sits at the
    // viewport center. The locator returns DIP offsets relative to the viewport
    // center without pan, or null when the layer has no GPU scene to measure.
    public bool FocusLayer(SceneLayerViewModel layer, Func<SceneLayerViewModel, (double X, double Y)?> locateContentCenter)
    {
        if (!sceneLayers.Contains(layer)) return false;
        SelectedSceneLayer = layer;
        if (locateContentCenter(layer) is not { } center
            || !double.IsFinite(center.X) || !double.IsFinite(center.Y))
        {
            LastAction = "Layer focus needs a rendered layer";
            return false;
        }
        var nextX = Math.Clamp(-center.X, -MaxViewportPan, MaxViewportPan);
        var nextY = Math.Clamp(-center.Y, -MaxViewportPan, MaxViewportPan);
        var changed = Math.Abs(viewportPanX - nextX) >= 0.001 || Math.Abs(viewportPanY - nextY) >= 0.001;
        viewportPanX = nextX;
        viewportPanY = nextY;
        if (changed) NotifyViewportChanged();
        LastAction = $"Focused {Path.GetFileName(layer.SkeletonPath)}";
        return true;
    }

    // TASK-065: move by whole preview frames on the 1 / PreviewFramesPerSecond
    // grid; stepping pauses playback and clamps without wrapping.
    private void StepFrames(int frames)
    {
        if (!CanPlay || frames == 0) return;
        IsPlaying = false;
        var framesPerSecond = Math.Max(1, previewFramesPerSecond);
        var exact = Position * framesPerSecond;
        var target = frames > 0
            ? Math.Floor(exact + 0.0001) + frames
            : Math.Ceiling(exact - 0.0001) + frames;
        Position = Math.Clamp(target / framesPerSecond, 0, Duration);
    }

    private void ApplySlotBatch(SlotBatchAction action)
    {
        if (selectedSceneLayer is null) return;
        var count = selectedSceneLayer.ApplySlotBatch(action);
        var slots = count == 1 ? "1 slot" : $"{count} slots";
        LastAction = count == 0
            ? "No slots changed"
            : action switch
            {
                SlotBatchAction.Show => $"Showed {slots}",
                SlotBatchAction.Hide => $"Hid {slots}",
                _ => $"Cleared attachments on {slots}"
            };
    }

    private void FitViewport()
    {
        FitToContent();
        LastAction = "Viewport fitted";
    }

    // TASK-071: zoom (never above 100%) and pan so the visible layers' current
    // content fits the preview area with padding; no content resets the view.
    private void FitToContent()
    {
        var bounds = SceneContentBounds();
        double zoom = 1, panX = 0, panY = 0;
        if (bounds is { } box)
        {
            var width = Math.Max(1e-6, box.MaxX - box.MinX);
            var height = Math.Max(1e-6, box.MaxY - box.MinY);
            zoom = Math.Clamp(
                Math.Min(1, Math.Min(
                    Math.Max(1, viewportContentWidth - FitPadding * 2) / width,
                    Math.Max(1, viewportContentHeight - FitPadding * 2) / height)),
                MinViewportZoom,
                MaxViewportZoom);
            panX = Math.Clamp(-(box.MinX + box.MaxX) / 2 * zoom, -MaxViewportPan, MaxViewportPan);
            panY = Math.Clamp(-(box.MinY + box.MaxY) / 2 * zoom, -MaxViewportPan, MaxViewportPan);
        }
        var changed = Math.Abs(viewportZoom - zoom) >= 0.0001 || Math.Abs(viewportPanX - panX) >= 0.001 || Math.Abs(viewportPanY - panY) >= 0.001;
        viewportZoom = zoom;
        viewportPanX = panX;
        viewportPanY = panY;
        if (changed) NotifyViewportChanged();
    }

    // Scene-space union of the visible layers' last published content bounds.
    private (double MinX, double MinY, double MaxX, double MaxY)? SceneContentBounds()
    {
        (double MinX, double MinY, double MaxX, double MaxY)? union = null;
        foreach (var layer in sceneLayers)
        {
            if (!layer.IsVisible || layer.PreviewScene is not { } scene || !ViewportMath.HasBounds(scene)) continue;
            if (SceneCamera.BoundsToScene(layer.ToDocument(), scene.BoundsX, scene.BoundsY, scene.BoundsWidth, scene.BoundsHeight) is not { } box)
                continue;
            union = union is { } current
                ? (Math.Min(current.MinX, box.MinX), Math.Min(current.MinY, box.MinY), Math.Max(current.MaxX, box.MaxX), Math.Max(current.MaxY, box.MaxY))
                : box;
        }
        return union;
    }

    // An open asset or project fits once every visible layer has a scene.
    private void FitIfPending()
    {
        if (!fitPending || disposed) return;
        if (sceneLayers.Any(layer => layer.IsVisible && layer.PreviewScene is null)) return;
        fitPending = false;
        FitToContent();
        // The CPU frames rendered while opening predate the camera.
        if (!UseGpuPreview) QueuePreviewRender();
    }

    // TASK-071: CPU preview and Screenshot rasters cover the preview content
    // area; scene point (x, y) appears at zoom * (x, y) + pan DIPs from its center.
    private double CpuPixelsPerDip(int width, int height) =>
        Math.Max(width / viewportContentWidth, height / viewportContentHeight);

    private SceneLayerPlacement PlaceInView(SceneLayerDocument layer, int width, int height) =>
        SceneCamera.Place(
            layer,
            -viewportPanX / viewportZoom,
            -viewportPanY / viewportZoom,
            viewportZoom * CpuPixelsPerDip(width, height),
            width,
            height);

    // TASK-065 / TASK-071: focus from the layer's last published scene bounds.
    public bool FocusLayer(SceneLayerViewModel layer) =>
        FocusLayer(layer, target => target.PreviewScene is { } scene
            && ViewportMath.TryGetLayerContentCenter(
                scene, target.ModelX, target.ModelY, target.ModelScale, target.ModelRotation,
                target.FlipX, target.FlipY, viewportZoom, out var x, out var y)
                ? (x, y)
                : null);

    // Raise only viewport-derived properties instead of refreshing every binding
    // on each wheel or drag step. The CPU preview renders through the camera, so
    // it re-renders; the GPU preview only redraws.
    private void NotifyViewportChanged()
    {
        if (!UseGpuPreview) QueuePreviewRender();
        Changed(nameof(ViewportZoom));
        Changed(nameof(ViewportZoomLabel));
        Changed(nameof(ViewportPanX));
        Changed(nameof(ViewportPanY));
        Changed(nameof(PreviewX));
        Changed(nameof(PreviewY));
        Changed(nameof(PreviewScaleX));
        Changed(nameof(PreviewScaleY));
    }

    private void ToggleDiagnostics()
    {
        isDiagnosticsVisible = !isDiagnosticsVisible;
        Changed(nameof(IsDiagnosticsVisible));
    }

    private void SetDiagnostics(IReadOnlyList<Diagnostic> next)
    {
        diagnostics = string.IsNullOrWhiteSpace(gpuPreviewFailure)
            ? next.Where(item => item.Code != "GPU_FALLBACK").ToArray()
            : [
                .. next.Where(item => item.Code != "GPU_FALLBACK"),
                new Diagnostic("warning", "GPU_FALLBACK", gpuPreviewFailure)
            ];
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

    private void CaptureScreenshot() => StartScreenshot(toClipboard: false);

    // TASK-075: the same capture as Screenshot, placed on the clipboard.
    private void CopyScreenshot() => StartScreenshot(toClipboard: true);

    private void StartScreenshot(bool toClipboard)
    {
        if (toClipboard && copyImageToClipboard is null) return;
        if (!TryBeginScreenshot()) return;
        try
        {
            var target = toClipboard ? null : chooseScreenshotPath();
            if (target is null && !toClipboard)
            {
                EndScreenshot();
                return;
            }

            var snapshot = new ScreenshotSnapshot(
                (float)Position,
                previewPixelWidth,
                previewPixelHeight,
                PreviewChannel,
                playbackCancellation.Token,
                // TASK-071: what the viewport shows, through the shared scene camera.
                sceneLayers.Select(layer =>
                {
                    var document = layer.ToDocument();
                    return new ScreenshotLayerSnapshot(
                        layer.RenderSession,
                        document,
                        (float)layer.Duration,
                        PlaceInView(document, previewPixelWidth, previewPixelHeight));
                }).ToArray());
            _ = CaptureScreenshotAsync(target, snapshot);
        }
        catch
        {
            EndScreenshot();
            throw;
        }
    }

    private bool TryBeginScreenshot()
    {
        if (disposed
            || IsExporting
            || Volatile.Read(ref exportInProgress) != 0
            || Volatile.Read(ref sceneLayerOperationInProgress) != 0
            || Interlocked.Exchange(ref screenshotInProgress, 1) != 0) return false;
        if (disposed
            || IsExporting
            || Volatile.Read(ref exportInProgress) != 0
            || Volatile.Read(ref sceneLayerOperationInProgress) != 0)
        {
            EndScreenshot();
            return false;
        }
        RefreshCommands();
        return true;
    }

    private void EndScreenshot()
    {
        Interlocked.Exchange(ref screenshotInProgress, 0);
        if (!disposed) RefreshCommands();
    }

    // A null target copies the capture to the clipboard on the UI thread.
    private async Task CaptureScreenshotAsync(string? target, ScreenshotSnapshot snapshot)
    {
        try
        {
            var output = target is null ? null : Path.GetFullPath(target);
            var captured = await Task.Run(() =>
            {
                var rendered = snapshot.Layers
                    .Where(layer => layer.Document.IsVisible && layer.Document.Opacity > 0)
                    .Select(layer =>
                    {
                        snapshot.CancellationToken.ThrowIfCancellationRequested();
                        var document = layer.Document;
                        var frame = layer.RenderSession.RenderFrame(
                            document.Animation,
                            Math.Min(snapshot.TimeSeconds, layer.Duration),
                            layer.Placement.FrameWidth,
                            layer.Placement.FrameHeight,
                            document.Pma ?? false,
                            string.IsNullOrWhiteSpace(document.SelectedSkin) ? [] : [document.SelectedSkin],
                            snapshot.CancellationToken,
                            (float)(document.TrackAlpha ?? 1),
                            slots: document.Slots,
                            camera: layer.Placement.Camera);
                        return new SceneFrameLayer(frame, layer.Placement.Layer);
                    }).ToArray();
                var source = rendered.Length == 1 && IsIdentityPresentation(rendered[0].Layer)
                    ? rendered[0].Frame
                    : SceneFrameCompositor.Compose(rendered, snapshot.Width, snapshot.Height);
                if (output is not null)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(output) ?? ".");
                    PreviewFrameBitmap.SavePng(source, snapshot.Channel, output);
                }
                return source;
            }, snapshot.CancellationToken);
            if (output is null)
            {
                copyImageToClipboard!(captured, snapshot.Channel);
                LastAction = "Copied screenshot to clipboard";
            }
            else
                LastAction = $"Captured {Path.GetFileName(output)}";
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            SetDiagnostics([
                .. diagnostics.Where(item => item.Code != "CAPTURE_FAILED"),
                new Diagnostic("error", "CAPTURE_FAILED", exception.Message, target)
            ]);
            LastAction = "Screenshot failed";
        }
        finally
        {
            EndScreenshot();
        }
    }

    private void StartExport()
    {
        if (disposed
            || Volatile.Read(ref screenshotInProgress) != 0
            || Interlocked.Exchange(ref exportInProgress, 1) != 0) return;
        var encodeFormat = EncodeFormat(exportFormat);
        var target = encodeFormat is null ? chooseExportPath() : chooseEncodedExportPath(exportFormat);
        if (target is null)
        {
            Interlocked.Exchange(ref exportInProgress, 0);
            return;
        }
        var layers = sceneLayers.Select(layer => layer.ToDocument()).ToArray();
        var sceneLayerSnapshot = layers.Length == 1 && IsIdentityPresentation(layers[0])
            ? null
            : layers;
        _ = ExportSequenceAsync(target, sceneLayerSnapshot, encodeFormat);
    }

    private void StartAddLayer()
    {
        if (disposed || Interlocked.Exchange(ref sceneLayerOperationInProgress, 1) != 0) return;
        var targets = chooseAssetPaths()?.Where(path => !string.IsNullOrWhiteSpace(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (targets is null || targets.Length == 0)
        {
            Interlocked.Exchange(ref sceneLayerOperationInProgress, 0);
            return;
        }

        _ = AddLayersAsync(targets);
    }

    private void StartDuplicateLayer()
    {
        var source = selectedSceneLayer;
        if (source is null || disposed || Interlocked.Exchange(ref sceneLayerOperationInProgress, 1) != 0) return;
        _ = DuplicateLayerAsync(source);
    }

    private async Task DuplicateLayerAsync(SceneLayerViewModel source)
    {
        try
        {
            var document = source.ToDocument();
            var duplicate = await Task.Run(() => OpenLayerFromDocument(document, source.ZIndex + 1));
            if (!sceneLayers.Contains(source))
            {
                duplicate.Dispose();
                return;
            }

            sceneLayers.Insert(sceneLayers.IndexOf(source) + 1, duplicate);
            RefreshLayerZIndices();
            SelectedSceneLayer = duplicate;
            MarkSceneEditWithoutUndo();
            QueuePreviewRender();
            LastAction = $"Duplicated layer {source.DisplayName}";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            SetDiagnostics([new Diagnostic("error", "LAYER_DUPLICATE_FAILED", exception.Message, source.SkeletonPath)]);
            LastAction = "Layer duplicate failed";
        }
        finally
        {
            Interlocked.Exchange(ref sceneLayerOperationInProgress, 0);
            RefreshCommands();
        }
    }

    private void StartReloadLayer()
    {
        var source = selectedSceneLayer;
        if (source is null
            || disposed
            || Volatile.Read(ref screenshotInProgress) != 0
            || Interlocked.Exchange(ref sceneLayerOperationInProgress, 1) != 0) return;
        if (Volatile.Read(ref screenshotInProgress) != 0)
        {
            Interlocked.Exchange(ref sceneLayerOperationInProgress, 0);
            RefreshCommands();
            return;
        }
        RefreshCommands();
        _ = ReloadLayerAsync(source);
    }

    private async Task ReloadLayerAsync(SceneLayerViewModel source)
    {
        try
        {
            if (await ReloadLayerCoreAsync(source, select: true) is { } replacement)
                LastAction = $"Reloaded layer {replacement.DisplayName}";
        }
        finally
        {
            Interlocked.Exchange(ref sceneLayerOperationInProgress, 0);
            RefreshCommands();
        }
    }

    // Reopens a layer's sources with its current settings and swaps it in place.
    // The caller owns the scene-layer operation flag.
    private async Task<SceneLayerViewModel?> ReloadLayerCoreAsync(SceneLayerViewModel source, bool select)
    {
        try
        {
            var document = source.ToDocument();
            var replacement = await Task.Run(() => OpenLayerFromDocument(document, source.ZIndex));
            var index = sceneLayers.IndexOf(source);
            if (index < 0 || disposed)
            {
                replacement.Dispose();
                return null;
            }

            var wasSelected = ReferenceEquals(selectedSceneLayer, source);
            sceneLayers[index] = replacement;
            source.Dispose();
            if (select || wasSelected) SelectedSceneLayer = replacement;
            Changed(nameof(SceneLayers));
            Changed(nameof(PreviewFrame));
            Changed(nameof(HasRenderedPreview));
            QueuePreviewRender();
            return replacement;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            SetDiagnostics([new Diagnostic("error", "LAYER_RELOAD_FAILED", exception.Message, source.SkeletonPath)]);
            LastAction = "Layer reload failed";
            return null;
        }
    }

    // TASK-073: auto reload. Off, the watcher watches nothing.
    public bool IsAutoReloadEnabled
    {
        get => userSettings.AutoReload;
        set
        {
            if (userSettings.AutoReload == value) return;
            userSettings.SetAutoReload(value);
            Changed();
            RefreshSourceWatch();
        }
    }

    internal IReadOnlyCollection<string> WatchedSourceFiles => sourceWatcher.WatchedFiles;

    // TASK-071: viewport guides, remembered in user settings. The export frame
    // shows only for Fixed size, the size it describes exactly.
    public bool ShowAxes
    {
        get => userSettings.ShowAxes;
        set
        {
            if (userSettings.ShowAxes == value) return;
            userSettings.SetShowAxes(value);
            Changed();
        }
    }

    public bool ShowExportFrame
    {
        get => userSettings.ShowExportFrame;
        set
        {
            if (userSettings.ShowExportFrame == value) return;
            userSettings.SetShowExportFrame(value);
            Changed();
            Changed(nameof(IsExportFrameVisible));
        }
    }

    public bool IsExportFrameVisible => ShowExportFrame && IsExportFixedSize;

    private void RefreshSourceWatch()
    {
        if (disposed) return;
        sourceWatcher.Watch(IsAutoReloadEnabled && assetService is not null
            ? sceneLayers.SelectMany(layer => SourceFileWatcher.SourceFiles(layer.SkeletonPath, layer.AtlasPath)).ToArray()
            : []);
    }

    private void OnSourcesChanged(IReadOnlyCollection<string> paths)
    {
        if (disposed) return;
        dispatcher.BeginInvoke(() => _ = ReloadChangedSourcesAsync(paths));
    }

    // Reloads every layer that reads one of the changed files, keeping its
    // settings and the selection. While busy, the paths are retried shortly.
    internal async Task ReloadChangedSourcesAsync(IReadOnlyCollection<string> paths)
    {
        if (disposed || assetService is null || !IsAutoReloadEnabled || paths.Count == 0) return;
        var changedFiles = new HashSet<string>(paths.Select(Path.GetFullPath), StringComparer.OrdinalIgnoreCase);
        var affected = sceneLayers
            .Where(layer => SourceFileWatcher.SourceFiles(layer.SkeletonPath, layer.AtlasPath)
                .Any(file => changedFiles.Contains(Path.GetFullPath(file))))
            .ToArray();
        if (affected.Length == 0) return;
        if (IsLoading || IsExporting
            || Volatile.Read(ref screenshotInProgress) != 0
            || Interlocked.Exchange(ref sceneLayerOperationInProgress, 1) != 0)
        {
            deferredReloadPaths.UnionWith(changedFiles);
            deferredReloadTimer.Start();
            return;
        }

        RefreshCommands();
        try
        {
            var reloaded = new List<string>();
            foreach (var layer in affected)
                if (await ReloadLayerCoreAsync(layer, select: false) is { } replacement)
                    reloaded.Add(replacement.DisplayName);
            if (reloaded.Count > 0)
                LastAction = $"Auto-reloaded {string.Join(", ", reloaded.Distinct())}";
        }
        finally
        {
            Interlocked.Exchange(ref sceneLayerOperationInProgress, 0);
            RefreshCommands();
        }
    }

    // TASK-073: recent files, most recent first, remembered across sessions.
    public IReadOnlyList<string> RecentFiles => userSettings.RecentFiles;
    public bool HasRecentFiles => userSettings.RecentFiles.Count > 0;

    private void RecordRecentFile(string path)
    {
        userSettings.AddRecentFile(path);
        Changed(nameof(RecentFiles));
        Changed(nameof(HasRecentFiles));
        RefreshCommands();
    }

    private void ClearRecentFiles()
    {
        userSettings.ClearRecentFiles();
        Changed(nameof(RecentFiles));
        Changed(nameof(HasRecentFiles));
        RefreshCommands();
    }

    internal async Task OpenRecentAsync(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!File.Exists(path))
        {
            userSettings.RemoveRecentFile(path);
            Changed(nameof(RecentFiles));
            Changed(nameof(HasRecentFiles));
            RefreshCommands();
            SetDiagnostics([new Diagnostic("warning", "RECENT_FILE_MISSING", "The file no longer exists and was removed from recent files.", path)]);
            LastAction = $"Missing {Path.GetFileName(path)}";
            return;
        }
        if (path.EndsWith(ViewerProjectStore.Extension, StringComparison.OrdinalIgnoreCase))
            await OpenProjectAsync(path);
        else
            await OpenAssetAsync(path);
    }

    private SceneLayerViewModel OpenLayerFromDocument(SceneLayerDocument document, int zIndex)
    {
        AssetRenderSession? session = null;
        try
        {
            var opened = assetService!.OpenRenderSession(document.SkeletonPath, document.AtlasPath, document.RuntimeOverride);
            session = opened.Session;
            var animation = document.Animation == "" || opened.Inspection.Animations.Any(item => item.Name == document.Animation)
                ? document.Animation
                : opened.Inspection.Animations.FirstOrDefault()?.Name
                    ?? "";
            var skin = opened.Inspection.Skins.Contains(document.SelectedSkin, StringComparer.Ordinal)
                ? document.SelectedSkin
                : opened.Inspection.Skins.FirstOrDefault() ?? "default";
            var duration = opened.Inspection.Animations.FirstOrDefault(item => item.Name == animation)?.DurationSeconds ?? 0;
            var normalized = document with { Animation = animation, SelectedSkin = skin, ZIndex = zIndex };
            var frame = session.RenderFrame(
                animation,
                duration / 2,
                previewPixelWidth,
                previewPixelHeight,
                normalized.Pma ?? false,
                [skin],
                trackAlpha: (float)(normalized.TrackAlpha ?? 1),
                slots: normalized.Slots);
            var layer = new SceneLayerViewModel(opened.Inspection, animation, skin, frame, session, zIndex, SceneLayerChanged, SceneLayerChanging);
            layer.ApplyDocument(normalized);
            return layer;
        }
        catch
        {
            session?.Dispose();
            throw;
        }
    }

    private void CopyLayerParameters(LayerParameterScope scope)
    {
        if (selectedSceneLayer is null) return;
        layerParameterClipboard = new LayerParameterClipboard(scope, selectedSceneLayer.ToDocument());
        LastAction = $"Copied {scope.ToString().ToLowerInvariant()} layer parameters";
        RefreshCommands();
    }

    private void PasteLayerParameters()
    {
        if (selectedSceneLayer is null || layerParameterClipboard is null) return;
        selectedSceneLayer.ApplyParameters(layerParameterClipboard.Document, layerParameterClipboard.Scope);
        LastAction = $"Pasted {layerParameterClipboard.Scope.ToString().ToLowerInvariant()} layer parameters";
    }

    private async Task AddLayersAsync(IReadOnlyList<string> paths)
    {
        var added = 0;
        try
        {
            var start = 0;
            if (sceneLayers.Count == 0)
            {
                await OpenAssetAsync(paths[0]);
                start = 1;
                if (sceneLayers.Count == 0) return;
            }

            foreach (var path in paths.Skip(start).Take(Math.Max(0, 8 - sceneLayers.Count)))
            {
                try
                {
                    RememberCurrentSelections();
                    assetSelections.TryGetValue(Path.GetFullPath(path), out var remembered);
                    var layer = await Task.Run(() => InspectAndRenderLayer(path, remembered));
                    sceneLayers.Add(layer);
                    SelectedSceneLayer = layer;
                    added++;
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    SetDiagnostics([new Diagnostic("error", "LAYER_OPEN_FAILED", exception.Message, path)]);
                }
            }

            if (added > 0)
            {
                RefreshLayerZIndices();
                ArrangeSceneLayers();
                MarkSceneEditWithoutUndo();
                if (UseGpuPreview) QueuePreviewRender();
                else QueueSlotMetadata();
            }
            LastAction = added == 0
                ? paths.Count == 1 ? "Layer add failed" : "No additional layers added"
                : $"Added {added} layer{(added == 1 ? "" : "s")}";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            SetDiagnostics([new Diagnostic("error", "LAYER_OPEN_FAILED", exception.Message, paths[0])]);
            LastAction = "Layer add failed";
        }
        finally
        {
            Interlocked.Exchange(ref sceneLayerOperationInProgress, 0);
            RefreshCommands();
        }
    }

    private void AutoLayoutLayers()
    {
        if (sceneLayers.Count < 2) return;
        ArrangeSceneLayers();
        MarkSceneEditWithoutUndo();
        LastAction = "Scene layers arranged";
    }

    private void ArrangeSceneLayers()
    {
        var columns = (int)Math.Ceiling(Math.Sqrt(sceneLayers.Count));
        const double spacing = 72;
        var rows = (int)Math.Ceiling((double)sceneLayers.Count / columns);
        for (var index = 0; index < sceneLayers.Count; index++)
        {
            var column = index % columns;
            var row = index / columns;
            var x = (column - (columns - 1) / 2d) * spacing;
            var y = (row - (rows - 1) / 2d) * spacing;
            sceneLayers[index].SetPosition(x, y);
        }
    }

    private SceneLayerViewModel InspectAndRenderLayer(string path, (string? Animation, string? Skin) remembered)
    {
        AssetRenderSession? session = null;
        try
        {
            var opened = assetService!.OpenRenderSession(path, null, null);
            session = opened.Session;
            var result = opened.Inspection;
            var selection = ResolveSelection(result, remembered);
            var animation = result.Animations.FirstOrDefault(item => item.Name == selection.Animation);
            var skin = selection.Skin;
            var frame = session.RenderFrame(
                selection.Animation,
                (animation?.DurationSeconds ?? 0) / 2,
                previewPixelWidth,
                previewPixelHeight,
                false,
                [skin]);
            return new SceneLayerViewModel(
                result,
                selection.Animation,
                skin,
                frame,
                session,
                sceneLayers.Count,
                SceneLayerChanged,
                SceneLayerChanging);
        }
        catch
        {
            session?.Dispose();
            throw;
        }
    }

    private void RemoveSelectedLayer()
    {
        var layer = selectedSceneLayer;
        if (layer is null || sceneLayers.Count <= 1 || Volatile.Read(ref screenshotInProgress) != 0) return;
        var index = sceneLayers.IndexOf(layer);
        sceneLayers.RemoveAt(index);
        layer.Dispose();
        RefreshLayerZIndices();
        SelectedSceneLayer = sceneLayers[Math.Min(index, sceneLayers.Count - 1)];
        Changed(nameof(PreviewFrame));
        Changed(nameof(HasRenderedPreview));
        Changed(nameof(PreviewResolutionLabel));
        MarkSceneEditWithoutUndo();
        LastAction = $"Removed layer {layer.DisplayName}";
    }

    private void MoveSelectedLayer(int delta)
    {
        if (!CanMoveSelectedLayer(delta) || selectedSceneLayer is null) return;
        var index = sceneLayers.IndexOf(selectedSceneLayer);
        sceneLayers.Move(index, index + delta);
        RefreshLayerZIndices();
        MarkSceneEditWithoutUndo();
    }

    private bool CanMoveSelectedLayer(int delta)
    {
        if (selectedSceneLayer is null) return false;
        var index = sceneLayers.IndexOf(selectedSceneLayer);
        return index >= 0 && index + delta >= 0 && index + delta < sceneLayers.Count;
    }

    private void RefreshLayerZIndices()
    {
        for (var index = 0; index < sceneLayers.Count; index++)
            sceneLayers[index].SetZIndex(index);
        Changed(nameof(SceneLayers));
        RefreshCommands();
    }

    private void MarkSceneEdited()
    {
        if (disposed) return;
        sceneDirty = true;
        Changed(nameof(IsDirty));
        Changed(nameof(DocumentTitle));
        Changed(nameof(WindowTitle));
        RefreshCommands();
    }

    private void MarkSceneEditWithoutUndo()
    {
        undo.Clear();
        redo.Clear();
        MarkSceneEdited();
    }

    private void SceneLayerChanged()
    {
        foreach (var layer in sceneLayers)
        {
            if (layer.SwitchedFromAnimation is not { } from) continue;
            layer.SwitchedFromAnimation = null;
            StartMix(layer, from, position);
        }
        Changed(nameof(TimelineEventMarkers));
        RememberCurrentSelections();
        MarkSceneEdited();
        Changed(nameof(Duration));
        Changed(nameof(PlaybackTimeLabel));
        QueuePreviewRender();
    }

    private void SceneLayerChanging()
    {
        if (disposed) return;
        undo.Push(CaptureUndo());
        redo.Clear();
        RefreshCommands();
    }

    private void SyncPrimaryLayerPlayback()
    {
        if (sceneLayers.FirstOrDefault() is { } layer)
            layer.SetPlayback(SelectedAnimation ?? "", SelectedSkin);
        RememberCurrentSelections();
    }

    private void SyncPrimaryLayerTransform()
    {
        sceneLayers.FirstOrDefault()?.SetTransform(ModelX, ModelY, ModelScale, ModelRotation, FlipX, FlipY);
    }

    private static (string Animation, string Skin) ResolveSelection(
        InspectResult result, (string? Animation, string? Skin) remembered) => (
        result.Animations.Any(item => item.Name == remembered.Animation)
            ? remembered.Animation! : result.Animations.FirstOrDefault()?.Name ?? "",
        result.Skins.Contains(remembered.Skin, StringComparer.Ordinal)
            ? remembered.Skin! : result.Skins.FirstOrDefault() ?? "default");

    private void RememberCurrentSelections()
    {
        // Only live, successfully applied layers participate. Selected layer wins
        // when the same canonical asset path occurs more than once.
        foreach (var layer in sceneLayers)
            assetSelections[layer.SkeletonPath] = (layer.Animation, layer.SelectedSkin);
        if (selectedSceneLayer is { } selected && sceneLayers.Contains(selected))
            assetSelections[selected.SkeletonPath] = (selected.Animation, selected.SelectedSkin);
    }

    private void ClearSceneLayers()
    {
        layerMixes.Clear();
        foreach (var layer in sceneLayers)
            layer.Dispose();
        sceneLayers.Clear();
        selectedSceneLayer = null;
        Changed(nameof(SceneLayers));
        Changed(nameof(SelectedSceneLayer));
        Changed(nameof(HasSelectedSceneLayer));
        Changed(nameof(HasGpuPreview));
        Changed(nameof(IsCpuPreviewVisible));
    }

    private void CancelExport() => exportCancellation?.Cancel();

    private async Task ExportSequenceAsync(
        string target,
        IReadOnlyList<SceneLayerDocument>? sceneLayerSnapshot,
        AnimationEncodeFormat? encodeFormat = null)
    {
        var previousState = State;
        var wasPlaying = IsPlaying;
        using var cancellation = new CancellationTokenSource();
        exportCancellation = cancellation;
        try
        {
            State = WorkspaceState.Exporting;
            exportCompletedFrames = 0;
            exportTotalFrames = 0;
            exportEncoding = encodeFormat is not null;
            Changed(string.Empty);

            var output = Path.GetFullPath(target);
            var primaryLayer = sceneLayers.FirstOrDefault();
            // A single-layer export renders the remaining layer itself, which may
            // not be the asset that opened the workspace (for example after the
            // original layer was removed).
            var singleLayer = sceneLayerSnapshot is null ? primaryLayer : null;
            var (rangeStart, rangeEnd) = EffectiveExportRange(singleLayer?.Duration ?? Duration);
            var request = new AnimationExportRequest(
                singleLayer?.SkeletonPath ?? skeletonPath,
                singleLayer?.AtlasPath ?? atlasPath,
                singleLayer?.RuntimeOverride ?? runtimeLine,
                singleLayer?.Animation ?? SelectedSceneLayer?.Animation ?? SelectedAnimation ?? "",
                (float)rangeEnd,
                (float)ExportFramesPerSecond,
                IsExportAutoFit ? ExportSize : ExportWidth,
                IsExportAutoFit ? ExportSize : ExportHeight,
                Path.GetDirectoryName(output) ?? ".",
                Path.GetFileName(output),
                false,
                primaryLayer?.Pma ?? false,
                string.IsNullOrWhiteSpace(primaryLayer?.SelectedSkin ?? SelectedSkin)
                    ? []
                    : [primaryLayer?.SelectedSkin ?? SelectedSkin],
                (float)(primaryLayer?.TrackAlpha ?? 1),
                primaryLayer?.SlotDisplaySettings,
                sceneLayerSnapshot,
                IsExportAutoFit ? new ExportFraming((float)ExportScale, ExportMargin) : null,
                (float)rangeStart,
                exportPhysicsWarmupLoops);
            var progress = new Progress<AnimationExportProgress>(value =>
            {
                exportCompletedFrames = value.CompletedFrames;
                exportTotalFrames = value.TotalFrames;
                Changed(nameof(ExportProgress));
                Changed(nameof(ExportProgressLabel));
            });
            if (IsExportAllAnimations)
            {
                // The selected layer's animations vary; other layers keep theirs.
                var batchLayer = sceneLayerSnapshot is null ? primaryLayer : SelectedSceneLayer ?? primaryLayer;
                var batchAnimations = (batchLayer?.Animations ?? animations).Where(name => name != "").ToArray();
                if (batchAnimations.Length == 0)
                    throw new InvalidOperationException("The selected layer has no animations to export.");
                var batch = new AnimationBatchExportRequest(
                    request,
                    batchAnimations,
                    sceneLayerSnapshot is null || batchLayer is null ? null : sceneLayers.IndexOf(batchLayer));
                var options = encodeFormat is { } batchFormat
                    ? new AnimationEncodeOptions(batchFormat, ResolvedFfmpegPath ?? "", exportVideoBackground)
                    : null;
                var results = await Task.Run(
                    () => assetService!.ExportBatch(batch, output, options, cancellation.Token, progress),
                    cancellation.Token);
                LastAction = $"Exported {results.Count} animations ({results.Sum(item => item.FrameCount)} frames)";
                LastExportSize = string.Join(", ", results
                    .Where(item => item.Width > 0 && item.Height > 0)
                    .Select(item => $"{item.Width} × {item.Height}")
                    .Distinct());
            }
            else if (encodeFormat is { } format)
            {
                // The format-specific save dialog already confirmed replacing the file.
                var options = new AnimationEncodeOptions(format, ResolvedFfmpegPath ?? "", exportVideoBackground);
                var encoded = await Task.Run(
                    () => assetService!.ExportEncoded(request, options, output, true, cancellation.Token, progress),
                    cancellation.Token);
                LastAction = $"Exported {encoded.FrameCount} frames to {Path.GetFileName(encoded.OutputPath)}";
                LastExportSize = $"{encoded.Width} × {encoded.Height}";
            }
            else
            {
                var result = await Task.Run(
                    () => assetService!.Export(request, cancellation.Token, progress),
                    cancellation.Token);
                LastAction = $"Exported {result.FrameCount} frames";
                if (result.Width > 0 && result.Height > 0)
                    LastExportSize = $"{result.Width} × {result.Height}";
            }
        }
        catch (OperationCanceledException)
        {
            LastAction = "Export canceled";
        }
        catch (Exception exception)
        {
            SetDiagnostics([new Diagnostic("error", "EXPORT_FAILED", exception.Message, target)]);
            LastAction = "Export failed";
        }
        finally
        {
            exportCancellation = null;
            // TASK-064: clear the flag before re-announcing commands so Screenshot
            // and Export are evaluated as available again.
            Interlocked.Exchange(ref exportInProgress, 0);
            if (!disposed)
            {
                State = previousState;
                IsPlaying = wasPlaying;
                Changed(string.Empty);
                RefreshCommands();
            }
        }
    }

    private StateDefinition Definition => Definitions[State];

    private string SuggestedProjectPath() => Path.Combine(
        Path.GetDirectoryName(skeletonPath) ?? "",
        $"{Path.GetFileNameWithoutExtension(SkeletonFileName)}{ViewerProjectStore.Extension}");

    public async Task OpenAssetAsync(string? path = null, string? atlasPath = null)
    {
        if (Volatile.Read(ref screenshotInProgress) != 0) return;
        if (assetService is null)
        {
            ApplyFakeAsset();
            return;
        }

        path ??= chooseAssetPath();
        if (path is null || IsDirty && !confirmDiscardChanges()) return;
        RememberCurrentSelections();
        var loadId = Interlocked.Increment(ref loadGeneration);

        State = WorkspaceState.Loading;
        stateTitleOverride = $"Opening {Path.GetFileName(path)}";
        stateDetailOverride = "Resolving atlas, textures, and Runtime…";
        Changed(nameof(StateTitle));
        Changed(nameof(StateDetail));

        try
        {
            assetSelections.TryGetValue(Path.GetFullPath(path), out var remembered);
            string? atlasOverride = atlasPath;
            (InspectResult Result, AssetRenderSession? Session, RenderedFrame? Frame, string? RenderError, string Animation, string Skin) opened;
            while (true)
            {
                try
                {
                    opened = await Task.Run(() => InspectAndRender(path, atlasOverride, remembered));
                    break;
                }
                catch (Exception exception) when (atlasOverride is null && IsAtlasSelectionError(exception))
                {
                    if (disposed || loadId != Volatile.Read(ref loadGeneration)) return;
                    atlasOverride = chooseAtlasPath();
                    if (string.IsNullOrWhiteSpace(atlasOverride)) throw;
                }
            }
            if (disposed || loadId != Volatile.Read(ref loadGeneration))
            {
                opened.Session?.Dispose();
                return;
            }
            ApplyAsset(opened.Result, opened.Session, opened.Frame, opened.RenderError, opened.Animation, opened.Skin);
            RememberCurrentSelections();
            RecordRecentFile(opened.Result.Asset.SkeletonPath);
        }
        catch (NotSupportedException exception)
        {
            if (disposed || loadId != Volatile.Read(ref loadGeneration)) return;
            SetDiagnostics([new Diagnostic("error", "UNSUPPORTED_ASSET", exception.Message, path)]);
            State = WorkspaceState.Unsupported;
            stateDetailOverride = exception.Message;
            LastAction = "Asset is not supported";
            Changed(nameof(StateDetail));
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (disposed || loadId != Volatile.Read(ref loadGeneration)) return;
            SetDiagnostics([new Diagnostic("error", "OPEN_FAILED", exception.Message, path)]);
            State = WorkspaceState.Failed;
            stateDetailOverride = exception.Message;
            LastAction = "Asset open failed";
            Changed(nameof(StateDetail));
        }
    }

    private static bool IsAtlasSelectionError(Exception exception)
    {
        var message = exception.Message;
        return exception is FileNotFoundException { FileName: { } file } &&
               string.Equals(Path.GetExtension(file), ".atlas", StringComparison.OrdinalIgnoreCase)
            || message.Contains("atlas", StringComparison.OrdinalIgnoreCase);
    }

    public async Task OpenProjectAsync(string? path = null)
    {
        if (Volatile.Read(ref screenshotInProgress) != 0) return;
        if (assetService is null)
        {
            LastAction = "Project open requires a Runtime adapter";
            return;
        }

        path ??= chooseProjectPathToOpen();
        if (path is null || IsDirty && !confirmDiscardChanges()) return;
        RememberCurrentSelections();
        var loadId = Interlocked.Increment(ref loadGeneration);

        var previousState = State;
        State = WorkspaceState.Loading;
        stateTitleOverride = $"Opening {Path.GetFileName(path)}";
        stateDetailOverride = "Loading Viewer project and rendering scene layers…";
        Changed(nameof(StateTitle));
        Changed(nameof(StateDetail));

        try
        {
            var loaded = await Task.Run(() => LoadProject(path));
            if (disposed || loadId != Volatile.Read(ref loadGeneration))
            {
                foreach (var item in loaded.Layers) item.Layer.Dispose();
                return;
            }
            ApplyProject(path, loaded.Project, loaded.Layers);
            RememberCurrentSelections();
            RecordRecentFile(path);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (disposed || loadId != Volatile.Read(ref loadGeneration)) return;
            SetDiagnostics([new Diagnostic("error", "PROJECT_OPEN_FAILED", exception.Message, path)]);
            State = previousState;
            stateDetailOverride = exception.Message;
            LastAction = "Project open failed";
            Changed(nameof(StateDetail));
        }
    }

    private (ViewerProjectDocument Project, IReadOnlyList<(SceneLayerViewModel Layer, InspectResult Inspection)> Layers) LoadProject(string path)
    {
        var project = projectStore.Load(path);
        var documents = project.SceneLayers ??
        [
            new SceneLayerDocument(
                project.SkeletonPath,
                project.AtlasPath,
                null,
                project.SelectedAnimation ?? "",
                project.SelectedSkin,
                project.ModelX,
                project.ModelY,
                project.ModelScale,
                project.ModelRotation,
                project.FlipX,
                project.FlipY,
                true,
                1,
                0)
        ];
        var layers = new List<(SceneLayerViewModel Layer, InspectResult Inspection)>(documents.Count);
        try
        {
            for (var documentIndex = 0; documentIndex < documents.Count; documentIndex++)
            {
                var document = documents[documentIndex];
                AssetRenderSession? session = null;
                try
                {
                    var opened = assetService!.OpenRenderSession(
                        document.SkeletonPath,
                        document.AtlasPath,
                        document.RuntimeOverride);
                    session = opened.Session;
                    var animation = document.Animation == "" || opened.Inspection.Animations.Any(item => item.Name == document.Animation)
                        ? document.Animation
                        : opened.Inspection.Animations.FirstOrDefault()?.Name
                            ?? "";
                    var skin = opened.Inspection.Skins.Contains(document.SelectedSkin, StringComparer.Ordinal)
                        ? document.SelectedSkin
                        : opened.Inspection.Skins.FirstOrDefault() ?? "default";
                    var duration = opened.Inspection.Animations.FirstOrDefault(item => item.Name == animation)?.DurationSeconds ?? 0;
                    var layerTrackAlpha = document.TrackAlpha ?? (documentIndex == 0 ? project.TrackAlpha : 1);
                    var layerPma = document.Pma ?? false;
                    var frame = session.RenderFrame(
                        animation,
                        duration / 2,
                        previewPixelWidth,
                        previewPixelHeight,
                        layerPma,
                        [skin],
                        trackAlpha: (float)layerTrackAlpha,
                        slots: document.Slots);

                    var layer = new SceneLayerViewModel(
                        opened.Inspection,
                        animation,
                        skin,
                        frame,
                        session,
                        document.ZIndex,
                        SceneLayerChanged,
                        SceneLayerChanging);
                    layer.ApplyDocument(
                        document with { Animation = animation, SelectedSkin = skin },
                        documentIndex == 0 ? project.TrackAlpha : null);
                    layers.Add((layer, opened.Inspection));
                }
                catch
                {
                    session?.Dispose();
                    throw;
                }
            }

            return (project, layers);
        }
        catch
        {
            foreach (var loaded in layers)
                loaded.Layer.Dispose();
            throw;
        }
    }

    private void ApplyProject(
        string path,
        ViewerProjectDocument project,
        IReadOnlyList<(SceneLayerViewModel Layer, InspectResult Inspection)> loaded)
    {
        var primary = loaded[0].Inspection;
        ClearSceneLayers();
        isPrototypePreview = false;
        animations = primary.Animations.Select(item => item.Name).ToArray();
        skinNames = primary.Skins.Count == 0 ? ["default"] : primary.Skins.ToArray();
        animationDurations = primary.Animations.ToDictionary(item => item.Name, item => (double)item.DurationSeconds, StringComparer.Ordinal);
        skeletonPath = primary.Asset.SkeletonPath;
        atlasPath = primary.Asset.AtlasPath;
        runtimeLine = primary.Runtime.SelectedLine;
        selectedAnimation = animations.Contains(project.SelectedAnimation, StringComparer.Ordinal)
            ? project.SelectedAnimation
            : animations.FirstOrDefault();
        selectedSkin = skinNames.Contains(project.SelectedSkin, StringComparer.Ordinal)
            ? project.SelectedSkin
            : skinNames[0];
        modelX = project.ModelX;
        modelY = project.ModelY;
        modelScale = project.ModelScale;
        modelRotation = project.ModelRotation;
        flipX = project.FlipX;
        flipY = project.FlipY;
        loop = project.Loop;
        playbackSpeed = project.PlaybackSpeed;
        trackAlpha = project.TrackAlpha;
        backgroundMode = project.BackgroundMode;
        backgroundColor = NormalizeHexColor(project.BackgroundColor) ?? DefaultBackgroundColor;
        position = 0;
        viewportZoom = 1;
        viewportPanX = viewportPanY = 0;
        fitPending = true;
        foreach (var loadedLayer in loaded)
            sceneLayers.Add(loadedLayer.Layer);
        SelectedSceneLayer = sceneLayers[0];
        projectPath = Path.GetFullPath(path);
        sceneDirty = false;
        undo.Clear();
        redo.Clear();
        savedSnapshot = Capture();
        SetDiagnostics(loaded.SelectMany(item => item.Inspection.Diagnostics).ToArray());
        State = diagnostics.Count == 0 ? WorkspaceState.Ready : WorkspaceState.ReadyWithWarnings;
        LastAction = $"Opened {Path.GetFileName(projectPath)}";
        Changed(nameof(SceneLayers));
        Changed(nameof(PreviewFrame));
        Changed(nameof(HasRenderedPreview));
        Changed(nameof(PreviewResolutionLabel));
        Changed(string.Empty);
        if (UseGpuPreview) QueuePreviewRender();
        else QueueSlotMetadata();
        RefreshCommands();
    }

    private void ApplyFakeAsset()
    {
        ClearSceneLayers();
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
        LastAction = "Workspace opened in 1 interaction";
        Changed(string.Empty);
        RefreshCommands();
    }

    private (InspectResult Result, AssetRenderSession? Session, RenderedFrame? Frame, string? RenderError, string Animation, string Skin) InspectAndRender(
        string path,
        string? atlasPathOverride,
        (string? Animation, string? Skin) remembered)
    {
        var result = assetService!.Inspect(path, atlasPathOverride, null);
        var selection = ResolveSelection(result, remembered);
        var duration = result.Animations.FirstOrDefault(item => item.Name == selection.Animation)?.DurationSeconds ?? 0;

        AssetRenderSession? session = null;
        try
        {
            session = assetService.OpenRenderSession(
                result.Asset.SkeletonPath,
                result.Asset.AtlasPath,
                result.Runtime.SelectedLine).Session;
            var frame = session.RenderFrame(
                selection.Animation,
                duration / 2,
                previewPixelWidth,
                previewPixelHeight,
                false,
                result.Skins.Count == 0 ? [] : [selection.Skin]);
            return (result, session, frame, null, selection.Animation, selection.Skin);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            session?.Dispose();
            return (result, null, null, exception.Message, selection.Animation, selection.Skin);
        }
    }

    private void ApplyAsset(InspectResult result, AssetRenderSession? session, RenderedFrame? frame, string? renderError, string animation, string skin)
    {
        ClearSceneLayers();
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
        selectedAnimation = animation.Length == 0 ? null : animation;
        selectedSkin = skin;
        if (renderError is null && session is not null && frame is not null)
        {
            var layer = new SceneLayerViewModel(
                result,
                selectedAnimation ?? "",
                selectedSkin,
                frame,
                session,
                0,
                SceneLayerChanged,
                SceneLayerChanging);
            sceneLayers.Add(layer);
            selectedSceneLayer = layer;
            Changed(nameof(SceneLayers));
            Changed(nameof(SelectedSceneLayer));
            Changed(nameof(PreviewFrame));
            Changed(nameof(HasRenderedPreview));
            Changed(nameof(PreviewResolutionLabel));
        }
        else
        {
            session?.Dispose();
        }
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
        if (UseGpuPreview) QueuePreviewRender();
        else QueueSlotMetadata();
        RefreshCommands();
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
        var maximumElapsed = Math.Max(0.25, 1.5 / previewFramesPerSecond);
        var elapsed = Math.Clamp((now - lastPlaybackTick).TotalSeconds, 0, maximumElapsed);
        lastPlaybackTick = now;
        var step = elapsed * Math.Max(0.01, PlaybackSpeed);
        var previous = Position;
        var next = previous + step;
        var wrapped = false;
        if (next >= duration)
        {
            if (Loop)
            {
                next %= duration;
                wrapped = true;
            }
            else
            {
                next = duration;
                IsPlaying = false;
            }
        }
        AdvanceMixes(step, wrapped);
        ReportCrossedEvent(previous, next, wrapped);
        Position = next;
    }

    // TASK-074: mixes advance with playback and end when finished or when the
    // timeline wraps (the mix contract needs the target time to cover the elapsed time).
    internal void AdvanceMixes(double step, bool wrapped)
    {
        if (layerMixes.Count == 0) return;
        if (wrapped)
        {
            layerMixes.Clear();
            return;
        }
        foreach (var (layer, mix) in layerMixes.ToArray())
        {
            mix.Elapsed += step;
            if (mix.Elapsed >= MixDuration || !sceneLayers.Contains(layer)) layerMixes.Remove(layer);
        }
    }

    private void StartMix(SceneLayerViewModel layer, string from, double fromTime)
    {
        if (MixDuration <= 0 || !IsPlaying || string.IsNullOrEmpty(from) || from == layer.Animation)
        {
            layerMixes.Remove(layer);
            return;
        }
        layerMixes[layer] = new LayerMix(from, (float)Math.Clamp(fromTime, 0, layer.DurationOf(from)));
    }

    internal AnimationMix? MixFor(SceneLayerViewModel layer) =>
        layerMixes.TryGetValue(layer, out var mix) && mix.Elapsed < MixDuration
            ? new AnimationMix(mix.From, mix.FromTime, (float)MixDuration, (float)mix.Elapsed)
            : null;

    // Shows the last event key of the selected layer passed during this tick.
    internal void ReportCrossedEvent(double previous, double next, bool wrapped)
    {
        if (selectedSceneLayer is not { } layer || layer.AnimationEvents.Count == 0) return;
        AnimationEventKey? crossed = null;
        foreach (var key in layer.AnimationEvents)
        {
            var time = key.TimeSeconds;
            if (wrapped ? time > previous || time <= next : time > previous && time <= next)
                crossed = key;
        }
        if (crossed is null) return;
        PlaybackEventLabel = DescribeEvent(crossed);
        eventLabelTimer.Stop();
        eventLabelTimer.Start();
    }

    // TASK-074: preview crossfade length when an animation changes during
    // playback, remembered in user settings; 0 turns mixing off.
    public double MixDuration
    {
        get => userSettings.MixDuration;
        set
        {
            var before = userSettings.MixDuration;
            userSettings.SetMixDuration(value);
            if (Math.Abs(before - userSettings.MixDuration) > 0.0001 && userSettings.MixDuration <= 0) layerMixes.Clear();
            Changed();
        }
    }

    public string PlaybackEventLabel
    {
        get => playbackEventLabel;
        private set
        {
            if (playbackEventLabel == value) return;
            playbackEventLabel = value;
            Changed();
            Changed(nameof(HasPlaybackEvent));
        }
    }

    public bool HasPlaybackEvent => playbackEventLabel.Length > 0;

    // Event keys of the selected layer's animation as timeline fractions.
    public IReadOnlyList<TimelineEventMarker> TimelineEventMarkers =>
        selectedSceneLayer is { } layer && layer.Duration > 0
            ? layer.AnimationEvents
                .Select(key => new TimelineEventMarker(
                    Math.Clamp(key.TimeSeconds / layer.Duration, 0, 1),
                    $"{key.TimeSeconds:0.###}s  {DescribeEvent(key)}"))
                .ToArray()
            : [];

    private static string DescribeEvent(AnimationEventKey key)
    {
        var details = new List<string>();
        if (key.Int != 0) details.Add($"int {key.Int}");
        if (key.Float != 0) details.Add($"float {key.Float:0.###}");
        if (!string.IsNullOrEmpty(key.String)) details.Add($"\"{key.String}\"");
        return details.Count == 0 ? key.Name : $"{key.Name} ({string.Join(", ", details)})";
    }

    private void UpdatePlaybackTimer()
    {
        playbackTimer.Interval = TimeSpan.FromSeconds(1d / previewFramesPerSecond);
        if (assetService is not null && IsPlaying && CanPlay && HasRenderedPreview)
            playbackTimer.Start();
        else
            playbackTimer.Stop();
    }

    private void QueuePreviewRender()
    {
        if (disposed || assetService is null || !HasRenderedPreview || !Definition.CanPlay) return;
        if (Interlocked.Exchange(ref previewRenderInProgress, 1) != 0)
        {
            if (IsPlaying) Interlocked.Increment(ref previewMetricCoalescedFrames);
            Interlocked.Exchange(ref previewRenderPending, 1);
            return;
        }
        _ = RenderPreviewAsync();
    }

    private void QueueSlotMetadata()
    {
        var layers = sceneLayers.Where(layer => layer.PreviewFrame is not null && layer.PreviewScene is null).ToArray();
        foreach (var layer in layers)
            _ = LoadSlotMetadataAsync(layer);
    }

    private async Task LoadSlotMetadataAsync(SceneLayerViewModel layer)
    {
        try
        {
            var scene = await Task.Run(
                () => layer.RenderSession.RenderScene(
                    layer.Animation,
                    Math.Min((float)Position, (float)layer.Duration),
                    layer.Pma,
                    string.IsNullOrWhiteSpace(layer.SelectedSkin) ? [] : [layer.SelectedSkin],
                    playbackCancellation.Token,
                    (float)layer.TrackAlpha,
                    layer.SlotDisplaySettings),
                playbackCancellation.Token);
            if (disposed || !sceneLayers.Contains(layer)) return;
            layer.PublishScene(scene);
            Changed(nameof(HasGpuPreview));
            Changed(nameof(IsCpuPreviewVisible));
            FitIfPending();
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            // Slot metadata is an enhancement; the authoritative CPU frame remains usable.
        }
    }

    private async Task RenderPreviewAsync()
    {
        var workStarted = Stopwatch.GetTimestamp();
        try
        {
            var time = (float)Position;
            var width = previewPixelWidth;
            var height = previewPixelHeight;
            var layers = sceneLayers
                .Where(layer => layer.PreviewFrame is not null && layer.IsVisible)
                .ToArray();
            if (layers.Length == 0) return;
            var mixes = layers.Select(MixFor).ToArray();
            if (UseGpuPreview)
            {
                var rendered = await Task.Run(
                    () => layers.Select((layer, index) =>
                    {
                        playbackCancellation.Token.ThrowIfCancellationRequested();
                        var scene = layer.RenderSession.RenderScene(
                            layer.Animation,
                            Math.Min(time, (float)layer.Duration),
                            layer.Pma,
                            string.IsNullOrWhiteSpace(layer.SelectedSkin) ? [] : [layer.SelectedSkin],
                            playbackCancellation.Token,
                            (float)layer.TrackAlpha,
                            layer.SlotDisplaySettings,
                            mix: mixes[index]);
                        return (Layer: layer, Scene: scene);
                    }).ToArray(),
                    playbackCancellation.Token);
                if (!playbackCancellation.IsCancellationRequested && layers.All(sceneLayers.Contains))
                {
                    foreach (var item in rendered)
                        item.Layer.PublishScene(item.Scene);
                    Changed(nameof(HasGpuPreview));
                    Changed(nameof(IsCpuPreviewVisible));
                    RecordPreviewPublished(workStarted);
                    FitIfPending();
                }
            }
            else
            {
                // Each raster is centered on the scene point at the view center;
                // the view applies only the layer's flips and rotation.
                var cameras = layers.Select(layer => PlaceInView(layer.ToDocument(), width, height).Camera).ToArray();
                var rendered = await Task.Run(
                    () => layers.Select((layer, index) =>
                    {
                        playbackCancellation.Token.ThrowIfCancellationRequested();
                        var frame = layer.RenderSession.RenderFrame(
                            layer.Animation,
                            Math.Min(time, (float)layer.Duration),
                            width,
                            height,
                            layer.Pma,
                            string.IsNullOrWhiteSpace(layer.SelectedSkin) ? [] : [layer.SelectedSkin],
                            playbackCancellation.Token,
                            (float)layer.TrackAlpha,
                            slots: layer.SlotDisplaySettings,
                            camera: cameras[index],
                            mix: mixes[index]);
                        return (Layer: layer, Frame: frame);
                    }).ToArray(),
                    playbackCancellation.Token);
                if (!playbackCancellation.IsCancellationRequested && layers.All(sceneLayers.Contains))
                {
                    foreach (var item in rendered)
                        item.Layer.PublishFrame(item.Frame);
                    Changed(nameof(PreviewFrame));
                    Changed(nameof(HasRenderedPreview));
                    Changed(nameof(PreviewResolutionLabel));
                    RecordPreviewPublished(workStarted);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            if (disposed) return;
            if (UseGpuPreview)
            {
                SetGpuPreviewAvailable(false, exception.Message);
                return;
            }
            SetDiagnostics([new Diagnostic("error", "RENDER_FAILED", exception.Message, skeletonPath)]);
            IsPlaying = false;
            State = WorkspaceState.RendererUnavailable;
            stateDetailOverride = exception.Message;
            Changed(nameof(StateDetail));
        }
        finally
        {
            Interlocked.Exchange(ref previewRenderInProgress, 0);
            if (Interlocked.Exchange(ref previewRenderPending, 0) != 0)
                QueuePreviewRender();
        }
    }

    private void RecordPreviewPublished(long workStarted)
    {
        if (!IsPlaying) return;
        previewMetricPublishedFrames++;
        previewMetricWorkMilliseconds += Stopwatch.GetElapsedTime(workStarted).TotalMilliseconds;
        var window = Stopwatch.GetElapsedTime(previewMetricWindowStarted);
        if (window < TimeSpan.FromSeconds(1)) return;

        var fps = previewMetricPublishedFrames / window.TotalSeconds;
        var averageMilliseconds = previewMetricWorkMilliseconds / Math.Max(1, previewMetricPublishedFrames);
        var coalesced = Interlocked.Exchange(ref previewMetricCoalescedFrames, 0);
        previewPerformanceLabel = $"{fps:0.0} FPS · {averageMilliseconds:0.0} ms work · {coalesced} coalesced";
        previewMetricWindowStarted = Stopwatch.GetTimestamp();
        previewMetricPublishedFrames = 0;
        previewMetricWorkMilliseconds = 0;
        Changed(nameof(PreviewPerformanceLabel));
    }

    private void ResetPreviewMetrics()
    {
        previewMetricWindowStarted = Stopwatch.GetTimestamp();
        previewMetricPublishedFrames = 0;
        previewMetricWorkMilliseconds = 0;
        Interlocked.Exchange(ref previewMetricCoalescedFrames, 0);
        previewPerformanceLabel = IsPlaying ? "Measuring preview…" : "Paused";
        Changed(nameof(PreviewPerformanceLabel));
    }

    private static string FormatTime(double seconds)
    {
        if (!double.IsFinite(seconds) || seconds < 0) seconds = 0;
        var time = TimeSpan.FromSeconds(seconds);
        return $"{(int)time.TotalMinutes}:{time.Seconds:00}.{time.Milliseconds / 100}";
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Interlocked.Increment(ref loadGeneration);
        playbackTimer.Stop();
        exportCancellation?.Cancel();
        playbackCancellation.Cancel();
        playbackCancellation.Dispose();
        deferredReloadTimer.Stop();
        sourceWatcher.Dispose();
        ClearSceneLayers();
    }

    private void ResetEditor()
    {
        modelX = modelY = modelRotation = 0;
        modelScale = playbackSpeed = trackAlpha = 1;
        viewportZoom = 1;
        viewportPanX = viewportPanY = 0;
        fitPending = true;
        position = 0;
        flipX = flipY = false;
        loop = true;
        backgroundMode = "Checkerboard";
        backgroundColor = DefaultBackgroundColor;
        projectPath = null;
        sceneDirty = false;
        SyncPrimaryLayerPlayback();
        SyncPrimaryLayerTransform();
        undo.Clear();
        redo.Clear();
        savedSnapshot = Capture();
    }

    private bool SaveTo(string path)
    {
        try
        {
            var layers = sceneLayers.Select(layer => layer.ToDocument()).ToArray();
            var primary = layers.FirstOrDefault();
            projectPath = projectStore.Save(path, new ViewerProjectDocument(
                ViewerProjectStore.CurrentSchemaVersion,
                primary?.SkeletonPath ?? skeletonPath,
                primary?.AtlasPath ?? atlasPath,
                primary?.Animation ?? SelectedAnimation,
                primary?.SelectedSkin ?? SelectedSkin,
                primary?.ModelX ?? ModelX,
                primary?.ModelY ?? ModelY,
                primary?.ModelScale ?? ModelScale,
                primary?.ModelRotation ?? ModelRotation,
                primary?.FlipX ?? FlipX,
                primary?.FlipY ?? FlipY,
                Loop,
                PlaybackSpeed,
                primary?.TrackAlpha ?? TrackAlpha,
                BackgroundMode,
                layers.Length == 0 ? null : layers,
                // Only a custom background stores its color.
                IsCustomBackground ? backgroundColor : null));
            savedSnapshot = Capture();
            sceneDirty = false;
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
        redo.Push(CaptureUndo());
        Restore(undo.Pop());
    }

    private void Redo()
    {
        undo.Push(CaptureUndo());
        Restore(redo.Pop());
    }

    private void Restore(UndoEntry entry)
    {
        var snapshot = entry.Editor;
        (selectedAnimation, selectedSkin, modelX, modelY, modelScale, modelRotation, flipX, flipY, loop, playbackSpeed, trackAlpha, backgroundMode, backgroundColor) =
            (snapshot.SelectedAnimation, snapshot.SelectedSkin, snapshot.ModelX, snapshot.ModelY, snapshot.ModelScale, snapshot.ModelRotation,
                snapshot.FlipX, snapshot.FlipY, snapshot.Loop, snapshot.PlaybackSpeed, snapshot.TrackAlpha, snapshot.BackgroundMode, snapshot.BackgroundColor);
        if (entry.Layers.Count == sceneLayers.Count
            && entry.Layers.Select(layer => layer.SkeletonPath).SequenceEqual(
                sceneLayers.Select(layer => layer.SkeletonPath),
                StringComparer.OrdinalIgnoreCase))
            for (var index = 0; index < sceneLayers.Count; index++)
                sceneLayers[index].ApplyDocument(entry.Layers[index]);
        sceneDirty = entry.SceneDirty;
        Changed(string.Empty);
        QueuePreviewRender();
        RefreshCommands();
    }

    private void Edit<T>(ref T field, T value, string propertyName, params string[] dependentProperties)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        undo.Push(CaptureUndo());
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
        BackgroundMode,
        BackgroundColor);

    private static bool IsIdentityPresentation(SceneLayerDocument layer) =>
        layer.IsVisible
        && Math.Abs(layer.Opacity - 1) < 0.000001
        && Math.Abs(layer.ModelX) < 0.000001
        && Math.Abs(layer.ModelY) < 0.000001
        && Math.Abs(layer.ModelScale - 1) < 0.000001
        && Math.Abs(layer.ModelRotation) < 0.000001
        && !layer.FlipX
        && !layer.FlipY;

    private UndoEntry CaptureUndo() => new(
        Capture(),
        sceneLayers.Select(layer => layer.ToDocument()).ToArray(),
        sceneDirty);

    private void RefreshCommands()
    {
        foreach (var command in new[]
                 {
                     OpenAssetCommand, OpenProjectCommand, ReloadCommand, ExportCommand, CancelExportCommand, ScreenshotCommand, CopyScreenshotCommand, TogglePlayCommand, StopCommand, FitCommand,
                     RestartCommand, PreviousFrameCommand, NextFrameCommand, BackTenFramesCommand, ForwardTenFramesCommand,
                     UseFfmpegFromPathCommand, CopySlotParametersCommand, ShowFilteredSlotsCommand, HideFilteredSlotsCommand, ClearFilteredSlotAttachmentsCommand,
                     AddLayerCommand, AutoLayoutCommand, RemoveLayerCommand, MoveLayerUpCommand, MoveLayerDownCommand,
                     DuplicateLayerCommand, ReloadLayerCommand, CopyAllLayerParametersCommand, CopyTransformParametersCommand,
                     CopyRenderParametersCommand, CopyAppearanceParametersCommand, PasteLayerParametersCommand,
                     SaveCommand, SaveAsCommand, UndoCommand, RedoCommand, ClearRecentFilesCommand
                 })
            ((RelayCommand)command).Refresh();
        ((ParameterCommand)OpenRecentCommand).Refresh();
    }

    private void Changed([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed record StateDefinition(string Title, string Detail, bool HasAsset, bool HasPreview, bool CanPlay);

    private sealed record LayerParameterClipboard(LayerParameterScope Scope, SceneLayerDocument Document);

    private sealed record ScreenshotSnapshot(
        float TimeSeconds,
        int Width,
        int Height,
        string Channel,
        CancellationToken CancellationToken,
        IReadOnlyList<ScreenshotLayerSnapshot> Layers);

    private sealed record ScreenshotLayerSnapshot(
        AssetRenderSession RenderSession,
        SceneLayerDocument Document,
        float Duration,
        SceneLayerPlacement Placement);

    private sealed class LayerMix(string from, float fromTime)
    {
        public string From { get; } = from;
        public float FromTime { get; } = fromTime;
        public double Elapsed { get; set; }
    }

    private sealed record UndoEntry(EditorSnapshot Editor, IReadOnlyList<SceneLayerDocument> Layers, bool SceneDirty);

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
        string BackgroundMode,
        string BackgroundColor);
}

// TASK-074: a Spine event key on the timeline, as a fraction of the duration.
public sealed record TimelineEventMarker(double Fraction, string Label);

internal sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => execute();
    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}

internal sealed class ParameterCommand(Action<object?> execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => execute(parameter);
    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
