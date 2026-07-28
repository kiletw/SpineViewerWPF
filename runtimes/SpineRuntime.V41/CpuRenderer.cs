using System;
using System.Globalization;
using System.IO;
using System.Linq;
#if SPINE_V40
using Spine4_0_64;
namespace SpineRuntime.V40;
#elif SPINE_LEGACY
#if SPINE_RUNTIME_2108
using Spine2_1_08;
#elif SPINE_RUNTIME_2125
using Spine2_1_25;
#elif SPINE_RUNTIME_3107
using Spine3_1_07;
#elif SPINE_RUNTIME_32XX
using Spine3_2_xx;
#elif SPINE_RUNTIME_3402
using Spine3_4_02;
#elif SPINE_RUNTIME_3551
using Spine3_5_51;
#elif SPINE_RUNTIME_3632
using Spine3_6_32;
#elif SPINE_RUNTIME_3639
using Spine3_6_39;
#elif SPINE_RUNTIME_3653
using Spine3_6_53;
#elif SPINE_RUNTIME_3794
using Spine3_7_94;
#elif SPINE_RUNTIME_3895
using Spine3_8_95;
#elif SPINE_RUNTIME_4031
using Spine4_0_31;
#else
#error A legacy Spine namespace symbol is required.
#endif
namespace SpineRuntime.Legacy;
#else
using Spine;
namespace SpineRuntime.V41;
#endif

internal readonly record struct Pixel(byte R, byte G, byte B, byte A);

internal sealed class TextureData
{
    internal TextureData(int width, int height, Pixel[] pixels)
    {
        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public int Width { get; }
    public int Height { get; }
    public Pixel[] Pixels { get; }

    public Pixel Sample(float u, float v)
    {
        var x = Math.Clamp((int)(u * Width), 0, Width - 1);
        var y = Math.Clamp((int)(v * Height), 0, Height - 1);
        return Pixels[y * Width + x];
    }

    public static TextureData LoadP3(string path)
    {
        var tokens = File.ReadLines(path)
            .Select(line => line.Split('#')[0])
            .SelectMany(line => line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries))
            .ToArray();
        if (tokens.Length < 4 || tokens[0] != "P3") throw new InvalidDataException($"Only P3 PPM textures are supported: {path}");

        var width = int.Parse(tokens[1], CultureInfo.InvariantCulture);
        var height = int.Parse(tokens[2], CultureInfo.InvariantCulture);
        var max = int.Parse(tokens[3], CultureInfo.InvariantCulture);
        if (width < 1 || height < 1 || max < 1 || tokens.Length != 4 + width * height * 3)
            throw new InvalidDataException($"Invalid P3 PPM texture: {path}");

        var pixels = new Pixel[width * height];
        for (var i = 0; i < pixels.Length; i++)
        {
            byte Scale(string value) => (byte)(int.Parse(value, CultureInfo.InvariantCulture) * 255 / max);
            pixels[i] = new Pixel(Scale(tokens[4 + i * 3]), Scale(tokens[5 + i * 3]), Scale(tokens[6 + i * 3]), 255);
        }
        return new TextureData(width, height, pixels);
    }

    public static TextureData LoadPng(string path) => PngReader.Load(path);
}

internal static class CpuRenderer
{
    private readonly record struct Vertex(float X, float Y, float U, float V);
    private static readonly int[] QuadTriangles = [0, 1, 2, 2, 3, 0];

    public static void Render(Skeleton skeleton, int width, int height, string outputPath, bool pma, bool overwrite)
    {
        if (pma) throw new NotSupportedException("PMA textures are not supported by this CPU renderer spike.");

        var pixels = new Pixel[width * height];
        var boundsBuffer = Array.Empty<float>();
#if SPINE_LEGACY_NO_BOUNDS
        GetBounds(skeleton, out var boundsX, out var boundsY, out var boundsWidth, out var boundsHeight);
#else
        skeleton.GetBounds(out var boundsX, out var boundsY, out var boundsWidth, out var boundsHeight, ref boundsBuffer);
#endif
        var hasBounds = float.IsFinite(boundsX) && float.IsFinite(boundsY)
            && float.IsFinite(boundsWidth) && float.IsFinite(boundsHeight)
            && boundsWidth > 0 && boundsHeight > 0;
        var shouldFit = hasBounds && (boundsWidth > width || boundsHeight > height);
        var viewScale = shouldFit
            ? MathF.Min(1, MathF.Min(width / boundsWidth, height / boundsHeight))
            : 1;
        if (!float.IsFinite(viewScale) || viewScale <= 0) viewScale = 1;
        var viewCenterX = shouldFit ? boundsX + boundsWidth / 2 : 0;
        var viewCenterY = shouldFit ? boundsY + boundsHeight / 2 : 0;
        foreach (var slot in skeleton.DrawOrder)
        {
            if (slot.Attachment is null) continue;
#if !SPINE_LEGACY_NO_BLEND
#if SPINE_LEGACY_LOWER_BLEND
            if (slot.Data.BlendMode != BlendMode.normal)
#else
            if (slot.Data.BlendMode != BlendMode.Normal)
#endif
                throw new NotSupportedException($"Blend mode '{slot.Data.BlendMode}' is not supported.");
#endif

            switch (slot.Attachment)
            {
                case RegionAttachment region:
                {
                    var positions = new float[8];
#if SPINE_LEGACY && SPINE_LEGACY_SHORT
                    region.ComputeWorldVertices(slot.Bone, positions);
#elif SPINE_V40 || SPINE_LEGACY
                    region.ComputeWorldVertices(slot.Bone, positions, 0);
#else
                    region.ComputeWorldVertices(slot, positions, 0);
#endif
                    Draw(
                        pixels, width, height, positions, region.UVs, QuadTriangles,
                        GetTexture(region),
                        skeleton.R * slot.R * region.R,
                        skeleton.G * slot.G * region.G,
                        skeleton.B * slot.B * region.B,
                        skeleton.A * slot.A * region.A,
                        viewCenterX, viewCenterY, viewScale);
                    break;
                }
                case MeshAttachment mesh:
                {
#if SPINE_LEGACY && SPINE_LEGACY_OLD_MESH
                    var positions = new float[mesh.Vertices.Length];
#else
                    var positions = new float[mesh.WorldVerticesLength];
#endif
                    mesh.ComputeWorldVertices(slot, positions);
                    Draw(
                        pixels, width, height, positions, mesh.UVs, mesh.Triangles,
                        GetTexture(mesh),
                        skeleton.R * slot.R * mesh.R,
                        skeleton.G * slot.G * mesh.G,
                        skeleton.B * slot.B * mesh.B,
                        skeleton.A * slot.A * mesh.A,
                        viewCenterX, viewCenterY, viewScale);
                    break;
                }
#if !SPINE_LEGACY_NO_CLIPPING
                case ClippingAttachment:
                    throw new NotSupportedException("Clipping attachments are not supported by this CPU renderer spike.");
#endif
            }
        }

        PngWriter.Write(outputPath, width, height, pixels, overwrite);
    }

#if SPINE_V40
    private static TextureData GetTexture(RegionAttachment attachment) =>
        attachment.RendererObject is AtlasRegion { page.rendererObject: TextureData texture }
            ? texture
            : throw new InvalidDataException("Attachment has no loaded atlas texture.");

    private static TextureData GetTexture(MeshAttachment attachment) =>
        attachment.RendererObject is AtlasRegion { page.rendererObject: TextureData texture }
            ? texture
            : throw new InvalidDataException("Attachment has no loaded atlas texture.");
#elif SPINE_LEGACY
    private static TextureData GetTexture(RegionAttachment attachment) =>
        attachment.RendererObject is AtlasRegion { page.rendererObject: TextureData texture }
            ? texture
            : throw new InvalidDataException("Attachment has no loaded atlas texture.");

    private static TextureData GetTexture(MeshAttachment attachment) =>
        attachment.RendererObject is AtlasRegion { page.rendererObject: TextureData texture }
            ? texture
            : throw new InvalidDataException("Attachment has no loaded atlas texture.");
#else
    private static TextureData GetTexture(RegionAttachment attachment) => GetTexture(attachment.Region);
    private static TextureData GetTexture(MeshAttachment attachment) => GetTexture(attachment.Region);

    private static TextureData GetTexture(TextureRegion region) =>
        region is AtlasRegion { page.rendererObject: TextureData texture }
            ? texture
            : throw new InvalidDataException("Attachment has no loaded atlas texture.");
#endif

#if SPINE_LEGACY_NO_BOUNDS
    private static void GetBounds(Skeleton skeleton, out float x, out float y, out float width, out float height)
    {
        x = float.PositiveInfinity;
        y = float.PositiveInfinity;
        var maxX = float.NegativeInfinity;
        var maxY = float.NegativeInfinity;
        foreach (var slot in skeleton.DrawOrder)
        {
            switch (slot.Attachment)
            {
                case RegionAttachment region:
                {
                    var positions = new float[8];
#if SPINE_LEGACY_SHORT
                    region.ComputeWorldVertices(slot.Bone, positions);
#else
                    region.ComputeWorldVertices(slot.Bone, positions, 0);
#endif
                    Accumulate(positions, ref x, ref y, ref maxX, ref maxY);
                    break;
                }
                case MeshAttachment mesh:
                {
#if SPINE_LEGACY_OLD_MESH
                    var positions = new float[mesh.Vertices.Length];
#else
                    var positions = new float[mesh.WorldVerticesLength];
#endif
                    mesh.ComputeWorldVertices(slot, positions);
                    Accumulate(positions, ref x, ref y, ref maxX, ref maxY);
                    break;
                }
            }
        }
        if (!float.IsFinite(x) || !float.IsFinite(y) || !float.IsFinite(maxX) || !float.IsFinite(maxY))
        {
            x = y = width = height = 0;
            return;
        }
        width = maxX - x;
        height = maxY - y;
    }

    private static void Accumulate(float[] positions, ref float minX, ref float minY, ref float maxX, ref float maxY)
    {
        for (var i = 0; i + 1 < positions.Length; i += 2)
        {
            minX = MathF.Min(minX, positions[i]);
            minY = MathF.Min(minY, positions[i + 1]);
            maxX = MathF.Max(maxX, positions[i]);
            maxY = MathF.Max(maxY, positions[i + 1]);
        }
    }
#endif

    private static void Draw(
        Pixel[] target,
        int width,
        int height,
        float[] positions,
        float[] uvs,
        int[] triangles,
        TextureData texture,
        float tintR,
        float tintG,
        float tintB,
        float tintA,
        float viewCenterX,
        float viewCenterY,
        float viewScale)
    {
        var vertices = new Vertex[positions.Length / 2];
        for (var i = 0; i < vertices.Length; i++)
            vertices[i] = new Vertex(
                width / 2f + (positions[i * 2] - viewCenterX) * viewScale,
                height / 2f - (positions[i * 2 + 1] - viewCenterY) * viewScale,
                uvs[i * 2],
                uvs[i * 2 + 1]);

        for (var i = 0; i < triangles.Length; i += 3)
            Rasterize(target, width, height, vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]], texture, tintR, tintG, tintB, tintA);
    }

    private static void Rasterize(
        Pixel[] target,
        int width,
        int height,
        Vertex a,
        Vertex b,
        Vertex c,
        TextureData texture,
        float tintR,
        float tintG,
        float tintB,
        float tintA)
    {
        static float Edge(Vertex p, Vertex q, float x, float y) => (x - p.X) * (q.Y - p.Y) - (y - p.Y) * (q.X - p.X);

        var area = Edge(a, b, c.X, c.Y);
        if (MathF.Abs(area) < 0.0001f) return;
        var minX = Math.Clamp((int)MathF.Floor(MathF.Min(a.X, MathF.Min(b.X, c.X))), 0, width - 1);
        var maxX = Math.Clamp((int)MathF.Ceiling(MathF.Max(a.X, MathF.Max(b.X, c.X))), 0, width - 1);
        var minY = Math.Clamp((int)MathF.Floor(MathF.Min(a.Y, MathF.Min(b.Y, c.Y))), 0, height - 1);
        var maxY = Math.Clamp((int)MathF.Ceiling(MathF.Max(a.Y, MathF.Max(b.Y, c.Y))), 0, height - 1);

        for (var y = minY; y <= maxY; y++)
        for (var x = minX; x <= maxX; x++)
        {
            var px = x + 0.5f;
            var py = y + 0.5f;
            var wa = Edge(b, c, px, py) / area;
            var wb = Edge(c, a, px, py) / area;
            var wc = 1 - wa - wb;
            if (wa < -0.0001f || wb < -0.0001f || wc < -0.0001f) continue;

            var source = texture.Sample(wa * a.U + wb * b.U + wc * c.U, wa * a.V + wb * b.V + wc * c.V);
            Blend(target, y * width + x, source, tintR, tintG, tintB, tintA);
        }
    }

    private static void Blend(Pixel[] target, int index, Pixel source, float tintR, float tintG, float tintB, float tintA)
    {
        var destination = target[index];
        var sourceAlpha = source.A / 255f * Math.Clamp(tintA, 0, 1);
        var destinationAlpha = destination.A / 255f;
        var outputAlpha = sourceAlpha + destinationAlpha * (1 - sourceAlpha);
        if (outputAlpha <= 0) return;

        byte Channel(byte src, byte dst, float tint) =>
            (byte)Math.Clamp(
                MathF.Round(((src / 255f * Math.Clamp(tint, 0, 1) * sourceAlpha) + (dst / 255f * destinationAlpha * (1 - sourceAlpha))) / outputAlpha * 255),
                0,
                255);

        target[index] = new Pixel(
            Channel(source.R, destination.R, tintR),
            Channel(source.G, destination.G, tintG),
            Channel(source.B, destination.B, tintB),
            (byte)MathF.Round(outputAlpha * 255));
    }
}
