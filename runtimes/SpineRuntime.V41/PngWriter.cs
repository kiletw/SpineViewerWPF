using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;

#if SPINE_V40
namespace SpineRuntime.V40;
#else
namespace SpineRuntime.V41;
#endif

internal static class PngWriter
{
    private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static void Write(string path, int width, int height, Pixel[] pixels, bool overwrite)
    {
        using var png = new MemoryStream();
        png.Write(Signature);

        var header = new byte[13];
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(0, 4), width);
        BinaryPrimitives.WriteInt32BigEndian(header.AsSpan(4, 4), height);
        header[8] = 8;
        header[9] = 6;
        WriteChunk(png, "IHDR", header);

        var raw = new byte[height * (1 + width * 4)];
        for (var y = 0; y < height; y++)
        {
            var offset = y * (1 + width * 4) + 1;
            for (var x = 0; x < width; x++)
            {
                var pixel = pixels[y * width + x];
                raw[offset++] = pixel.R;
                raw[offset++] = pixel.G;
                raw[offset++] = pixel.B;
                raw[offset++] = pixel.A;
            }
        }

        using var compressed = new MemoryStream();
        using (var zlib = new ZLibStream(compressed, CompressionLevel.SmallestSize, true))
            zlib.Write(raw);
        WriteChunk(png, "IDAT", compressed.ToArray());
        WriteChunk(png, "IEND", []);
        using var output = new FileStream(path, overwrite ? FileMode.Create : FileMode.CreateNew, FileAccess.Write, FileShare.None);
        png.WriteTo(output);
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
}
