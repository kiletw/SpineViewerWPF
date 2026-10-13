using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SpineViewerWPF.Wpf;

// TASK-077: UI text produced in code. The English text is written at the call
// site; a language dictionary merged by App.OnStartup overrides it by key. Without
// a running application (tests) or off the UI thread, the English text is used.
internal static class Localized
{
    public static string Text(string key, string english)
    {
        try
        {
            return System.Windows.Application.Current?.TryFindResource(key) as string ?? english;
        }
        catch (InvalidOperationException)
        {
            return english;
        }
    }

    public static string Format(string key, string english, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Text(key, english), args);
}

// TASK-077: UI languages. The setting stores "system", "en", or "zh-TW"; the
// system choice follows the Windows display language, with Chinese mapped to
// Traditional Chinese and everything else to English.
public static class UiLanguage
{
    public const string System = "system";
    public const string English = "en";
    public const string TraditionalChinese = "zh-TW";

    public static IReadOnlyList<string> Choices { get; } = [System, English, TraditionalChinese];

    public static string Normalize(string? setting) =>
        setting is English or TraditionalChinese ? setting : System;

    public static string Resolve(string? setting, CultureInfo culture) =>
        Normalize(setting) switch
        {
            English => English,
            TraditionalChinese => TraditionalChinese,
            _ => culture.Name.StartsWith("zh", StringComparison.OrdinalIgnoreCase) ? TraditionalChinese : English
        };

    // The dictionary merged over English, or null for English itself.
    public static Uri? Dictionary(string language) =>
        language == TraditionalChinese
            ? new Uri("pack://application:,,,/SpineViewerWPF;component/Localization/Strings.zh-TW.xaml")
            : null;
}

// TASK-077: shows option values (stored in projects and settings) in the UI
// language; the value itself never changes. Key: "Option." + value without spaces.
public sealed class OptionTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is string text ? Localized.Text("Option." + text.Replace(" ", ""), text) : value ?? "";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
