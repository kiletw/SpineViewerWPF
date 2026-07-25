using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

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

    private WorkspaceState state;
    private bool isRailExpanded;
    private bool isPlaying;
    private string animationFilter = "";
    private string? selectedAnimation;
    private double position;
    private string lastAction = "Prototype ready";

    public ShellViewModel(WorkspaceState initialState, bool railExpanded)
    {
        state = initialState;
        isRailExpanded = railExpanded;
        selectedAnimation = Animations[0];
        isPlaying = initialState is WorkspaceState.Ready or WorkspaceState.ReadyWithWarnings;

        OpenAssetCommand = new RelayCommand(() =>
        {
            State = WorkspaceState.Ready;
            LastAction = "Preview opened in 1 interaction";
        });
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
        CycleStateCommand = new RelayCommand(() =>
            State = (WorkspaceState)(((int)State + 1) % Enum.GetValues<WorkspaceState>().Length));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<WorkspaceState> States { get; } = Enum.GetValues<WorkspaceState>();
    public IReadOnlyList<string> FilteredAnimations =>
        Animations.Where(x => x.Contains(AnimationFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
    public IReadOnlyList<string> Skins { get; } = ["default", "armor", "shadow"];

    public ICommand OpenAssetCommand { get; }
    public ICommand ReloadCommand { get; }
    public ICommand ExportCommand { get; }
    public ICommand ScreenshotCommand { get; }
    public ICommand TogglePlayCommand { get; }
    public ICommand StopCommand { get; }
    public ICommand FitCommand { get; }
    public ICommand DiagnosticsCommand { get; }
    public ICommand ToggleRailCommand { get; }
    public ICommand CycleStateCommand { get; }

    public WorkspaceState State
    {
        get => state;
        set
        {
            if (state == value) return;
            state = value;
            isPlaying = value is WorkspaceState.Ready or WorkspaceState.ReadyWithWarnings;
            Position = 0;
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
        set
        {
            if (selectedAnimation == value) return;
            selectedAnimation = value;
            Changed();
        }
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
    public double Duration => 1.8;

    private StateDefinition Definition => Definitions[State];

    private void RefreshCommands()
    {
        foreach (var command in new[] { ReloadCommand, ExportCommand, ScreenshotCommand, TogglePlayCommand, StopCommand, FitCommand })
            ((RelayCommand)command).Refresh();
    }

    private void Changed([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    private sealed record StateDefinition(string Title, string Detail, bool HasAsset, bool HasPreview, bool CanPlay);
}

internal sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => canExecute?.Invoke() ?? true;
    public void Execute(object? parameter) => execute();
    public void Refresh() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
