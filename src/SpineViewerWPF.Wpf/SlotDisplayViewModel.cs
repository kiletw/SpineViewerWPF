using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SpineViewerWPF.Wpf;

public sealed class SlotDisplayViewModel : INotifyPropertyChanged
{
    private readonly Action changing;
    private readonly Action changed;
    private bool isVisible = true;
    private double opacity = 1;
    private string selectedAttachmentKey = "";

    internal SlotDisplayViewModel(
        string name,
        IReadOnlyList<string> attachments,
        string? setupAttachment,
        Action changing,
        Action changed,
        bool isVisible = true,
        double opacity = 1,
        string? attachmentName = null)
    {
        Name = name;
        SetupAttachment = setupAttachment;
        Attachments = attachments
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        AttachmentOptions = ["", .. Attachments];
        this.changing = changing;
        this.changed = changed;
        this.isVisible = isVisible;
        this.opacity = Math.Clamp(opacity, 0, 1);
        selectedAttachmentKey = Attachments.Contains(attachmentName, StringComparer.Ordinal) ? attachmentName! : "";
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Name { get; }
    public string? SetupAttachment { get; }
    public IReadOnlyList<string> Attachments { get; }
    public IReadOnlyList<string> AttachmentOptions { get; }
    public string SelectedAttachmentKey
    {
        get => selectedAttachmentKey;
        set
        {
            var next = Attachments.Contains(value, StringComparer.Ordinal) ? value : "";
            if (selectedAttachmentKey == next) return;
            changing();
            selectedAttachmentKey = next;
            Changed();
            changed();
        }
    }
    internal string? AttachmentName => string.IsNullOrEmpty(selectedAttachmentKey) ? null : selectedAttachmentKey;

    public bool IsVisible
    {
        get => isVisible;
        set
        {
            if (isVisible == value) return;
            changing();
            isVisible = value;
            Changed();
            changed();
        }
    }

    public double Opacity
    {
        get => opacity;
        set
        {
            var next = Math.Clamp(double.IsFinite(value) ? value : 1, 0, 1);
            if (Math.Abs(opacity - next) < 0.001) return;
            changing();
            opacity = next;
            Changed();
            changed();
        }
    }

    internal void Apply(bool visible, double value, string? attachmentName)
    {
        isVisible = visible;
        opacity = Math.Clamp(double.IsFinite(value) ? value : 1, 0, 1);
        selectedAttachmentKey = Attachments.Contains(attachmentName, StringComparer.Ordinal) ? attachmentName! : "";
        Changed(nameof(IsVisible));
        Changed(nameof(Opacity));
        Changed(nameof(SelectedAttachmentKey));
    }

    private void Changed([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
