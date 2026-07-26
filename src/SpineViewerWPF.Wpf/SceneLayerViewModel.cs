using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using SpineViewerWPF.Core;

namespace SpineViewerWPF.Wpf;

public sealed class SceneLayerViewModel : INotifyPropertyChanged
{
    private readonly Action? changed;
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
        string skeletonPath,
        string atlasPath,
        string runtimeOverride,
        string animation,
        string selectedSkin,
        string previewImagePath,
        int zIndex,
        Action? changed = null)
    {
        SkeletonPath = skeletonPath;
        AtlasPath = atlasPath;
        RuntimeOverride = runtimeOverride;
        Animation = animation;
        SelectedSkin = selectedSkin;
        this.previewImagePath = previewImagePath;
        this.zIndex = zIndex;
        this.changed = changed;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string SkeletonPath { get; }
    public string AtlasPath { get; }
    public string RuntimeOverride { get; }
    public string Animation { get; private set; }
    public string SelectedSkin { get; private set; }
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
        Animation = animation;
        SelectedSkin = selectedSkin;
        Changed(nameof(Animation));
        Changed(nameof(SelectedSkin));
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
