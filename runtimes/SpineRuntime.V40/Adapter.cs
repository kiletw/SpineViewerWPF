using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Spine = Spine4_0_64;
using SpineViewerWPF.Application;
using SpineViewerWPF.Core;

namespace SpineRuntime.V40;

public sealed class SpineV40Adapter : IRuntimeAdapter
{
    public string RuntimeLine => "4.0";

    public InspectResult Inspect(string skeletonPath, string atlasPath, bool overridden, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var textureLoader = new MetadataTextureLoader();
        using var asset = LoadedAsset.Open(skeletonPath, atlasPath, textureLoader, textureLoader.Paths);
        return new InspectResult(
            true,
            new AssetDescriptor(skeletonPath, atlasPath, asset.TexturePaths),
            new RuntimeDescriptor(asset.Data.Version, RuntimeLine, "4.0.64@01524d4", overridden),
            asset.Data.Animations.Select(item => new AnimationDescriptor(item.Name, item.Duration)).ToArray(),
            asset.Data.Skins.Select(item => item.Name).ToArray(),
            Array.Empty<Diagnostic>());
    }

    public IRuntimeRenderSession OpenSession(string skeletonPath, string atlasPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var textureLoader = new RenderTextureLoader();
        return new RenderSession(LoadedAsset.Open(skeletonPath, atlasPath, textureLoader, textureLoader.Paths));
    }

    private sealed class RenderSession : IRuntimeRenderSession
    {
        private LoadedAsset asset;

        public RenderSession(LoadedAsset asset) => this.asset = asset;

        public void Render(RenderRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var loaded = asset ?? throw new ObjectDisposedException(nameof(RenderSession));
            var animation = loaded.Data.FindAnimation(request.Animation)
                ?? throw new InvalidDataException($"Animation not found: {request.Animation}");
            if (request.Skins.Count > 1)
                throw new NotSupportedException("This vertical slice supports at most one skin.");

            var skeleton = new Spine.Skeleton(loaded.Data);
            if (request.Skins.Count == 1)
            {
                skeleton.SetSkin(request.Skins[0]);
                skeleton.SetSlotsToSetupPose();
            }

            var state = new Spine.AnimationState(new Spine.AnimationStateData(loaded.Data));
            state.SetAnimation(0, animation, false).Alpha = request.TrackAlpha;
            state.Update(request.TimeSeconds);
            state.Apply(skeleton);
            skeleton.UpdateWorldTransform();
            CpuRenderer.Render(skeleton, request.Width, request.Height, request.OutputPath, request.Pma, request.Overwrite);
        }

        public void Dispose()
        {
            asset?.Dispose();
            asset = null;
        }
    }

    private sealed class LoadedAsset : IDisposable
    {
        private LoadedAsset(Spine.Atlas atlas, Spine.SkeletonData data, IReadOnlyList<string> texturePaths)
        {
            Atlas = atlas;
            Data = data;
            TexturePaths = texturePaths;
        }

        public Spine.Atlas Atlas { get; }
        public Spine.SkeletonData Data { get; }
        public IReadOnlyList<string> TexturePaths { get; }

        public static LoadedAsset Open(string skeletonPath, string atlasPath, Spine.TextureLoader textureLoader, IReadOnlyList<string> texturePaths)
        {
            Spine.Atlas atlas = null;
            try
            {
                atlas = new Spine.Atlas(atlasPath, textureLoader);
                var attachments = new Spine.AtlasAttachmentLoader(atlas);
                Spine.SkeletonData data = string.Equals(Path.GetExtension(skeletonPath), ".skel", StringComparison.OrdinalIgnoreCase)
                    ? new Spine.SkeletonBinary(attachments).ReadSkeletonData(skeletonPath)
                    : new Spine.SkeletonJson(attachments).ReadSkeletonData(skeletonPath);
                if (data.Version is null || !data.Version.StartsWith("4.0.64", StringComparison.Ordinal))
                    throw new NotSupportedException($"Expected a 4.0.64 export, found '{data.Version ?? "unknown"}'.");
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

internal sealed class MetadataTextureLoader : Spine.TextureLoader
{
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
    private readonly List<string> paths = new();
    public IReadOnlyList<string> Paths => paths;

    public void Load(Spine.AtlasPage page, string path)
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
        if (width < 1 || height < 1) throw new InvalidDataException($"Invalid PNG texture dimensions: {path}");
        return (width, height);
    }

    private static (int Width, int Height) ReadPpmSize(string path)
    {
        var texture = TextureData.LoadP3(path);
        return (texture.Width, texture.Height);
    }
}

internal sealed class RenderTextureLoader : Spine.TextureLoader
{
    private readonly List<string> paths = new();
    public IReadOnlyList<string> Paths => paths;

    public void Load(Spine.AtlasPage page, string path)
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
