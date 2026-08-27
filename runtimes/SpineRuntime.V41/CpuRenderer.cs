using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SpineViewerWPF.Core;
#if SPINE_V43
using Spine;
namespace SpineRuntime.V43;
#elif SPINE_V42
using Spine;
namespace SpineRuntime.V42;
#elif SPINE_V40
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

internal enum CompositeMode
{
    Normal,
    Additive,
    Multiply,
    Screen
}

internal sealed class TextureData
{
    private PreviewTexture previewTexture;

    internal TextureData(string key, int width, int height, Pixel[] pixels)
    {
        Key = Path.GetFullPath(key);
        Width = width;
        Height = height;
        Pixels = pixels;
    }

    public string Key { get; }
    public int Width { get; }
    public int Height { get; }
    public Pixel[] Pixels { get; }

    public PreviewTexture GetPreviewTexture()
    {
        if (previewTexture is not null) return previewTexture;
        var rgba = new byte[Pixels.Length * 4];
        for (var index = 0; index < Pixels.Length; index++)
        {
            var pixel = Pixels[index];
            var offset = index * 4;
            rgba[offset] = pixel.R;
            rgba[offset + 1] = pixel.G;
            rgba[offset + 2] = pixel.B;
            rgba[offset + 3] = pixel.A;
        }
        return previewTexture = new PreviewTexture(Key, Width, Height, rgba);
    }

    public Pixel Sample(float u, float v, bool linearFiltering)
    {
        if (!linearFiltering)
        {
            var nearestX = Math.Clamp((int)(u * Width), 0, Width - 1);
            var nearestY = Math.Clamp((int)(v * Height), 0, Height - 1);
            return Pixels[nearestY * Width + nearestX];
        }

        var sampleX = Math.Clamp(u * Width - 0.5f, 0, Width - 1);
        var sampleY = Math.Clamp(v * Height - 0.5f, 0, Height - 1);
        var x0 = (int)MathF.Floor(sampleX);
        var y0 = (int)MathF.Floor(sampleY);
        var x1 = Math.Min(x0 + 1, Width - 1);
        var y1 = Math.Min(y0 + 1, Height - 1);
        var tx = sampleX - x0;
        var ty = sampleY - y0;
        var topLeft = Pixels[y0 * Width + x0];
        var topRight = Pixels[y0 * Width + x1];
        var bottomLeft = Pixels[y1 * Width + x0];
        var bottomRight = Pixels[y1 * Width + x1];

        byte Channel(byte tl, byte tr, byte bl, byte br)
        {
            var top = tl + (tr - tl) * tx;
            var bottom = bl + (br - bl) * tx;
            return (byte)Math.Clamp(MathF.Round(top + (bottom - top) * ty), 0, 255);
        }

        return new Pixel(
            Channel(topLeft.R, topRight.R, bottomLeft.R, bottomRight.R),
            Channel(topLeft.G, topRight.G, bottomLeft.G, bottomRight.G),
            Channel(topLeft.B, topRight.B, bottomLeft.B, bottomRight.B),
            Channel(topLeft.A, topRight.A, bottomLeft.A, bottomRight.A));
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
        return new TextureData(path, width, height, pixels);
    }

    public static TextureData LoadPng(string path) => PngReader.Load(path);
}

internal static class CpuRenderer
{
    private readonly record struct Vertex(float X, float Y, float U, float V);
    private static readonly int[] QuadTriangles = [0, 1, 2, 2, 3, 0];

    public static void Render(Skeleton skeleton, int width, int height, string outputPath, bool pma, bool overwrite)
    {
        var pixels = RasterizeScene(skeleton, width, height, pma, false, false);
        PngWriter.Write(outputPath, width, height, pixels, overwrite);
    }

    public static RenderedFrame RenderFrame(Skeleton skeleton, int width, int height, bool pma, bool linearFiltering)
    {
        var pixels = RasterizeScene(skeleton, width, height, pma, linearFiltering, true);
        var bgra = new byte[pixels.Length * 4];
        for (var index = 0; index < pixels.Length; index++)
        {
            var pixel = pixels[index];
            var offset = index * 4;
            bgra[offset] = pixel.B;
            bgra[offset + 1] = pixel.G;
            bgra[offset + 2] = pixel.R;
            bgra[offset + 3] = pixel.A;
        }
        return new RenderedFrame(width, height, bgra);
    }

    public static PreviewSceneFrame BuildPreviewScene(Skeleton skeleton, bool pma)
    {
        var boundsBuffer = Array.Empty<float>();
#if SPINE_LEGACY_NO_BOUNDS
        GetBounds(skeleton, out var boundsX, out var boundsY, out var boundsWidth, out var boundsHeight);
#else
        skeleton.GetBounds(out var boundsX, out var boundsY, out var boundsWidth, out var boundsHeight, ref boundsBuffer);
#endif
        if (!float.IsFinite(boundsX) || !float.IsFinite(boundsY)
            || !float.IsFinite(boundsWidth) || !float.IsFinite(boundsHeight)
            || boundsWidth <= 0 || boundsHeight <= 0)
            boundsX = boundsY = boundsWidth = boundsHeight = 0;

        var commands = new List<PreviewDrawCommand>();
#if !SPINE_LEGACY_NO_CLIPPING
        var clipper = new SkeletonClipping();
#endif
#if SPINE_V43
        var drawOrder = skeleton.DrawOrder.AppliedPose;
        var skeletonColor = skeleton.GetColor();
#else
        var drawOrder = skeleton.DrawOrder;
#endif
        foreach (var slot in drawOrder)
        {
#if SPINE_V43
            var slotPose = slot.AppliedPose;
            var attachment = slotPose.Attachment;
#else
            var attachment = slot.Attachment;
#endif
            if (attachment is null)
            {
#if !SPINE_LEGACY_NO_CLIPPING
                clipper.ClipEnd(slot);
#endif
                continue;
            }
#if !SPINE_LEGACY_NO_CLIPPING
            if (attachment is ClippingAttachment clippingAttachment)
            {
#if SPINE_V43
                clipper.ClipStart(skeleton, slot, clippingAttachment);
#else
                clipper.ClipStart(slot, clippingAttachment);
#endif
                continue;
            }
#endif
            var blendMode = ToPreviewBlendMode(GetCompositeMode(slot));
            switch (attachment)
            {
                case RegionAttachment region:
                {
                    var positions = new float[8];
#if SPINE_V43
                    var sequenceIndex = region.Sequence.ResolveIndex(slotPose);
                    var uvs = region.Sequence.GetUVs(sequenceIndex);
                    region.ComputeWorldVertices(
                        slot,
                        region.Sequence.GetOffsets(sequenceIndex),
                        positions,
                        0);
                    var slotColor = slotPose.GetColor();
                    var attachmentColor = region.GetColor();
#elif SPINE_LEGACY && SPINE_LEGACY_SHORT
                    region.ComputeWorldVertices(slot.Bone, positions);
#elif SPINE_V40 || SPINE_LEGACY
                    region.ComputeWorldVertices(slot.Bone, positions, 0);
#else
                    region.ComputeWorldVertices(slot, positions, 0);
#endif
#if !SPINE_LEGACY_NO_CLIPPING
                    AddPreviewAttachment(clipper,
#else
                    AddPreviewAttachment(
#endif
#if SPINE_V43
                        commands, positions, uvs, QuadTriangles,
                        GetTexture(region.Sequence.GetRegion(sequenceIndex)),
                        skeletonColor.r * slotColor.r * attachmentColor.r,
                        skeletonColor.g * slotColor.g * attachmentColor.g,
                        skeletonColor.b * slotColor.b * attachmentColor.b,
                        skeletonColor.a * slotColor.a * attachmentColor.a,
#else
                        commands, positions, region.UVs, QuadTriangles, GetTexture(region),
                        skeleton.R * slot.R * region.R,
                        skeleton.G * slot.G * region.G,
                        skeleton.B * slot.B * region.B,
                        skeleton.A * slot.A * region.A,
#endif
                        blendMode, pma, slot.Data.Name);
                    break;
                }
                case MeshAttachment mesh:
                {
#if SPINE_LEGACY && SPINE_LEGACY_OLD_MESH
                    var positions = new float[mesh.Vertices.Length];
#else
                    var positions = new float[mesh.WorldVerticesLength];
#endif
#if SPINE_V43
                    var sequenceIndex = mesh.Sequence.ResolveIndex(slotPose);
                    var uvs = mesh.Sequence.GetUVs(sequenceIndex);
                    mesh.ComputeWorldVertices(skeleton, slot, positions);
                    var slotColor = slotPose.GetColor();
                    var attachmentColor = mesh.GetColor();
#else
                    mesh.ComputeWorldVertices(slot, positions);
#endif
#if !SPINE_LEGACY_NO_CLIPPING
                    AddPreviewAttachment(clipper,
#else
                    AddPreviewAttachment(
#endif
#if SPINE_V43
                        commands, positions, uvs, mesh.Triangles,
                        GetTexture(mesh.Sequence.GetRegion(sequenceIndex)),
                        skeletonColor.r * slotColor.r * attachmentColor.r,
                        skeletonColor.g * slotColor.g * attachmentColor.g,
                        skeletonColor.b * slotColor.b * attachmentColor.b,
                        skeletonColor.a * slotColor.a * attachmentColor.a,
#else
                        commands, positions, mesh.UVs, mesh.Triangles, GetTexture(mesh),
                        skeleton.R * slot.R * mesh.R,
                        skeleton.G * slot.G * mesh.G,
                        skeleton.B * slot.B * mesh.B,
                        skeleton.A * slot.A * mesh.A,
#endif
                        blendMode, pma, slot.Data.Name);
                    break;
                }
            }
#if !SPINE_LEGACY_NO_CLIPPING
            clipper.ClipEnd(slot);
#endif
        }

        return new PreviewSceneFrame(boundsX, boundsY, boundsWidth, boundsHeight, commands);
    }

    private static Pixel[] RasterizeScene(
        Skeleton skeleton,
        int width,
        int height,
        bool pma,
        bool linearFiltering,
        bool fitViewport)
    {
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
        // ponytail: keep tiny world-space fixtures stable; a production camera should persist its own framing.
        var shouldFitViewport = fitViewport
            && hasBounds
            && (boundsWidth > width / 4f || boundsHeight > height / 4f);
        var shouldFit = hasBounds && (shouldFitViewport || boundsWidth > width || boundsHeight > height);
        var viewScale = shouldFit
            ? shouldFitViewport
                ? MathF.Min(1, MathF.Min(Math.Max(1, width - 32) / boundsWidth, Math.Max(1, height - 32) / boundsHeight))
                : MathF.Min(1, MathF.Min(width / boundsWidth, height / boundsHeight))
            : 1;
        if (!float.IsFinite(viewScale) || viewScale <= 0) viewScale = 1;
        var viewCenterX = shouldFit ? boundsX + boundsWidth / 2 : 0;
        var viewCenterY = shouldFit ? boundsY + boundsHeight / 2 : 0;
#if !SPINE_LEGACY_NO_CLIPPING
        var clipper = new SkeletonClipping();
#endif
#if SPINE_V43
        var drawOrder = skeleton.DrawOrder.AppliedPose;
        var skeletonColor = skeleton.GetColor();
#else
        var drawOrder = skeleton.DrawOrder;
#endif
        foreach (var slot in drawOrder)
        {
#if SPINE_V43
            var slotPose = slot.AppliedPose;
            var attachment = slotPose.Attachment;
#else
            var attachment = slot.Attachment;
#endif
            if (attachment is null)
            {
#if !SPINE_LEGACY_NO_CLIPPING
                clipper.ClipEnd(slot);
#endif
                continue;
            }
#if !SPINE_LEGACY_NO_CLIPPING
            if (attachment is ClippingAttachment clippingAttachment)
            {
#if SPINE_V43
                clipper.ClipStart(skeleton, slot, clippingAttachment);
#else
                clipper.ClipStart(slot, clippingAttachment);
#endif
                continue;
            }
#endif
            var compositeMode = GetCompositeMode(slot);

            switch (attachment)
            {
                case RegionAttachment region:
                {
                    var positions = new float[8];
#if SPINE_V43
                    var sequenceIndex = region.Sequence.ResolveIndex(slotPose);
                    var uvs = region.Sequence.GetUVs(sequenceIndex);
                    region.ComputeWorldVertices(
                        slot,
                        region.Sequence.GetOffsets(sequenceIndex),
                        positions,
                        0);
                    var slotColor = slotPose.GetColor();
                    var attachmentColor = region.GetColor();
#elif SPINE_LEGACY && SPINE_LEGACY_SHORT
                    region.ComputeWorldVertices(slot.Bone, positions);
#elif SPINE_V40 || SPINE_LEGACY
                    region.ComputeWorldVertices(slot.Bone, positions, 0);
#else
                    region.ComputeWorldVertices(slot, positions, 0);
#endif
#if !SPINE_LEGACY_NO_CLIPPING
                    DrawAttachment(clipper,
#else
                    DrawAttachment(
#endif
#if SPINE_V43
                        pixels, width, height, positions, uvs, QuadTriangles,
                        GetTexture(region.Sequence.GetRegion(sequenceIndex)),
                        skeletonColor.r * slotColor.r * attachmentColor.r,
                        skeletonColor.g * slotColor.g * attachmentColor.g,
                        skeletonColor.b * slotColor.b * attachmentColor.b,
                        skeletonColor.a * slotColor.a * attachmentColor.a,
#else
                        pixels, width, height, positions, region.UVs, QuadTriangles,
                        GetTexture(region),
                        skeleton.R * slot.R * region.R,
                        skeleton.G * slot.G * region.G,
                        skeleton.B * slot.B * region.B,
                        skeleton.A * slot.A * region.A,
#endif
                        viewCenterX, viewCenterY, viewScale, compositeMode, pma, linearFiltering);
                    break;
                }
                case MeshAttachment mesh:
                {
#if SPINE_LEGACY && SPINE_LEGACY_OLD_MESH
                    var positions = new float[mesh.Vertices.Length];
#else
                    var positions = new float[mesh.WorldVerticesLength];
#endif
#if SPINE_V43
                    var sequenceIndex = mesh.Sequence.ResolveIndex(slotPose);
                    var uvs = mesh.Sequence.GetUVs(sequenceIndex);
                    mesh.ComputeWorldVertices(skeleton, slot, positions);
                    var slotColor = slotPose.GetColor();
                    var attachmentColor = mesh.GetColor();
#else
                    mesh.ComputeWorldVertices(slot, positions);
#endif
#if !SPINE_LEGACY_NO_CLIPPING
                    DrawAttachment(clipper,
#else
                    DrawAttachment(
#endif
#if SPINE_V43
                        pixels, width, height, positions, uvs, mesh.Triangles,
                        GetTexture(mesh.Sequence.GetRegion(sequenceIndex)),
                        skeletonColor.r * slotColor.r * attachmentColor.r,
                        skeletonColor.g * slotColor.g * attachmentColor.g,
                        skeletonColor.b * slotColor.b * attachmentColor.b,
                        skeletonColor.a * slotColor.a * attachmentColor.a,
#else
                        pixels, width, height, positions, mesh.UVs, mesh.Triangles,
                        GetTexture(mesh),
                        skeleton.R * slot.R * mesh.R,
                        skeleton.G * slot.G * mesh.G,
                        skeleton.B * slot.B * mesh.B,
                        skeleton.A * slot.A * mesh.A,
#endif
                        viewCenterX, viewCenterY, viewScale, compositeMode, pma, linearFiltering);
                    break;
                }
            }
#if !SPINE_LEGACY_NO_CLIPPING
            clipper.ClipEnd(slot);
#endif
        }

        return pixels;
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
#elif SPINE_V43
    private static TextureData GetTexture(TextureRegion region) =>
        region is AtlasRegion { page.rendererObject: TextureData texture }
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

    private static CompositeMode GetCompositeMode(Slot slot)
    {
#if SPINE_LEGACY_NO_BLEND
        return CompositeMode.Normal;
#elif SPINE_LEGACY_LOWER_BLEND
        return slot.Data.BlendMode switch
        {
            BlendMode.additive => CompositeMode.Additive,
            BlendMode.multiply => CompositeMode.Multiply,
            BlendMode.screen => CompositeMode.Screen,
            _ => CompositeMode.Normal
        };
#else
        return slot.Data.BlendMode switch
        {
            BlendMode.Additive => CompositeMode.Additive,
            BlendMode.Multiply => CompositeMode.Multiply,
            BlendMode.Screen => CompositeMode.Screen,
            _ => CompositeMode.Normal
        };
#endif
    }

    private static PreviewBlendMode ToPreviewBlendMode(CompositeMode mode) => mode switch
    {
        CompositeMode.Additive => PreviewBlendMode.Additive,
        CompositeMode.Multiply => PreviewBlendMode.Multiply,
        CompositeMode.Screen => PreviewBlendMode.Screen,
        _ => PreviewBlendMode.Normal
    };

    private static void AddPreviewAttachment(
#if !SPINE_LEGACY_NO_CLIPPING
        SkeletonClipping clipper,
#endif
        List<PreviewDrawCommand> commands,
        float[] positions,
        float[] uvs,
        int[] triangles,
        TextureData texture,
        float tintR,
        float tintG,
        float tintB,
        float tintA,
        PreviewBlendMode blendMode,
        bool pma,
        string slotName)
    {
#if !SPINE_LEGACY_NO_CLIPPING
        var isClipping =
#if SPINE_RUNTIME_3632 || SPINE_RUNTIME_3639
            clipper.IsClipping();
#else
            clipper.IsClipping;
#endif
        if (isClipping)
        {
#if SPINE_V42 || SPINE_V43
            clipper.ClipTriangles(positions, triangles, triangles.Length, uvs);
#else
            clipper.ClipTriangles(positions, positions.Length, triangles, triangles.Length, uvs);
#endif
            AddPreviewDraw(
                commands,
                clipper.ClippedVertices.Items, clipper.ClippedVertices.Count,
                clipper.ClippedUVs.Items,
                clipper.ClippedTriangles.Items, clipper.ClippedTriangles.Count,
                texture, tintR, tintG, tintB, tintA, blendMode, pma, slotName);
            return;
        }
#endif
        AddPreviewDraw(
            commands,
            positions, positions.Length, uvs, triangles, triangles.Length,
            texture, tintR, tintG, tintB, tintA, blendMode, pma, slotName);
    }

    private static void AddPreviewDraw(
        List<PreviewDrawCommand> commands,
        float[] positions,
        int positionsLength,
        float[] uvs,
        int[] triangles,
        int trianglesLength,
        TextureData texture,
        float tintR,
        float tintG,
        float tintB,
        float tintA,
        PreviewBlendMode blendMode,
        bool pma,
        string slotName = "")
    {
        var vertices = new PreviewVertex[positionsLength / 2];
        for (var index = 0; index < vertices.Length; index++)
            vertices[index] = new PreviewVertex(
                positions[index * 2],
                positions[index * 2 + 1],
                uvs[index * 2],
                uvs[index * 2 + 1]);

        var indices = new int[trianglesLength];
        Array.Copy(triangles, indices, trianglesLength);
        commands.Add(new PreviewDrawCommand(
            texture.GetPreviewTexture(),
            vertices,
            indices,
            tintR,
            tintG,
            tintB,
            tintA,
            blendMode,
            pma,
            slotName));
    }

    private static void DrawAttachment(
#if !SPINE_LEGACY_NO_CLIPPING
        SkeletonClipping clipper,
#endif
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
        float viewScale,
        CompositeMode compositeMode,
        bool pma,
        bool linearFiltering)
    {
#if !SPINE_LEGACY_NO_CLIPPING
        var isClipping =
#if SPINE_RUNTIME_3632 || SPINE_RUNTIME_3639
            clipper.IsClipping();
#else
            clipper.IsClipping;
#endif
        if (isClipping)
        {
#if SPINE_V42 || SPINE_V43
            clipper.ClipTriangles(positions, triangles, triangles.Length, uvs);
#else
            clipper.ClipTriangles(positions, positions.Length, triangles, triangles.Length, uvs);
#endif
            Draw(
                target, width, height,
                clipper.ClippedVertices.Items, clipper.ClippedVertices.Count,
                clipper.ClippedUVs.Items,
                clipper.ClippedTriangles.Items, clipper.ClippedTriangles.Count,
                texture, tintR, tintG, tintB, tintA,
                viewCenterX, viewCenterY, viewScale, compositeMode, pma, linearFiltering);
            return;
        }
#endif
        Draw(
            target, width, height,
            positions, positions.Length, uvs, triangles, triangles.Length,
            texture, tintR, tintG, tintB, tintA,
            viewCenterX, viewCenterY, viewScale, compositeMode, pma, linearFiltering);
    }

    private static void Draw(
        Pixel[] target,
        int width,
        int height,
        float[] positions,
        int positionsLength,
        float[] uvs,
        int[] triangles,
        int trianglesLength,
        TextureData texture,
        float tintR,
        float tintG,
        float tintB,
        float tintA,
        float viewCenterX,
        float viewCenterY,
        float viewScale,
        CompositeMode compositeMode,
        bool pma,
        bool linearFiltering)
    {
        var vertices = new Vertex[positionsLength / 2];
        for (var i = 0; i < vertices.Length; i++)
            vertices[i] = new Vertex(
                width / 2f + (positions[i * 2] - viewCenterX) * viewScale,
                height / 2f - (positions[i * 2 + 1] - viewCenterY) * viewScale,
                uvs[i * 2],
                uvs[i * 2 + 1]);

        for (var i = 0; i < trianglesLength; i += 3)
            Rasterize(target, width, height, vertices[triangles[i]], vertices[triangles[i + 1]], vertices[triangles[i + 2]], texture, tintR, tintG, tintB, tintA, compositeMode, pma, linearFiltering);
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
        float tintA,
        CompositeMode compositeMode,
        bool pma,
        bool linearFiltering)
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

            var source = texture.Sample(
                wa * a.U + wb * b.U + wc * c.U,
                wa * a.V + wb * b.V + wc * c.V,
                linearFiltering);
            Blend(target, y * width + x, source, tintR, tintG, tintB, tintA, compositeMode, pma);
        }
    }

    private static void Blend(Pixel[] target, int index, Pixel source, float tintR, float tintG, float tintB, float tintA, CompositeMode compositeMode, bool pma)
    {
        // ponytail: bounded straight-alpha CPU equations; use a production GPU backend for exact blend-state parity.
        var destination = target[index];
        var textureAlpha = source.A / 255f;
        var sourceAlpha = textureAlpha * Math.Clamp(tintA, 0, 1);
        var destinationAlpha = destination.A / 255f;
        var outputAlpha = sourceAlpha + destinationAlpha * (1 - sourceAlpha);
        if (outputAlpha <= 0) return;

        var sourceR = textureAlpha > 0 && pma ? source.R / 255f / textureAlpha : source.R / 255f;
        var sourceG = textureAlpha > 0 && pma ? source.G / 255f / textureAlpha : source.G / 255f;
        var sourceB = textureAlpha > 0 && pma ? source.B / 255f / textureAlpha : source.B / 255f;
        var destinationR = destination.R / 255f;
        var destinationG = destination.G / 255f;
        var destinationB = destination.B / 255f;
        var sourceTintR = sourceR * Math.Clamp(tintR, 0, 1);
        var sourceTintG = sourceG * Math.Clamp(tintG, 0, 1);
        var sourceTintB = sourceB * Math.Clamp(tintB, 0, 1);
        var destinationFactor = destinationAlpha * (1 - sourceAlpha);

        float BlendChannel(float sourceColor, float destinationColor) => compositeMode switch
        {
            CompositeMode.Additive => sourceColor * sourceAlpha + destinationColor * destinationAlpha,
            CompositeMode.Multiply => sourceColor * destinationColor * sourceAlpha + destinationColor * destinationFactor,
            CompositeMode.Screen => (sourceColor + destinationColor - sourceColor * destinationColor) * sourceAlpha + destinationColor * destinationFactor,
            _ => sourceColor * sourceAlpha + destinationColor * destinationFactor
        };

        byte Channel(float premultiplied) =>
            (byte)Math.Clamp(MathF.Round(premultiplied / outputAlpha * 255), 0, 255);

        target[index] = new Pixel(
            Channel(BlendChannel(sourceTintR, destinationR)),
            Channel(BlendChannel(sourceTintG, destinationG)),
            Channel(BlendChannel(sourceTintB, destinationB)),
            (byte)MathF.Round(outputAlpha * 255));
    }
}
