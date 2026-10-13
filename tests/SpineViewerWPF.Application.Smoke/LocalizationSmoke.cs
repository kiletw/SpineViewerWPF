using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SpineViewerWPF.Wpf;

// TASK-077: every UI text key has a Traditional Chinese entry with the same
// placeholders, and the language setting resolves as documented.
static class LocalizationSmoke
{
    private static readonly Regex Placeholder = new(@"\{\d+(?::[^}]*)?\}");
    private static readonly Regex CodeKey = new(@"Localized\.(?:Text|Format)\(\s*""([^""{}]+)"",\s*""((?:[^""\\]|\\.)*)""");

    public static void Run()
    {
        var source = Path.Combine(Directory.GetCurrentDirectory(), "src", "SpineViewerWPF.Wpf");
        var english = Load(Path.Combine(source, "Localization", "Strings.en.xaml"));
        var chinese = Load(Path.Combine(source, "Localization", "Strings.zh-TW.xaml"));

        var code = Directory.EnumerateFiles(source, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}"))
            .SelectMany(path => CodeKey.Matches(File.ReadAllText(path)))
            .GroupBy(match => match.Groups[1].Value)
            .ToDictionary(group => group.Key, group => Regex.Unescape(group.First().Groups[2].Value));
        Check(code.Count > 50, $"Only {code.Count} code-side text keys were found.");

        foreach (var (key, text) in english.Concat(code))
        {
            Check(chinese.TryGetValue(key, out var translated), $"Traditional Chinese text is missing for {key}.");
            Check(Placeholders(text).SequenceEqual(Placeholders(translated!)), $"Placeholders differ for {key}.");
        }
        foreach (var key in chinese.Keys)
            Check(english.ContainsKey(key) || code.ContainsKey(key) || key.StartsWith("State.") || key.StartsWith("Option.")
                  || key is "Status.Warning" or "Status.Warnings" or "Status.Error" or "Status.Errors" or "Status.Issue"
                      or "Status.Issues" or "Status.Slot" or "Status.Slots" or "Status.AddedLayer" or "Status.AddedLayers",
                $"Traditional Chinese has an unused key {key}.");
        foreach (var state in Enum.GetNames<WorkspaceState>())
            Check(chinese.ContainsKey($"State.{state}.Title") && chinese.ContainsKey($"State.{state}.Detail"), $"State text is missing for {state}.");

        Check(UiLanguage.Resolve(null, new CultureInfo("zh-TW")) == "zh-TW"
              && UiLanguage.Resolve("system", new CultureInfo("zh-CN")) == "zh-TW"
              && UiLanguage.Resolve("system", new CultureInfo("ja-JP")) == "en"
              && UiLanguage.Resolve("en", new CultureInfo("zh-TW")) == "en"
              && UiLanguage.Resolve("zh-TW", new CultureInfo("en-US")) == "zh-TW"
              && UiLanguage.Normalize("fr") == "system",
            "The UI language did not resolve as documented.");
        var settingsPath = Path.Combine(Path.GetTempPath(), $"SpineViewerWPF-language-{Guid.NewGuid():N}.json");
        try
        {
            var settings = UserSettingsStore.Load(settingsPath);
            Check(settings.Language == "system", "The language setting did not default to system.");
            settings.SetLanguage("zh-TW");
            Check(UserSettingsStore.Load(settingsPath).Language == "zh-TW", "The language setting was not remembered.");
        }
        finally
        {
            File.Delete(settingsPath);
        }
        Console.WriteLine($"TASK-077: {english.Count + code.Count} UI text keys localized.");
    }

    private static Dictionary<string, string> Load(string path)
    {
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        return XDocument.Load(path).Root!.Elements()
            .ToDictionary(element => (string)element.Attribute(x + "Key")!, element => element.Value);
    }

    private static string[] Placeholders(string text) =>
        Placeholder.Matches(text).Select(match => match.Value).Order().ToArray();

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
