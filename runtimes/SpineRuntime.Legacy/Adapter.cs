using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SpineViewerWPF.Application;
using SpineViewerWPF.Core;
using SpineRuntime.Legacy;

#if SPINE_RUNTIME_2108
using RuntimeSpine = Spine2_1_08;
namespace SpineRuntime.V21_08;
internal static class RuntimeInfo { public const string Line = "2.1.08"; public const string Prefix = "2.1.08"; public const string Commit = "2.1.08@39ce4b2"; }
#elif SPINE_RUNTIME_2125
using RuntimeSpine = Spine2_1_25;
namespace SpineRuntime.V21_25;
internal static class RuntimeInfo { public const string Line = "2.1.25"; public const string Prefix = "2.1.25"; public const string Commit = "2.1.25@142e770"; }
#elif SPINE_RUNTIME_3107
using RuntimeSpine = Spine3_1_07;
namespace SpineRuntime.V31_07;
internal static class RuntimeInfo { public const string Line = "3.1.07"; public const string Prefix = "3.1.07"; public const string Commit = "3.1.07@e74b61e"; }
#elif SPINE_RUNTIME_32XX
using RuntimeSpine = Spine3_2_xx;
namespace SpineRuntime.V32;
internal static class RuntimeInfo { public const string Line = "3.2.xx"; public const string Prefix = "3.2"; public const string Commit = "3.2.xx@local"; }
#elif SPINE_RUNTIME_3402
using RuntimeSpine = Spine3_4_02;
namespace SpineRuntime.V34_02;
internal static class RuntimeInfo { public const string Line = "3.4.02"; public const string Prefix = "3.4.02"; public const string Commit = "3.4.02@ef50131"; }
#elif SPINE_RUNTIME_3551
using RuntimeSpine = Spine3_5_51;
namespace SpineRuntime.V35_51;
internal static class RuntimeInfo { public const string Line = "3.5.51"; public const string Prefix = "3.5.51"; public const string Commit = "3.5.51@2cd9467"; }
#elif SPINE_RUNTIME_3632
using RuntimeSpine = Spine3_6_32;
namespace SpineRuntime.V36_32;
internal static class RuntimeInfo { public const string Line = "3.6.32"; public const string Prefix = "3.6.32"; public const string Commit = "3.6.32@283f63b"; }
#elif SPINE_RUNTIME_3639
using RuntimeSpine = Spine3_6_39;
namespace SpineRuntime.V36_39;
internal static class RuntimeInfo { public const string Line = "3.6.39"; public const string Prefix = "3.6.39"; public const string Commit = "3.6.39@43f37ce"; }
#elif SPINE_RUNTIME_3653
using RuntimeSpine = Spine3_6_53;
namespace SpineRuntime.V36_53;
internal static class RuntimeInfo { public const string Line = "3.6.53"; public const string Prefix = "3.6.53"; public const string Commit = "3.6.53@a4a36d8"; }
#elif SPINE_RUNTIME_3794
using RuntimeSpine = Spine3_7_94;
namespace SpineRuntime.V37_94;
internal static class RuntimeInfo { public const string Line = "3.7.94"; public const string Prefix = "3.7.94"; public const string Commit = "3.7.94@45b8125"; }
#elif SPINE_RUNTIME_3895
using RuntimeSpine = Spine3_8_95;
namespace SpineRuntime.V38_95;
internal static class RuntimeInfo { public const string Line = "3.8.95"; public const string Prefix = "3.8"; public const string Commit = "3.8.95@3e93e2d"; }
#elif SPINE_RUNTIME_4031
using RuntimeSpine = Spine4_0_31;
namespace SpineRuntime.V40_31;
internal static class RuntimeInfo { public const string Line = "4.0.31"; public const string Prefix = "4.0.31"; public const string Commit = "4.0.31@8770e31"; }
#else
#error A legacy Runtime symbol is required.
#endif

public sealed class LegacyRuntimeAdapter : IRuntimeAdapter
{
    public string RuntimeLine => RuntimeInfo.Line;

    public InspectResult Inspect(string skeletonPath, string atlasPath, bool overridden, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var loader = new MetadataTextureLoader();
        using var asset = LoadedAsset.Open(skeletonPath, atlasPath, loader, loader.Paths);
        return new InspectResult(
            true,
            new AssetDescriptor(skeletonPath, atlasPath, asset.TexturePaths),
            new RuntimeDescriptor(asset.Data.Version, RuntimeLine, RuntimeInfo.Commit, overridden),
            asset.Data.Animations.Select(item => new AnimationDescriptor(item.Name, item.Duration)).ToArray(),
            asset.Data.Skins.Select(item => item.Name).ToArray(),
            Array.Empty<Diagnostic>(),
            BuildSlots(asset.Data));
    }

    private static IReadOnlyList<SlotDescriptor> BuildSlots(RuntimeSpine.SkeletonData data)
    {
        var result = new List<SlotDescriptor>(data.Slots.Count);
        for (var slotIndex = 0; slotIndex < data.Slots.Count; slotIndex++)
        {
#if SPINE_RUNTIME_2108 || SPINE_RUNTIME_2125
            var slot = data.Slots[slotIndex];
#else
            var slot = data.Slots.Items[slotIndex];
#endif
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var skin in data.Skins)
            {
#if SPINE_RUNTIME_3895 || SPINE_RUNTIME_4031
                var entries = new List<RuntimeSpine.Skin.SkinEntry>();
                skin.GetAttachments(slotIndex, entries);
                foreach (var entry in entries) names.Add(entry.Name);
#else
                var skinNames = new List<string>();
                skin.FindNamesForSlot(slotIndex, skinNames);
                foreach (var name in skinNames) names.Add(name);
#endif
            }
            if (!string.IsNullOrWhiteSpace(slot.AttachmentName)) names.Add(slot.AttachmentName);
            result.Add(new SlotDescriptor(slot.Name, slot.AttachmentName, names.OrderBy(x => x, StringComparer.Ordinal).ToArray()));
        }
        return result;
    }

    public IRuntimeRenderSession OpenSession(string skeletonPath, string atlasPath, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var loader = new RenderTextureLoader();
        return new RenderSession(LoadedAsset.Open(skeletonPath, atlasPath, loader, loader.Paths));
    }

    private sealed class RenderSession : IRuntimeRenderSession
    {
        private LoadedAsset asset;
        private readonly RuntimeSpine.Skeleton skeleton;
        private readonly RuntimeSpine.AnimationStateData stateData;

        public RenderSession(LoadedAsset asset)
        {
            this.asset = asset;
            skeleton = new RuntimeSpine.Skeleton(asset.Data);
            stateData = new RuntimeSpine.AnimationStateData(asset.Data);
        }

        public void Render(RenderRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var skeleton = Pose(new FrameRenderRequest(
                request.Animation,
                request.TimeSeconds,
                request.Width,
                request.Height,
                request.Pma,
                request.Skins,
                request.TrackAlpha,
                false,
                request.Slots));
            CpuRenderer.Render(skeleton, request.Width, request.Height, request.OutputPath, request.Pma, request.Overwrite);
        }

        public RenderedFrame RenderFrame(FrameRenderRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var skeleton = Pose(request);
            return CpuRenderer.RenderFrame(skeleton, request.Width, request.Height, request.Pma, request.LinearFiltering);
        }

        public PreviewSceneFrame RenderScene(PreviewSceneRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var skeleton = Pose(new FrameRenderRequest(
                request.Animation, request.TimeSeconds, 1, 1, request.Pma, request.Skins, request.TrackAlpha, false, request.Slots));
            return CpuRenderer.BuildPreviewScene(skeleton, request.Pma);
        }

        private RuntimeSpine.Skeleton Pose(FrameRenderRequest request)
        {
            var loaded = asset ?? throw new ObjectDisposedException(nameof(RenderSession));
            var animation = loaded.Data.FindAnimation(request.Animation)
                ?? throw new InvalidDataException($"Animation not found: {request.Animation}");
            if (request.Skins.Count > 1) throw new NotSupportedException("This vertical slice supports at most one skin.");

            skeleton.Skin = null;
            skeleton.SetToSetupPose();
            if (request.Skins.Count == 1)
            {
                skeleton.SetSkin(request.Skins[0]);
                skeleton.SetSlotsToSetupPose();
            }

            var state = new RuntimeSpine.AnimationState(stateData);
            var entry = state.SetAnimation(0, animation, false);
#if SPINE_RUNTIME_2108 || SPINE_RUNTIME_2125 || SPINE_RUNTIME_3107 || SPINE_RUNTIME_32XX || SPINE_RUNTIME_3402
            entry.Mix = request.TrackAlpha;
#else
            entry.Alpha = request.TrackAlpha;
#endif
            state.Update(request.TimeSeconds);
            state.Apply(skeleton);
            ApplySlotDisplaySettings(skeleton, request.Slots);
            skeleton.UpdateWorldTransform();
            return skeleton;
        }

        private static void ApplySlotDisplaySettings(RuntimeSpine.Skeleton skeleton, IReadOnlyList<SlotDisplayDocument> settings)
        {
            if (settings is null || settings.Count == 0) return;
            var byName = settings.ToDictionary(item => item.Name, StringComparer.Ordinal);
            for (var index = 0; index < skeleton.Slots.Count; index++)
            {
#if SPINE_RUNTIME_2108 || SPINE_RUNTIME_2125
                var slot = skeleton.Slots[index];
#else
                var slot = skeleton.Slots.Items[index];
#endif
                if (!byName.TryGetValue(slot.Data.Name, out var setting)) continue;
                if (!setting.IsVisible)
                {
                    slot.Attachment = null;
                    continue;
                }
                if (!string.IsNullOrWhiteSpace(setting.AttachmentName))
                {
                    var attachment = skeleton.GetAttachment(index, setting.AttachmentName);
                    if (attachment is not null) slot.Attachment = attachment;
                }
                slot.A *= Math.Clamp((float)setting.Opacity, 0, 1);
            }
        }

        public void Dispose()
        {
            asset?.Dispose();
            asset = null;
        }
    }

    private sealed class LoadedAsset : IDisposable
    {
        private LoadedAsset(RuntimeSpine.Atlas atlas, RuntimeSpine.SkeletonData data, IReadOnlyList<string> texturePaths)
        {
            Atlas = atlas;
            Data = data;
            TexturePaths = texturePaths;
        }

        public RuntimeSpine.Atlas Atlas { get; }
        public RuntimeSpine.SkeletonData Data { get; }
        public IReadOnlyList<string> TexturePaths { get; }

        public static LoadedAsset Open(string skeletonPath, string atlasPath, RuntimeSpine.TextureLoader textureLoader, IReadOnlyList<string> texturePaths)
        {
            var atlas = new RuntimeSpine.Atlas(atlasPath, textureLoader);
            var attachments = new RuntimeSpine.AtlasAttachmentLoader(atlas);
            RuntimeSpine.SkeletonData data;
            if (string.Equals(Path.GetExtension(skeletonPath), ".skel", StringComparison.OrdinalIgnoreCase))
            {
#if SPINE_RUNTIME_2108
                throw new NotSupportedException("Binary skeletons are not supported by the 2.1.08 vendored Runtime.");
#else
                data = new RuntimeSpine.SkeletonBinary(attachments).ReadSkeletonData(skeletonPath);
#endif
            }
            else
            {
                data = new RuntimeSpine.SkeletonJson(attachments).ReadSkeletonData(skeletonPath);
            }
            if (data.Version is null || !data.Version.StartsWith(RuntimeInfo.Prefix, StringComparison.Ordinal))
                throw new NotSupportedException($"Expected a {RuntimeInfo.Line} export, found '{data.Version ?? "unknown"}'.");
            return new LoadedAsset(atlas, data, texturePaths);
        }

        public void Dispose()
        {
            // Historical Atlas snapshots do not own disposable GPU resources.
        }
    }

    private sealed class MetadataTextureLoader : RuntimeSpine.TextureLoader
    {
        private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];
        private readonly List<string> paths = new();
        public IReadOnlyList<string> Paths => paths;

        public void Load(RuntimeSpine.AtlasPage page, string path)
        {
            var fullPath = Path.GetFullPath(path);
            var size = Path.GetExtension(fullPath).ToLowerInvariant() switch
            {
                ".png" => ReadPngSize(fullPath),
                ".ppm" => ReadPpmSize(fullPath),
                _ => throw new NotSupportedException($"Texture metadata format is not supported: {fullPath}")
            };
            page.rendererObject = fullPath;
            if (page.width <= 0 || page.height <= 0)
            {
                page.width = size.Width;
                page.height = size.Height;
            }
            paths.Add(fullPath);
        }

        public void Unload(object texture) { }

        private static (int Width, int Height) ReadPngSize(string path)
        {
            Span<byte> header = stackalloc byte[24];
            using var stream = File.OpenRead(path);
            stream.ReadExactly(header);
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

    private sealed class RenderTextureLoader : RuntimeSpine.TextureLoader
    {
        private readonly List<string> paths = new();
        public IReadOnlyList<string> Paths => paths;

        public void Load(RuntimeSpine.AtlasPage page, string path)
        {
            var fullPath = Path.GetFullPath(path);
            page.rendererObject = Path.GetExtension(fullPath).ToLowerInvariant() switch
            {
                ".png" => TextureData.LoadPng(fullPath),
                ".ppm" => TextureData.LoadP3(fullPath),
                _ => throw new NotSupportedException($"Render texture format is not supported: {fullPath}")
            };
            if (page.width <= 0 || page.height <= 0)
            {
                page.width = ((TextureData)page.rendererObject).Width;
                page.height = ((TextureData)page.rendererObject).Height;
            }
            paths.Add(fullPath);
        }

        public void Unload(object texture) { }
    }
}
