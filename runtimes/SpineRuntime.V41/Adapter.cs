using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Spine;
using SpineViewerWPF.Application;
using SpineViewerWPF.Core;

namespace SpineRuntime.V41;

public sealed class SpineV41Adapter : IRuntimeAdapter
{
    public string RuntimeLine => "4.1";

    public InspectResult Inspect(
        string skeletonPath,
        string atlasPath,
        bool overridden,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var textureLoader = new MetadataTextureLoader();
        using var asset = LoadedAsset.Open(skeletonPath, atlasPath, textureLoader, textureLoader.Paths);
        return new InspectResult(
            true,
            new AssetDescriptor(skeletonPath, atlasPath, asset.TexturePaths),
            new RuntimeDescriptor(asset.Data.Version, RuntimeLine, "4.1.00@ab28b77", overridden),
            asset.Data.Animations.Select(x => new AnimationDescriptor(x.Name, x.Duration)).ToArray(),
            asset.Data.Skins.Select(x => x.Name).ToArray(),
            Array.Empty<Diagnostic>());
    }

    public void Render(RenderRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var textureLoader = new RenderTextureLoader();
        using var asset = LoadedAsset.Open(request.SkeletonPath, request.AtlasPath, textureLoader, textureLoader.Paths);
        var animation = asset.Data.FindAnimation(request.Animation)
            ?? throw new InvalidDataException($"Animation not found: {request.Animation}");
        if (request.Skins.Count > 1)
            throw new NotSupportedException("This vertical slice supports at most one skin.");

        var skeleton = new Skeleton(asset.Data);
        if (request.Skins.Count == 1)
        {
            skeleton.SetSkin(request.Skins[0]);
            skeleton.SetSlotsToSetupPose();
        }

        var state = new AnimationState(new AnimationStateData(asset.Data));
        state.SetAnimation(0, animation, false);
        state.Update(request.TimeSeconds);
        state.Apply(skeleton);
        skeleton.UpdateWorldTransform();

        CpuRenderer.Render(skeleton, request.Width, request.Height, request.OutputPath, request.Pma, request.Overwrite);
    }

    private sealed class LoadedAsset : IDisposable
    {
        private LoadedAsset(Atlas atlas, SkeletonData data, IReadOnlyList<string> texturePaths)
        {
            Atlas = atlas;
            Data = data;
            TexturePaths = texturePaths;
        }

        public Atlas Atlas { get; }
        public SkeletonData Data { get; }
        public IReadOnlyList<string> TexturePaths { get; }

        public static LoadedAsset Open(
            string skeletonPath,
            string atlasPath,
            TextureLoader textureLoader,
            IReadOnlyList<string> texturePaths)
        {
            Atlas atlas = null;
            try
            {
                atlas = new Atlas(atlasPath, textureLoader);
                var attachments = new AtlasAttachmentLoader(atlas);
                SkeletonData data = string.Equals(Path.GetExtension(skeletonPath), ".skel", StringComparison.OrdinalIgnoreCase)
                    ? new SkeletonBinary(attachments).ReadSkeletonData(skeletonPath)
                    : new SkeletonJson(attachments).ReadSkeletonData(skeletonPath);
                if (data.Version is null || !data.Version.StartsWith("4.1", StringComparison.Ordinal))
                    throw new NotSupportedException($"Expected a 4.1 export, found '{data.Version ?? "unknown"}'.");
                return new LoadedAsset(atlas, data, texturePaths);
            }
            catch
            {
                atlas?.Dispose();
                throw;
            }
        }

        public void Dispose() => Atlas.Dispose();
    }
}

internal sealed class MetadataTextureLoader : TextureLoader
{
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
    private readonly List<string> paths = new();
    public IReadOnlyList<string> Paths => paths;

    public void Load(AtlasPage page, string path)
    {
        var fullPath = Path.GetFullPath(path);
        var size = Path.GetExtension(fullPath).ToLowerInvariant() switch
        {
            ".png" => ReadPngSize(fullPath),
            ".ppm" => ReadPpmSize(fullPath),
            _ => throw new NotSupportedException($"Texture metadata format is not supported: {fullPath}")
        };
        page.rendererObject = fullPath;
        page.width = size.Width;
        page.height = size.Height;
        paths.Add(fullPath);
    }

    public void Unload(object texture)
    {
    }

    private static (int Width, int Height) ReadPngSize(string path)
    {
        Span<byte> header = stackalloc byte[24];
        try
        {
            using var stream = File.OpenRead(path);
            stream.ReadExactly(header);
        }
        catch (EndOfStreamException exception)
        {
            throw new InvalidDataException($"Truncated PNG texture: {path}", exception);
        }

        if (!header[..8].SequenceEqual(PngSignature)
            || BinaryPrimitives.ReadInt32BigEndian(header[8..12]) != 13
            || !header[12..16].SequenceEqual("IHDR"u8))
            throw new InvalidDataException($"Invalid PNG texture header: {path}");

        var width = BinaryPrimitives.ReadInt32BigEndian(header[16..20]);
        var height = BinaryPrimitives.ReadInt32BigEndian(header[20..24]);
        if (width < 1 || height < 1)
            throw new InvalidDataException($"Invalid PNG texture dimensions: {path}");
        return (width, height);
    }

    private static (int Width, int Height) ReadPpmSize(string path)
    {
        var texture = TextureData.LoadP3(path);
        return (texture.Width, texture.Height);
    }
}

internal sealed class RenderTextureLoader : TextureLoader
{
    private readonly List<string> paths = new();
    public IReadOnlyList<string> Paths => paths;

    public void Load(AtlasPage page, string path)
    {
        var fullPath = Path.GetFullPath(path);
        page.rendererObject = Path.GetExtension(fullPath).ToLowerInvariant() switch
        {
            ".png" => TextureData.LoadPng(fullPath),
            ".ppm" => TextureData.LoadP3(fullPath),
            _ => throw new NotSupportedException($"Render texture format is not supported: {fullPath}")
        };
        page.width = ((TextureData)page.rendererObject).Width;
        page.height = ((TextureData)page.rendererObject).Height;
        paths.Add(fullPath);
    }

    public void Unload(object texture)
    {
    }
}
