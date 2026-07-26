using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace SharpJson;

// Project-owned compatibility bridge for the vendored 4.0 parser's historical decoder API.
internal sealed class JsonDecoder
{
    public bool parseNumbersAsFloat { get; set; }

    public object Decode(string text) => Convert(JsonDocument.Parse(text).RootElement);

    private object Convert(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Object => Object(value),
        JsonValueKind.Array => Array(value),
        JsonValueKind.String => value.GetString(),
        JsonValueKind.Number => parseNumbersAsFloat
            ? (object)float.Parse(value.GetRawText(), NumberStyles.Float, CultureInfo.InvariantCulture)
            : double.Parse(value.GetRawText(), NumberStyles.Float, CultureInfo.InvariantCulture),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Null => null,
        _ => throw new InvalidDataException($"Unsupported JSON value: {value.ValueKind}")
    };

    private Dictionary<string, object> Object(JsonElement value)
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
            result[property.Name] = Convert(property.Value);
        return result;
    }

    private List<object> Array(JsonElement value)
    {
        var result = new List<object>();
        foreach (var item in value.EnumerateArray())
            result.Add(Convert(item));
        return result;
    }
}
