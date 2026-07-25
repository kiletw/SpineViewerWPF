using System;
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
        using var asset = LoadedAsset.Open(skeletonPath, atlasPath);
        return new InspectResult(
            true,
            new AssetDescriptor(skeletonPath, atlasPath, asset.TextureLoader.Paths),
            new RuntimeDescriptor(asset.Data.Version, RuntimeLine, "4.1.00@ab28b77", overridden),
            asset.Data.Animations.Select(x => new AnimationDescriptor(x.Name, x.Duration)).ToArray(),
            asset.Data.Skins.Select(x => x.Name).ToArray(),
            Array.Empty<Diagnostic>());
    }

    public void Render(RenderRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var asset = LoadedAsset.Open(request.SkeletonPath, request.AtlasPath);
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
        private LoadedAsset(Atlas atlas, SkeletonData data, PpmTextureLoader textureLoader)
        {
            Atlas = atlas;
            Data = data;
            TextureLoader = textureLoader;
        }

        public Atlas Atlas { get; }
        public SkeletonData Data { get; }
        public PpmTextureLoader TextureLoader { get; }

        public static LoadedAsset Open(string skeletonPath, string atlasPath)
        {
            var textureLoader = new PpmTextureLoader();
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
                return new LoadedAsset(atlas, data, textureLoader);
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

internal sealed class PpmTextureLoader : TextureLoader
{
    private readonly List<string> paths = new();
    public IReadOnlyList<string> Paths => paths;

    public void Load(AtlasPage page, string path)
    {
        var fullPath = Path.GetFullPath(path);
        page.rendererObject = TextureData.LoadP3(fullPath);
        page.width = ((TextureData)page.rendererObject).Width;
        page.height = ((TextureData)page.rendererObject).Height;
        paths.Add(fullPath);
    }

    public void Unload(object texture)
    {
    }
}
