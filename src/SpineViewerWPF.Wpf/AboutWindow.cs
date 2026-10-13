using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SpineViewerWPF.Wpf;

// TASK-076: Help > About. Shares the owner's theme resources and styles, so it
// matches Dark/Light; every visible string comes from the Text.About* resources.
internal sealed class AboutWindow : Window
{
    private readonly AboutInfo info;

    public AboutWindow(Window owner, AboutInfo info, Action<Window> applyTitleBar)
    {
        this.info = info;
        Owner = owner;
        foreach (var key in owner.Resources.Keys) Resources[key] = owner.Resources[key];
        SetResourceReference(TitleProperty, "Text.AboutTitle");
        SetResourceReference(BackgroundProperty, "WindowBrush");
        SetResourceReference(ForegroundProperty, "TextBrush");
        Icon = owner.Icon;
        FontFamily = owner.FontFamily;
        FontSize = 13;
        Width = 560;
        SizeToContent = SizeToContent.Height;
        ResizeMode = ResizeMode.NoResize;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        SourceInitialized += (_, _) => applyTitleBar(this);
        Content = Build();
    }

    private UIElement Build()
    {
        var root = new StackPanel { Margin = new Thickness(22, 20, 22, 18) };

        var header = new DockPanel { Margin = new Thickness(0, 0, 0, 14) };
        var icon = new Image
        {
            Width = 56,
            Height = 56,
            Margin = new Thickness(0, 0, 14, 0),
            Source = LargestIconFrame()
        };
        RenderOptions.SetBitmapScalingMode(icon, BitmapScalingMode.HighQuality);
        header.Children.Add(icon);
        var titles = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
        titles.Children.Add(new TextBlock { Text = "SpineViewerWPF", FontSize = 20, FontWeight = FontWeights.SemiBold });
        titles.Children.Add(Muted(Format("Text.AboutVersion", info.Version), "Main.About.Version"));
        header.Children.Add(titles);
        root.Children.Add(header);

        root.Children.Add(Paragraph(Text("Text.AboutDescription")));

        root.Children.Add(Section("Text.AboutRuntimes"));
        root.Children.Add(Paragraph(string.Join("  ", info.RuntimeLines), "Main.About.Runtimes"));
        var license = Paragraph(Text("Text.AboutSpineLicense"));
        license.Inlines.Add(new LineBreak());
        license.Inlines.Add(Link(Text("Text.AboutSpineLicenseLink"), AboutInfo.SpineLicenseUrl));
        root.Children.Add(license);

        root.Children.Add(Section("Text.AboutEnvironment"));
        root.Children.Add(Paragraph(Format("Text.AboutRenderer", info.Renderer)));
        root.Children.Add(Paragraph(Format("Text.AboutFfmpeg", info.FfmpegStatus), "Main.About.Ffmpeg"));
        root.Children.Add(Paragraph(Text("Text.AboutThirdParty")));

        var buttons = new WrapPanel { Margin = new Thickness(0, 14, 0, 0), HorizontalAlignment = HorizontalAlignment.Right };
        buttons.Children.Add(Button("Text.AboutNotices", "Main.About.Notices", OpenNotices));
        buttons.Children.Add(Button("Text.AboutGitHub", "Main.About.GitHub", () => Open(AboutInfo.RepositoryUrl)));
        buttons.Children.Add(Button("Text.AboutReleases", "Main.About.Releases", () => Open(AboutInfo.ReleasesUrl)));
        buttons.Children.Add(Button("Text.AboutCopy", "Main.About.Copy", CopyInfo));
        var close = Button("Text.AboutClose", "Main.About.Close", Close);
        close.IsCancel = true;
        close.IsDefault = true;
        buttons.Children.Add(close);
        root.Children.Add(buttons);
        return root;
    }

    // The .ico's first frame is 16 px; use the largest one for the header.
    private static BitmapSource LargestIconFrame()
    {
        var decoder = BitmapDecoder.Create(
            new Uri("pack://application:,,,/Assets/AppIcon.ico"),
            BitmapCreateOptions.None,
            BitmapCacheOption.OnLoad);
        return decoder.Frames.MaxBy(frame => frame.PixelWidth)!;
    }

    // The release package ships THIRD-PARTY-NOTICES.md and licenses/ beside the
    // executable; development builds open the repository copy instead.
    private void OpenNotices()
    {
        var local = Path.Combine(AppContext.BaseDirectory, "THIRD-PARTY-NOTICES.md");
        Open(File.Exists(local) ? local : AboutInfo.NoticesUrl);
    }

    private void CopyInfo()
    {
        try
        {
            Clipboard.SetText(info.CopyText);
        }
        catch (System.Runtime.InteropServices.ExternalException)
        {
            // The clipboard is busy; the user can try again.
        }
    }

    private static void Open(string target)
    {
        try
        {
            Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (Exception exception) when (exception is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            // No handler for the link; nothing else to do in an About box.
        }
    }

    private string Text(string key) => TryFindResource(key) as string ?? key;

    private string Format(string key, string value) => string.Format(Text(key), value);

    private TextBlock Section(string key)
    {
        var block = new TextBlock { Text = Text(key), FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 12, 0, 4) };
        block.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");
        return block;
    }

    private static TextBlock Paragraph(string text, string? automationId = null)
    {
        var block = new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 4) };
        if (automationId is not null) System.Windows.Automation.AutomationProperties.SetAutomationId(block, automationId);
        return block;
    }

    private TextBlock Muted(string text, string automationId)
    {
        var block = Paragraph(text, automationId);
        block.SetResourceReference(TextBlock.ForegroundProperty, "MutedBrush");
        return block;
    }

    private Hyperlink Link(string text, string url)
    {
        var link = new Hyperlink(new Run(text)) { NavigateUri = new Uri(url) };
        link.SetResourceReference(TextElement.ForegroundProperty, "AccentBrush");
        link.RequestNavigate += (_, e) =>
        {
            Open(e.Uri.AbsoluteUri);
            e.Handled = true;
        };
        return link;
    }

    private Button Button(string key, string automationId, Action action)
    {
        var button = new Button { Content = Text(key), Margin = new Thickness(6, 0, 0, 6), MinWidth = 72 };
        if (TryFindResource("ButtonBase") is Style style) button.Style = style;
        System.Windows.Automation.AutomationProperties.SetAutomationId(button, automationId);
        button.Click += (_, _) => action();
        return button;
    }
}
