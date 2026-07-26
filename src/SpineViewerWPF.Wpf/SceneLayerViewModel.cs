using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using SpineViewerWPF.Core;

namespace SpineViewerWPF.Wpf;

public sealed class SceneLayerViewModel : INotifyPropertyChanged
{
    private readonly Action? changed;
    private readonly IReadOnlyDictionary<string, double> animationDurations;
    private string animation;
    private string selectedSkin;
    private string? previewImagePath;
    private double modelX;
    private double modelY;
    private double modelScale = 1;
    private double modelRotation;
    private bool flipX;
    private bool flipY;
    private bool isVisible = true;
    private double opacity = 1;
    private int zIndex;

    internal SceneLayerViewModel(
        InspectResult inspection,
        string animation,
        string selectedSkin,
        string previewImagePath,
        int zIndex,
        Action? changed = null)
    {
        SkeletonPath = inspection.Asset.SkeletonPath;
        AtlasPath = inspection.Asset.AtlasPath;
        RuntimeOverride = inspection.Runtime.SelectedLine;
        Animations = inspection.Animations.Select(item => item.Name).ToArray();
        Skins = inspection.Skins.Count == 0 ? ["default"] : inspection.Skins.ToArray();
        animationDurations = inspection.Animations.ToDictionary(item => item.Name, item => (double)item.DurationSeconds, StringComparer.Ordinal);
        this.animation = animation;
        this.selectedSkin = selectedSkin;
        this.previewImagePath = previewImagePath;
        this.zIndex = zIndex;
        this.changed = changed;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string SkeletonPath { get; }
    public string AtlasPath { get; }
    public string RuntimeOverride { get; }
    public IReadOnlyList<string> Animations { get; }
    public IReadOnlyList<string> Skins { get; }
    public string Animation
    {
        get => animation;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || !animationDurations.ContainsKey(value) || animation == value) return;
            animation = value;
            Changed(nameof(Animation));
            Changed(nameof(Duration));
            changed?.Invoke();
        }
    }

    public string SelectedSkin
    {
        get => selectedSkin;
        set
        {
            if (string.IsNullOrWhiteSpace(value) || !Skins.Contains(value, StringComparer.Ordinal) || selectedSkin == value) return;
            selectedSkin = value;
            Changed(nameof(SelectedSkin));
            changed?.Invoke();
        }
    }

    public double Duration => animationDurations.TryGetValue(Animation, out var duration) ? duration : 0;
    public string DisplayName => Path.GetFileName(SkeletonPath);
    public string? PreviewImagePath => previewImagePath;

    public double ModelX
    {
        get => modelX;
        set => Set(ref modelX, value, nameof(ModelX));
    }

    public double ModelY
    {
        get => modelY;
        set => Set(ref modelY, value, nameof(ModelY));
    }

    public double ModelScale
    {
        get => modelScale;
        set => Set(ref modelScale, value, nameof(ModelScale));
    }

    public double ModelRotation
    {
        get => modelRotation;
        set => Set(ref modelRotation, value, nameof(ModelRotation));
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

    public int ZIndex => zIndex;

    internal void SetPlayback(string animation, string selectedSkin)
    {
        this.animation = animation;
        this.selectedSkin = selectedSkin;
        Changed(nameof(Animation));
        Changed(nameof(SelectedSkin));
        Changed(nameof(Duration));
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

    internal void ApplyDocument(SceneLayerDocument document)
    {
        modelX = document.ModelX;
        modelY = document.ModelY;
        modelScale = document.ModelScale;
        modelRotation = document.ModelRotation;
        flipX = document.FlipX;
        flipY = document.FlipY;
        isVisible = document.IsVisible;
        opacity = document.Opacity;
        zIndex = document.ZIndex;
        Changed(string.Empty);
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

    internal void RefreshPreview() => Changed(nameof(PreviewImagePath));

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
        ZIndex);

    private void Set<T>(ref T field, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return;
        field = value;
        Changed(propertyName);
        changed?.Invoke();
    }

    private void Changed([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
