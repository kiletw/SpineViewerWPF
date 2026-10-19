using System.Text;
using System.Text.RegularExpressions;

namespace SpineViewerWPF.Application;

// TASK-078: reads a skeleton's export version without parsing the whole file, so
// automatic Runtime detection can skip adapters for other versions. Every
// adapter rejects other versions anyway, but only after a full parse, and the
// vendored 2.1.25 binary parser never finishes on 3.8 data.
internal static partial class SkeletonVersionProbe
{
    private const int JsonProbeBytes = 4096;
    private const int BinaryProbeBytes = 256;

    // The export version, or null when it cannot be read cheaply.
    public static string? Read(string skeletonPath)
    {
        try
        {
            using var stream = new FileStream(skeletonPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var binary = string.Equals(Path.GetExtension(skeletonPath), ".skel", StringComparison.OrdinalIgnoreCase);
            var buffer = new byte[binary ? BinaryProbeBytes : JsonProbeBytes];
            var read = stream.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
            return binary ? ReadBinary(buffer.AsSpan(0, read)) : ReadJson(buffer.AsSpan(0, read));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    // "skeleton": { ..., "spine": "3.8.99", ... } near the start of the file.
    public static string? ReadJson(ReadOnlySpan<byte> prefix)
    {
        var match = JsonVersion().Match(Encoding.UTF8.GetString(prefix));
        return match.Success && IsVersion(match.Groups[1].Value) ? match.Groups[1].Value : null;
    }

    // 2.x/3.x binaries start with the hash and version strings; 4.x binaries
    // start with an 8-byte hash followed by the version string.
    public static string? ReadBinary(ReadOnlySpan<byte> header)
    {
        var offset = 0;
        if (TryReadString(header, ref offset, out _) && TryReadString(header, ref offset, out var legacy) && IsVersion(legacy))
            return legacy;
        offset = 8;
        return header.Length > 8 && TryReadString(header, ref offset, out var modern) && IsVersion(modern) ? modern : null;
    }

    // Major and minor parts ("3.8.55" -> "3.8", "3.2.xx" -> "3.2"), or null.
    public static string? Line(string version)
    {
        var parts = version.Split('.', 3);
        return parts.Length >= 2 && int.TryParse(parts[0], out var major) && int.TryParse(parts[1], out var minor)
            ? $"{major}.{minor}"
            : null;
    }

    private static bool IsVersion(string? value) => value is not null && VersionShape().IsMatch(value);

    // Spine binary strings: a variable-length byte count (0 = null, 1 = empty,
    // otherwise length + 1) followed by UTF-8 bytes.
    private static bool TryReadString(ReadOnlySpan<byte> data, ref int offset, out string? value)
    {
        value = null;
        if (!TryReadVarInt(data, ref offset, out var count)) return false;
        if (count == 0) return true;
        var length = count - 1;
        if (length > 64 || offset + length > data.Length) return false;
        value = Encoding.UTF8.GetString(data.Slice(offset, length));
        offset += length;
        return true;
    }

    private static bool TryReadVarInt(ReadOnlySpan<byte> data, ref int offset, out int value)
    {
        value = 0;
        for (var shift = 0; shift < 35; shift += 7)
        {
            if (offset >= data.Length) return false;
            var current = data[offset++];
            value |= (current & 0x7F) << shift;
            if ((current & 0x80) == 0) return value >= 0;
        }
        return false;
    }

    [GeneratedRegex("\"spine\"\\s*:\\s*\"([^\"]{1,32})\"")]
    private static partial Regex JsonVersion();

    [GeneratedRegex("^\\d+\\.\\d+(\\.[0-9A-Za-z]+)?([-+][0-9A-Za-z.-]+)?$")]
    private static partial Regex VersionShape();
}
