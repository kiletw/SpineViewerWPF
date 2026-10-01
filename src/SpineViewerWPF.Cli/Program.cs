using System.Globalization;
using System.Text.Json;
using SpineRuntime.V40;
using SpineRuntime.V41;
using SpineViewerWPF.Application;

return Cli.Run(args);

static class Cli
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static int Run(string[] args)
    {
        var command = args.FirstOrDefault();
        try
        {
            if (args.Length < 2) throw new ArgumentException(Usage);
            var service = new AssetService(new IRuntimeAdapter[]
            {
                new SpineRuntime.V21_08.LegacyRuntimeAdapter(), new SpineRuntime.V21_25.LegacyRuntimeAdapter(),
                new SpineRuntime.V31_07.LegacyRuntimeAdapter(), new SpineRuntime.V32.LegacyRuntimeAdapter(),
                new SpineRuntime.V34_02.LegacyRuntimeAdapter(), new SpineRuntime.V35_51.LegacyRuntimeAdapter(),
                new SpineRuntime.V36_32.LegacyRuntimeAdapter(), new SpineRuntime.V36_39.LegacyRuntimeAdapter(),
                new SpineRuntime.V36_53.LegacyRuntimeAdapter(), new SpineRuntime.V37_94.LegacyRuntimeAdapter(),
                new SpineRuntime.V38_95.LegacyRuntimeAdapter(), new SpineRuntime.V40_31.LegacyRuntimeAdapter(),
                new SpineV40Adapter(), new SpineV41Adapter(), new SpineRuntime.V42.Adapter(),
                new SpineRuntime.V43.Adapter()
            });
            return command switch
            {
                "inspect" => Inspect(service, args[1], Options.Parse(args[2..], ["atlas", "runtime", "format"])),
                "render" => Render(service, args[1], Options.Parse(
                    args[2..],
                    ["atlas", "runtime", "animation", "time", "output", "width", "height", "skin", "overwrite", "pma"],
                    ["overwrite", "pma"])),
                _ => throw new ArgumentException(Usage)
            };
        }
        catch (OperationCanceledException)
        {
            Console.Error.WriteLine("Canceled.");
            return 130;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error.Message);
            return error switch
            {
                ArgumentException or FormatException or OverflowException => 2,
                NotSupportedException when error.Message.Contains("Runtime", StringComparison.OrdinalIgnoreCase)
                    || error.Message.Contains("export", StringComparison.OrdinalIgnoreCase) => 3,
                FileNotFoundException => 4,
                InvalidDataException when command == "render" => 6,
                InvalidDataException => 5,
                NotSupportedException when command == "render" => 6,
                IOException => 7,
                _ => 1
            };
        }
    }

    private static int Inspect(AssetService service, string skeleton, Options options)
    {
        if (options.Get("format") is { } format && format != "json")
            throw new ArgumentException("Only --format json is supported.");

        Console.WriteLine(JsonSerializer.Serialize(
            service.Inspect(skeleton, options.Get("atlas"), options.Get("runtime")),
            Json));
        return 0;
    }

    private static int Render(AssetService service, string skeleton, Options options)
    {
        var inspection = service.Inspect(skeleton, options.Get("atlas"), options.Get("runtime"));
        var output = service.Render(
            skeleton,
            options.Get("atlas"),
            options.Get("runtime"),
            options.Get("animation") ?? (inspection.Animations.Count == 0 ? "" : options.Require("animation")),
            ParseFloat(options.Require("time"), "time"),
            ParseInt(options.Get("width") ?? "512", "width"),
            ParseInt(options.Get("height") ?? "512", "height"),
            options.Require("output"),
            options.Has("overwrite"),
            options.Has("pma"),
            options.Get("skin") is { } skin ? [skin] : []);

        Console.WriteLine(JsonSerializer.Serialize(new
        {
            success = true,
            outputPath = output,
            runtime = inspection.Runtime.SelectedLine
        }, Json));
        return 0;
    }

    private static float ParseFloat(string value, string option) =>
        float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new FormatException($"--{option} must be a number.");

    private static int ParseInt(string value, string option) =>
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result)
            ? result
            : throw new FormatException($"--{option} must be an integer.");

    private const string Usage =
        """
        Usage:
          spineviewerwpf inspect <skeleton> [--atlas <path>] [--runtime <line>] [--format json]
          spineviewerwpf render <skeleton> --animation <name> --time <seconds> --output <png>
              [--atlas <path>] [--runtime <line>] [--width <pixels>] [--height <pixels>]
              [--skin <name>] [--overwrite] [--pma]
        """;
}

sealed class Options(Dictionary<string, string?> values)
{
    public string? Get(string name) => values.GetValueOrDefault(name);
    public bool Has(string name) => values.ContainsKey(name);
    public string Require(string name) => Get(name) ?? throw new ArgumentException($"--{name} is required.");

    public static Options Parse(string[] args, string[] allowed, string[]? flags = null)
    {
        var flagSet = (flags ?? []).ToHashSet(StringComparer.Ordinal);
        var values = new Dictionary<string, string?>(StringComparer.Ordinal);
        for (var i = 0; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal))
                throw new ArgumentException($"Unexpected argument: {args[i]}");

            var name = args[i][2..];
            if (!allowed.Contains(name, StringComparer.Ordinal))
                throw new ArgumentException($"Unknown option: --{name}");
            if (!values.TryAdd(name, flagSet.Contains(name) ? null : i + 1 < args.Length ? args[++i] : null))
                throw new ArgumentException($"Duplicate option: --{name}");
            if (!flagSet.Contains(name) && values[name] is null)
                throw new ArgumentException($"--{name} requires a value.");
        }
        return new Options(values);
    }
}
