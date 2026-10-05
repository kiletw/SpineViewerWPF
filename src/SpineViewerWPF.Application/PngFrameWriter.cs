using System.Buffers.Binary;
using System.IO.Compression;
using System.Text;
using SpineViewerWPF.Core;

namespace SpineViewerWPF.Application;

internal static class PngFrameWriter
{
    private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static void Write(string path, RenderedFrame frame, bool overwrite)
    {
        ArgumentNullException.ThrowIfNull(frame);
        if (frame.Width < 1 || frame.Height < 1
            || frame.Bgra32.Length != checked(frame.Width * frame.Height * 4))
            throw new InvalidDataException("The composite frame has invalid BGRA dimensions.");

        using var png = new MemoryStream();
        png.Write(Signature);

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), frame.Width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), frame.Height);
        header[8] = 8;
        header[9] = 6;
        WriteChunk(png, "IHDR", header);

        var raw = new byte[checked(frame.Height * (1 + frame.Width * 4))];
        for (var y = 0; y < frame.Height; y++)
        {
            var source = y * frame.Width * 4;
            var target = y * (1 + frame.Width * 4) + 1;
            for (var x = 0; x < frame.Width; x++)
            {
                raw[target++] = frame.Bgra32[source + 2];
                raw[target++] = frame.Bgra32[source + 1];
                raw[target++] = frame.Bgra32[source];
                raw[target++] = frame.Bgra32[source + 3];
                source += 4;
            }
        }

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, true))
            zlib.Write(raw);
        WriteChunk(png, "IDAT", compressed.ToArray());
        WriteChunk(png, "IEND", []);

        var ownsCreatedFile = false;
        try
        {
            using var output = new FileStream(
                path,
                overwrite ? FileMode.Create : FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None);
            ownsCreatedFile = !overwrite;
            png.WriteTo(output);
        }
        catch
        {
            if (ownsCreatedFile) TryDelete(path);
            throw;
        }
    }

    private static void WriteChunk(Stream stream, string type, byte[] data)
    {
        Span<byte> number = stackalloc byte[4];
        BinaryPrimitives.WriteInt32BigEndian(number, data.Length);
        stream.Write(number);

        var typeBytes = Encoding.ASCII.GetBytes(type);
        stream.Write(typeBytes);
        stream.Write(data);

        var crc = 0xffffffffu;
        foreach (var value in typeBytes) crc = UpdateCrc(crc, value);
        foreach (var value in data) crc = UpdateCrc(crc, value);
        BinaryPrimitives.WriteUInt32BigEndian(number, ~crc);
        stream.Write(number);
    }

    private static uint UpdateCrc(uint crc, byte value)
    {
        crc ^= value;
        for (var bit = 0; bit < 8; bit++)
            crc = (crc & 1) == 0 ? crc >> 1 : 0xedb88320u ^ (crc >> 1);
        return crc;
    }

    private static void TryDelete(string path)
    {
        try { File.Delete(path); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
