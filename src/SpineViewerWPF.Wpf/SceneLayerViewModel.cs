using System.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.CompilerServices;
using SpineViewerWPF.Application;
using SpineViewerWPF.Core;

namespace SpineViewerWPF.Wpf;

internal enum LayerParameterScope
{
    All,
    Transform,
    Render,
    Appearance,
    Slots
}

internal enum SlotBatchAction
{
    Show,
    Hide,
    ClearAttachments
}

public sealed class SceneLayerViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly Action? changed;
    private readonly Action? changing;
    private readonly IReadOnlyDictionary<string, double> animationDurations;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<AnimationEventKey>> animationEvents;
    private string animation;
    private string selectedSkin;
    private RenderedFrame? previewFrame;
    private PreviewSceneFrame? previewScene;
    private double modelX;
    private double modelY;
    private double modelScale = 1;
    private double modelRotation;
    private bool flipX;
    private bool flipY;
    private bool isVisible = true;
    private double opacity = 1;
    private double trackAlpha = 1;
    private bool pma;
    private int zIndex;
    private string slotFilter = "";
    private IReadOnlyList<SlotDisplayViewModel>? filteredSlots;
    private readonly Dictionary<string, SlotDisplayViewModel> slotSettings = new(StringComparer.Ordinal);
    private readonly Dictionary<string, SlotDisplayDocument> pendingSlotSettings = new(StringComparer.Ordinal);

    internal SceneLayerViewModel(
        InspectResult inspection,
        string animation,
        string selectedSkin,
        RenderedFrame previewFrame,
        AssetRenderSession renderSession,
        int zIndex,
        Action? changed = null,
        Action? changing = null)
    {
        SkeletonPath = inspection.Asset.SkeletonPath;
        AtlasPath = inspection.Asset.AtlasPath;
        RuntimeOverride = inspection.Runtime.SelectedLine;
        Animations = inspection.Animations.Select(item => item.Name).ToArray();
        Skins = inspection.Skins.Count == 0 ? ["default"] : inspection.Skins.ToArray();
        animationDurations = inspection.Animations.ToDictionary(item => item.Name, item => (double)item.DurationSeconds, StringComparer.Ordinal);
        animationEvents = inspection.Animations.ToDictionary(
            item => item.Name,
            item => item.Events ?? (IReadOnlyList<AnimationEventKey>)[],
            StringComparer.Ordinal);
        this.animation = animation;
        this.selectedSkin = selectedSkin;
        this.previewFrame = previewFrame;
        RenderSession = renderSession;
        this.zIndex = zIndex;
        this.changed = changed;
        this.changing = changing;
        foreach (var slot in inspection.Slots ?? []) AddSlot(slot);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string SkeletonPath { get; }
    public string AtlasPath { get; }
    public string RuntimeOverride { get; }
    internal AssetRenderSession RenderSession { get; }
    public IReadOnlyList<string> Animations { get; }
    public IReadOnlyList<string> Skins { get; }
    public string Animation
    {
        get => animation;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || !animationDurations.ContainsKey(value) || animation == value) return;
            changing?.Invoke();
            SwitchedFromAnimation = animation;
            animation = value;
            Changed(nameof(Animation));
            Changed(nameof(Duration));
            Changed(nameof(AnimationEvents));
            changed?.Invoke();
        }
    }

    public string SelectedSkin
    {
        get => selectedSkin;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || !Skins.Contains(value, StringComparer.Ordinal) || selectedSkin == value) return;
            changing?.Invoke();
            selectedSkin = value;
            Changed(nameof(SelectedSkin));
            changed?.Invoke();
        }
    }

    public double Duration => animationDurations.TryGetValue(Animation, out var duration) ? duration : 0;

    // TASK-074: Spine event keys of the current animation, in time order.
    public IReadOnlyList<AnimationEventKey> AnimationEvents =>
        animationEvents.TryGetValue(Animation, out var events) ? events : [];

    // TASK-074: the animation shown before the last Animation change, until the
    // Shell consumes it to start a mix.
    internal string? SwitchedFromAnimation { get; set; }

    internal double DurationOf(string name) => animationDurations.TryGetValue(name, out var duration) ? duration : 0;
    public string DisplayName => Path.GetFileName(SkeletonPath);
    public RenderedFrame? PreviewFrame => previewFrame;
    public PreviewSceneFrame? PreviewScene => previewScene;
    public ObservableCollection<SlotDisplayViewModel> Slots { get; } = [];
    // TASK-061: cache the filtered list so repeated notifications keep the same
    // instance; a new array per frame made the Slots ListBox rebuild every row.
    public IReadOnlyList<SlotDisplayViewModel> FilteredSlots =>
        filteredSlots ??= Slots.Where(slot => slot.Name.Contains(SlotFilter, StringComparison.OrdinalIgnoreCase)).ToArray();
    public string SlotFilter
    {
        get => slotFilter;
        set
        {
            var next = value ?? "";
            if (slotFilter == next) return;
            slotFilter = next;
            filteredSlots = null;
            Changed();
            Changed(nameof(FilteredSlots));
        }
    }
    internal IReadOnlyList<SlotDisplayDocument> SlotDisplaySettings =>
        Slots.Select(slot => new SlotDisplayDocument(slot.Name, slot.IsVisible, slot.Opacity, slot.AttachmentName)).ToArray();

    public double ModelX
    {
        get => modelX;
        set
        {
            if (double.IsFinite(value)) Set(ref modelX, value, nameof(ModelX));
        }
    }

    public double ModelY
    {
        get => modelY;
        set
        {
            if (double.IsFinite(value)) Set(ref modelY, value, nameof(ModelY));
        }
    }

    public double ModelScale
    {
        get => modelScale;
        set
        {
            if (double.IsFinite(value) && value > 0) Set(ref modelScale, value, nameof(ModelScale));
        }
    }

    public double ModelRotation
    {
        get => modelRotation;
        set
        {
            if (double.IsFinite(value)) Set(ref modelRotation, value, nameof(ModelRotation));
        }
    }

    public bool FlipX
    {
        get => flipX;
        set => Set(ref flipX, value, nameof(FlipX));
    }

    public bool FlipY
    {
        get => flipY;
        set => Set(ref flipY, value, nameof(FlipY));
    }

    public bool IsVisible
    {
        get => isVisible;
        set => Set(ref isVisible, value, nameof(IsVisible));
    }

    public double Opacity
    {
        get => opacity;
        set => Set(ref opacity, Math.Clamp(value, 0, 1), nameof(Opacity));
    }

    public double TrackAlpha
    {
        get => trackAlpha;
        set
        {
            if (double.IsFinite(value))
                Set(ref trackAlpha, Math.Clamp(value, 0, 1), nameof(TrackAlpha));
        }
    }

    public bool Pma
    {
        get => pma;
        set => Set(ref pma, value, nameof(Pma));
    }

    public int ZIndex => zIndex;

    internal void SetPlayback(string animation, string selectedSkin)
    {
        this.animation = animation;
        this.selectedSkin = selectedSkin;
        Changed(nameof(Animation));
        Changed(nameof(SelectedSkin));
        Changed(nameof(Duration));
        Changed(nameof(AnimationEvents));
    }

    internal void SetTransform(double x, double y, double scale, double rotation, bool flipX, bool flipY)
    {
        modelX = x;
        modelY = y;
        modelScale = scale;
        modelRotation = rotation;
        this.flipX = flipX;
        this.flipY = flipY;
        Changed(nameof(ModelX));
        Changed(nameof(ModelY));
        Changed(nameof(ModelScale));
        Changed(nameof(ModelRotation));
        Changed(nameof(FlipX));
        Changed(nameof(FlipY));
    }

    internal void ApplyDocument(SceneLayerDocument document, double? fallbackTrackAlpha = null)
    {
        modelX = document.ModelX;
        modelY = document.ModelY;
        modelScale = document.ModelScale;
        modelRotation = document.ModelRotation;
        flipX = document.FlipX;
        flipY = document.FlipY;
        isVisible = document.IsVisible;
        opacity = document.Opacity;
        trackAlpha = Math.Clamp(document.TrackAlpha ?? fallbackTrackAlpha ?? 1, 0, 1);
        pma = document.Pma ?? false;
        zIndex = document.ZIndex;
        pendingSlotSettings.Clear();
        foreach (var slot in document.Slots ?? [])
        {
            if (string.IsNullOrWhiteSpace(slot.Name)) continue;
            pendingSlotSettings[slot.Name] = slot;
            if (slotSettings.TryGetValue(slot.Name, out var setting))
                setting.Apply(slot.IsVisible, slot.Opacity, slot.AttachmentName);
        }
        Changed(string.Empty);
    }

    internal void ApplyParameters(SceneLayerDocument source, LayerParameterScope scope)
    {
        changing?.Invoke();
        if (scope is LayerParameterScope.All or LayerParameterScope.Transform)
        {
            modelX = source.ModelX;
            modelY = source.ModelY;
            modelScale = source.ModelScale;
            modelRotation = source.ModelRotation;
            flipX = source.FlipX;
            flipY = source.FlipY;
        }

        if (scope is LayerParameterScope.All or LayerParameterScope.Render)
        {
            isVisible = source.IsVisible;
            opacity = Math.Clamp(source.Opacity, 0, 1);
            trackAlpha = Math.Clamp(source.TrackAlpha ?? 1, 0, 1);
            pma = source.Pma ?? false;
        }

        if (scope is LayerParameterScope.All or LayerParameterScope.Appearance)
        {
            if (Animations.Contains(source.Animation, StringComparer.Ordinal)) animation = source.Animation;
            if (Skins.Contains(source.SelectedSkin, StringComparer.Ordinal)) selectedSkin = source.SelectedSkin;
        }

        if (scope is LayerParameterScope.All or LayerParameterScope.Appearance or LayerParameterScope.Slots)
        {
            foreach (var saved in source.Slots ?? [])
            {
                if (string.IsNullOrWhiteSpace(saved.Name)) continue;
                pendingSlotSettings[saved.Name] = saved;
                if (slotSettings.TryGetValue(saved.Name, out var slot))
                    slot.Apply(saved.IsVisible, saved.Opacity, saved.AttachmentName);
            }
        }

        Changed(string.Empty);
        changed?.Invoke();
    }

    // TASK-065: apply one batch action to the slots shown by the current filter
    // as a single Undo step; returns how many slots changed.
    internal int ApplySlotBatch(SlotBatchAction action)
    {
        var targets = FilteredSlots.Where(slot => action switch
        {
            SlotBatchAction.Show => !slot.IsVisible,
            SlotBatchAction.Hide => slot.IsVisible,
            _ => slot.AttachmentName is not null
        }).ToArray();
        if (targets.Length == 0) return 0;

        changing?.Invoke();
        foreach (var slot in targets)
        {
            switch (action)
            {
                case SlotBatchAction.Show:
                    slot.Apply(true, slot.Opacity, slot.AttachmentName);
                    break;
                case SlotBatchAction.Hide:
                    slot.Apply(false, slot.Opacity, slot.AttachmentName);
                    break;
                default:
                    slot.Apply(slot.IsVisible, slot.Opacity, null);
                    break;
            }
        }
        changed?.Invoke();
        return targets.Length;
    }

    internal void SetPosition(double x, double y)
    {
        modelX = x;
        modelY = y;
        Changed(nameof(ModelX));
        Changed(nameof(ModelY));
    }

    internal void SetZIndex(int value)
    {
        if (zIndex == value) return;
        zIndex = value;
        Changed(nameof(ZIndex));
    }

    internal void PublishFrame(RenderedFrame frame)
    {
        previewFrame = frame;
        Changed(nameof(PreviewFrame));
    }

    internal void PublishScene(PreviewSceneFrame scene)
    {
        previewScene = scene;
        var names = scene.DrawCommands
            .Select(command => command.SlotName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var slotsAdded = false;
        foreach (var name in names)
        {
            if (slotSettings.ContainsKey(name)) continue;
            AddSlot(new SlotDescriptor(name, null, []));
            slotsAdded = true;
        }
        Changed(nameof(PreviewScene));
        // Every playback frame publishes a scene; only announce the slot list
        // when it actually gained slots.
        if (!slotsAdded) return;
        Changed(nameof(Slots));
        Changed(nameof(FilteredSlots));
    }

    internal SceneLayerDocument ToDocument() => new(
        SkeletonPath,
        AtlasPath,
        RuntimeOverride,
        Animation,
        SelectedSkin,
        ModelX,
        ModelY,
        ModelScale,
        ModelRotation,
        FlipX,
        FlipY,
        IsVisible,
        Opacity,
        ZIndex,
        TrackAlpha,
        Pma,
        Slots.Select(slot => new SlotDisplayDocument(slot.Name, slot.IsVisible, slot.Opacity, slot.AttachmentName)).ToArray());

    private void AddSlot(SlotDescriptor descriptor)
    {
        if (string.IsNullOrWhiteSpace(descriptor.Name) || slotSettings.ContainsKey(descriptor.Name)) return;
        var saved = pendingSlotSettings.TryGetValue(descriptor.Name, out var savedSetting) ? savedSetting : null;
        var setting = new SlotDisplayViewModel(
            descriptor.Name,
            descriptor.Attachments,
            descriptor.SetupAttachment,
            () => changing?.Invoke(),
            () => changed?.Invoke(),
            saved?.IsVisible ?? true,
            saved?.Opacity ?? 1,
            saved?.AttachmentName);
        slotSettings.Add(descriptor.Name, setting);
        Slots.Add(setting);
        filteredSlots = null;
    }

    public void Dispose() => RenderSession.Dispose();

    private void Set<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        changing?.Invoke();
        field = value;
        Changed(propertyName);
        changed?.Invoke();
    }

    private void Changed([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
