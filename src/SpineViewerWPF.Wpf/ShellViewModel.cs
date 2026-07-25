using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
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

public sealed class ShellViewModel : INotifyPropertyChanged
{
    private static readonly string[] Animations = ["idle", "walk", "attack", "victory"];
    private static readonly string[] SkinNames = ["default", "armor", "shadow"];
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

    private readonly ViewerProjectStore projectStore;
    private readonly Func<string?> chooseProjectPath;
    // ponytail: in-memory history is enough for one fake document; cap or persist it with multi-document editing.
    private readonly Stack<EditorSnapshot> undo = [];
    private readonly Stack<EditorSnapshot> redo = [];
    private WorkspaceState state;
    private EditorSnapshot savedSnapshot;
    private bool isRailExpanded;
    private bool isInspectorVisible;
    private bool isPlaying;
    private string animationFilter = "";
    private string? selectedAnimation = Animations[0];
    private string selectedSkin = SkinNames[0];
    private double position;
    private double modelX;
    private double modelY;
    private double modelScale = 1;
    private double modelRotation;
    private bool flipX;
    private bool flipY;
    private bool loop = true;
    private double playbackSpeed = 1;
    private double trackAlpha = 1;
    private string backgroundMode = "Checkerboard";
    private string? projectPath;
    private string lastAction = "Prototype ready";

    public ShellViewModel(
        WorkspaceState initialState,
        bool expandedWorkspace,
        ViewerProjectStore? projectStore = null,
        Func<string?>? chooseProjectPath = null)
    {
        state = initialState;
        isRailExpanded = expandedWorkspace;
        isInspectorVisible = expandedWorkspace;
        isPlaying = initialState is WorkspaceState.Ready or WorkspaceState.ReadyWithWarnings;
        this.projectStore = projectStore ?? new ViewerProjectStore();
        this.chooseProjectPath = chooseProjectPath ?? (() => null);
        savedSnapshot = Capture();

        OpenAssetCommand = new RelayCommand(OpenFakeAsset);
        ReloadCommand = new RelayCommand(() => LastAction = "Fake asset reloaded", () => HasAsset);
        ExportCommand = new RelayCommand(() => State = WorkspaceState.Exporting, () => CanPlay);
        ScreenshotCommand = new RelayCommand(() => LastAction = "Screenshot command invoked", () => HasPreview);
        TogglePlayCommand = new RelayCommand(() => IsPlaying = !IsPlaying, () => CanPlay);
        StopCommand = new RelayCommand(() =>
        {
            IsPlaying = false;
            Position = 0;
        }, () => CanPlay);
        FitCommand = new RelayCommand(() => LastAction = "Viewport fitted", () => HasPreview);
        DiagnosticsCommand = new RelayCommand(() => LastAction = IsWarning ? "1 warning shown" : "No blocking diagnostics");
        ToggleRailCommand = new RelayCommand(() => IsRailExpanded = !IsRailExpanded);
        ToggleInspectorCommand = new RelayCommand(() => IsInspectorVisible = !IsInspectorVisible);
        SaveCommand = new RelayCommand(() => TrySave(), () => HasAsset && (IsDirty || ProjectPath is null));
        SaveAsCommand = new RelayCommand(() => TrySaveAs(), () => HasAsset);
        UndoCommand = new RelayCommand(Undo, () => undo.Count > 0);
        RedoCommand = new RelayCommand(Redo, () => redo.Count > 0);
        CycleStateCommand = new RelayCommand(() =>
            State = (WorkspaceState)(((int)State + 1) % Enum.GetValues<WorkspaceState>().Length));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<string> FilteredAnimations =>
        Animations.Where(x => x.Contains(AnimationFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
    public IReadOnlyList<string> Skins { get; } = SkinNames;
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
            isPlaying = value is WorkspaceState.Ready or WorkspaceState.ReadyWithWarnings;
            position = 0;
            Changed(string.Empty);
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
        set => Edit(ref selectedAnimation, value, nameof(SelectedAnimation));
    }

    public string SelectedSkin
    {
        get => selectedSkin;
        set => Edit(ref selectedSkin, value ?? SkinNames[0], nameof(SelectedSkin));
    }

    public double Position
    {
        get => position;
        set
        {
            if (Math.Abs(position - value) < 0.001) return;
            position = value;
            Changed();
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
    public bool CanPlay => Definition.CanPlay;
    public bool IsDirty => Capture() != savedSnapshot;
    public bool HasBlockingOverlay => State is WorkspaceState.Unsupported or WorkspaceState.Failed or WorkspaceState.RendererUnavailable;
    public string StateTitle => Definition.Title;
    public string StateDetail => Definition.Detail;
    public string RuntimeLabel => HasAsset ? "Runtime 4.1" : "Runtime —";
    public string DiagnosticLabel => IsWarning
        ? "1 warning"
        : State is WorkspaceState.Unsupported or WorkspaceState.Failed or WorkspaceState.RendererUnavailable
            ? "1 error"
            : "No issues";
    public string PlaybackLabel => IsPlaying ? "Pause" : "Play";
    public string DocumentTitle => HasAsset ? $"hero.json{(IsDirty ? " *" : "")}" : "No document";
    public string WindowTitle => $"Spine Viewer · {DocumentTitle}";
    public string ProjectPathLabel => ProjectPath ?? "Not saved";
    public string? ProjectPath => projectPath;
    public double PreviewX => ModelX;
    public double PreviewY => ModelY;
    public double PreviewScaleX => ModelScale * (FlipX ? -1 : 1);
    public double PreviewScaleY => ModelScale * (FlipY ? -1 : 1);
    public double Duration => 1.8;

    public bool TrySave()
    {
        var path = ProjectPath ?? chooseProjectPath();
        return path is not null && SaveTo(path);
    }

    public bool TrySaveAs()
    {
        var path = chooseProjectPath();
        return path is not null && SaveTo(path);
    }

    private StateDefinition Definition => Definitions[State];

    private void OpenFakeAsset()
    {
        State = WorkspaceState.Ready;
        selectedAnimation = Animations[0];
        selectedSkin = SkinNames[0];
        modelX = modelY = modelRotation = 0;
        modelScale = playbackSpeed = trackAlpha = 1;
        flipX = flipY = false;
        loop = true;
        backgroundMode = "Checkerboard";
        projectPath = null;
        undo.Clear();
        redo.Clear();
        savedSnapshot = Capture();
        LastAction = "Preview opened in 1 interaction";
        Changed(string.Empty);
        RefreshCommands();
    }

    private bool SaveTo(string path)
    {
        try
        {
            projectPath = projectStore.Save(path, new ViewerProjectDocument(
                ViewerProjectStore.CurrentSchemaVersion,
                "hero.json",
                "hero.atlas",
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
                     ReloadCommand, ExportCommand, ScreenshotCommand, TogglePlayCommand, StopCommand, FitCommand,
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
