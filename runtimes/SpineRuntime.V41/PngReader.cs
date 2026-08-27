using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Text;

#if SPINE_V43
namespace SpineRuntime.V43;
#elif SPINE_V42
namespace SpineRuntime.V42;
#elif SPINE_V40
namespace SpineRuntime.V40;
#elif SPINE_LEGACY
namespace SpineRuntime.Legacy;
#else
namespace SpineRuntime.V41;
#endif

internal static class PngReader
{
    private static readonly byte[] Signature = [137, 80, 78, 71, 13, 10, 26, 10];
    private const int MaxDimension = 8192;
    private const int MaxChunkBytes = 64 * 1024 * 1024;
    private const int MaxDecodedBytes = 128 * 1024 * 1024;

    public static TextureData Load(string path)
    {
        try
        {
            using var input = File.OpenRead(path);
            Span<byte> signature = stackalloc byte[8];
            input.ReadExactly(signature);
            if (!signature.SequenceEqual(Signature)) throw Invalid(path, "signature");

            using var compressed = new MemoryStream();
            byte[] palette = null;
            byte[] transparency = null;
            var width = 0;
            var height = 0;
            var colorType = -1;
            var channels = 0;
            var hasHeader = false;
            var hasData = false;
            var dataEnded = false;
            var hasEnd = false;
            Span<byte> number = stackalloc byte[4];
            Span<byte> typeBytes = stackalloc byte[4];

            while (!hasEnd)
            {
                input.ReadExactly(number);
                var unsignedLength = BinaryPrimitives.ReadUInt32BigEndian(number);
                if (unsignedLength > MaxChunkBytes) throw Invalid(path, "chunk is too large");
                var length = (int)unsignedLength;

                input.ReadExactly(typeBytes);
                if (!IsChunkType(typeBytes)) throw Invalid(path, "chunk type");
                var type = Encoding.ASCII.GetString(typeBytes);
                var data = new byte[length];
                input.ReadExactly(data);
                input.ReadExactly(number);
                if (BinaryPrimitives.ReadUInt32BigEndian(number) != CalculateCrc(typeBytes, data))
                    throw Invalid(path, $"{type} CRC");

                if (!hasHeader && type != "IHDR") throw Invalid(path, "IHDR must be first");
                if (hasData && type != "IDAT") dataEnded = true;

                switch (type)
                {
                    case "IHDR":
                        if (hasHeader || length != 13) throw Invalid(path, "IHDR");
                        width = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(0, 4));
                        height = BinaryPrimitives.ReadInt32BigEndian(data.AsSpan(4, 4));
                        colorType = data[9];
                        channels = colorType switch
                        {
                            0 => 1,
                            2 => 3,
                            3 => 1,
                            4 => 2,
                            6 => 4,
                            _ => throw Invalid(path, "color type")
                        };
                        if (width is < 1 or > MaxDimension || height is < 1 or > MaxDimension)
                            throw Invalid(path, "dimensions");
                        if (data[8] != 8 || data[10] != 0 || data[11] != 0 || data[12] != 0)
                            throw Invalid(path, "only 8-bit non-interlaced PNG is supported");
                        hasHeader = true;
                        break;

                    case "PLTE":
                        if (hasData || palette is not null || transparency is not null || colorType is 0 or 4
                            || length is < 3 or > 768 || length % 3 != 0)
                            throw Invalid(path, "palette");
                        palette = data;
                        break;

                    case "tRNS":
                        if (hasData || transparency is not null
                            || colorType == 3 && (palette is null || length < 1 || length > palette.Length / 3)
                            || colorType == 0 && length != 2
                            || colorType == 2 && length != 6
                            || colorType is 4 or 6)
                            throw Invalid(path, "transparency");
                        transparency = data;
                        break;

                    case "IDAT":
                        if (dataEnded || colorType == 3 && palette is null) throw Invalid(path, "IDAT order");
                        if (compressed.Length + length > MaxDecodedBytes) throw Invalid(path, "compressed data is too large");
                        compressed.Write(data);
                        hasData = true;
                        break;

                    case "IEND":
                        if (!hasData || length != 0) throw Invalid(path, "IEND");
                        hasEnd = true;
                        break;

                    default:
                        if ((typeBytes[0] & 0x20) == 0) throw Invalid(path, $"unknown critical chunk {type}");
                        break;
                }
            }

            if (input.Position != input.Length) throw Invalid(path, "trailing data");
            return Decode(path, width, height, colorType, channels, palette, transparency, compressed);
        }
        catch (EndOfStreamException exception)
        {
            throw new InvalidDataException($"Truncated PNG texture: {path}", exception);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (FileNotFoundException)
        {
            throw;
        }
        catch (DirectoryNotFoundException)
        {
            throw;
        }
        catch (Exception exception) when (exception is IOException or OverflowException)
        {
            throw new InvalidDataException($"Invalid PNG texture: {path}", exception);
        }
    }

    private static TextureData Decode(
        string path,
        int width,
        int height,
        int colorType,
        int channels,
        byte[] palette,
        byte[] transparency,
        MemoryStream compressed)
    {
        var stride = checked(width * channels);
        var rawLength = checked(height * (stride + 1));
        var pixelBytesLength = checked(height * stride);
        if (rawLength > MaxDecodedBytes) throw Invalid(path, "decoded data is too large");

        var raw = new byte[rawLength];
        compressed.Position = 0;
        try
        {
            using var zlib = new ZLibStream(compressed, CompressionMode.Decompress);
            zlib.ReadExactly(raw);
            if (zlib.ReadByte() != -1) throw Invalid(path, "decompressed length");
        }
        catch (InvalidDataException exception)
        {
            throw new InvalidDataException($"Invalid compressed PNG data: {path}", exception);
        }

        var decoded = new byte[pixelBytesLength];
        for (var y = 0; y < height; y++)
        {
            var filter = raw[y * (stride + 1)];
            if (filter > 4) throw Invalid(path, "scanline filter");
            var source = y * (stride + 1) + 1;
            var target = y * stride;
            for (var x = 0; x < stride; x++)
            {
                var left = x >= channels ? decoded[target + x - channels] : 0;
                var up = y > 0 ? decoded[target + x - stride] : 0;
                var upperLeft = y > 0 && x >= channels ? decoded[target + x - stride - channels] : 0;
                var predictor = filter switch
                {
                    0 => 0,
                    1 => left,
                    2 => up,
                    3 => (left + up) / 2,
                    4 => Paeth(left, up, upperLeft),
                    _ => 0
                };
                decoded[target + x] = unchecked((byte)(raw[source + x] + predictor));
            }
        }

        var pixels = new Pixel[checked(width * height)];
        var transparentGray = transparency is { Length: 2 }
            ? BinaryPrimitives.ReadUInt16BigEndian(transparency)
            : -1;
        var transparentRed = colorType == 2 && transparency is { Length: 6 }
            ? BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(0, 2))
            : -1;
        var transparentGreen = colorType == 2 && transparency is { Length: 6 }
            ? BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(2, 2))
            : -1;
        var transparentBlue = colorType == 2 && transparency is { Length: 6 }
            ? BinaryPrimitives.ReadUInt16BigEndian(transparency.AsSpan(4, 2))
            : -1;

        for (var i = 0; i < pixels.Length; i++)
        {
            var offset = i * channels;
            pixels[i] = colorType switch
            {
                0 => new Pixel(
                    decoded[offset], decoded[offset], decoded[offset],
                    decoded[offset] == transparentGray ? (byte)0 : (byte)255),
                2 => new Pixel(
                    decoded[offset], decoded[offset + 1], decoded[offset + 2],
                    decoded[offset] == transparentRed
                    && decoded[offset + 1] == transparentGreen
                    && decoded[offset + 2] == transparentBlue ? (byte)0 : (byte)255),
                3 => PalettePixel(path, decoded[offset], palette, transparency),
                4 => new Pixel(decoded[offset], decoded[offset], decoded[offset], decoded[offset + 1]),
                6 => new Pixel(decoded[offset], decoded[offset + 1], decoded[offset + 2], decoded[offset + 3]),
                _ => throw Invalid(path, "color type")
            };
        }

        return new TextureData(path, width, height, pixels);
    }

    private static Pixel PalettePixel(string path, byte index, byte[] palette, byte[] transparency)
    {
        var offset = index * 3;
        if (offset + 2 >= palette.Length) throw Invalid(path, "palette index");
        return new Pixel(
            palette[offset],
            palette[offset + 1],
            palette[offset + 2],
            transparency is not null && index < transparency.Length ? transparency[index] : (byte)255);
    }

    private static int Paeth(int left, int up, int upperLeft)
    {
        var estimate = left + up - upperLeft;
        var leftDistance = Math.Abs(estimate - left);
        var upDistance = Math.Abs(estimate - up);
        var upperLeftDistance = Math.Abs(estimate - upperLeft);
        return leftDistance <= upDistance && leftDistance <= upperLeftDistance
            ? left
            : upDistance <= upperLeftDistance ? up : upperLeft;
    }

    private static uint CalculateCrc(ReadOnlySpan<byte> type, ReadOnlySpan<byte> data)
    {
        var crc = 0xffffffffu;
        foreach (var value in type) crc = UpdateCrc(crc, value);
        foreach (var value in data) crc = UpdateCrc(crc, value);
        return ~crc;
    }

    private static bool IsChunkType(ReadOnlySpan<byte> type)
    {
        foreach (var value in type)
            if (value is not (>= (byte)'A' and <= (byte)'Z') and not (>= (byte)'a' and <= (byte)'z'))
                return false;
        return (type[2] & 0x20) == 0;
    }

    private static uint UpdateCrc(uint crc, byte value)
    {
        crc ^= value;
        for (var bit = 0; bit < 8; bit++)
            crc = (crc & 1) == 0 ? crc >> 1 : 0xedb88320u ^ (crc >> 1);
        return crc;
    }

    private static InvalidDataException Invalid(string path, string reason) =>
        new($"Invalid PNG {reason}: {path}");
}
