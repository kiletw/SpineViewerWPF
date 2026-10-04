using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Wpf;
using SpineViewerWPF.Core;

namespace SpineViewerWPF.Wpf;

public sealed class GpuViewport : Grid, IDisposable
{
    private const string VertexShaderSource =
        """
        #version 330 core
        layout (location = 0) in vec2 aPosition;
        layout (location = 1) in vec2 aUv;
        uniform vec2 uCenter;
        uniform float uFitScale;
        uniform vec2 uViewport;
        uniform vec2 uLayerScale;
        uniform float uRotation;
        uniform vec2 uTranslation;
        uniform float uViewportZoom;
        uniform vec2 uViewportPan;
        out vec2 vUv;

        void main()
        {
            vec2 p = (aPosition - uCenter) * uFitScale;
            p *= uLayerScale;
            float cosine = cos(-uRotation);
            float sine = sin(-uRotation);
            p = vec2(p.x * cosine - p.y * sine, p.x * sine + p.y * cosine);
            p = p * uViewportZoom + uTranslation + uViewportPan;
            gl_Position = vec4(p.x * 2.0 / uViewport.x, p.y * 2.0 / uViewport.y, 0.0, 1.0);
            vUv = aUv;
        }
        """;

    private const string FragmentShaderSource =
        """
        #version 330 core
        in vec2 vUv;
        uniform sampler2D uTexture;
        uniform vec4 uTint;
        uniform int uPma;
        out vec4 color;

        void main()
        {
            vec4 sampled = texture(uTexture, vUv);
            float alpha = sampled.a * uTint.a;
            if (uPma == 1)
                color = vec4(sampled.rgb * uTint.rgb * uTint.a, alpha);
            else
                color = vec4(sampled.rgb * sampled.a * uTint.rgb * uTint.a, alpha);
        }
        """;

    private readonly GLWpfControl control = new()
    {
        Focusable = false,
        IsHitTestVisible = false
    };
    // Keyed by texture instance, not path: a reloaded session yields new pixel
    // data under the same path, and layers sharing an atlas file each own theirs.
    private readonly Dictionary<PreviewTexture, int> textures = new(ReferenceEqualityComparer.Instance);
    private readonly HashSet<PreviewTexture> activeTextureKeys = new(ReferenceEqualityComparer.Instance);
    private bool started;
    private bool initialized;
    private bool frameValidated;
    private bool failed;
    private bool disposed;
    private int program;
    private int vertexArray;
    private int vertexBuffer;
    private int indexBuffer;
    private int layerFramebuffer;
    private int layerTexture;
    private int layerTargetWidth;
    private int layerTargetHeight;
    private ShellViewModel? subscribedViewModel;

    public GpuViewport()
    {
        IsHitTestVisible = false;
        ClipToBounds = true;
        Children.Add(control);
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
        SizeChanged += (_, _) => RequestRender();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (started || disposed) return;
        try
        {
            AttachViewModel();
            control.Render += Render;
            control.Start(new GLWpfControlSettings
            {
                MajorVersion = 3,
                MinorVersion = 3,
                RenderContinuously = true,
                TransparentBackground = true,
                UseDeviceDpi = true
            });
            control.RenderContinuously = false;
            started = true;
            subscribedViewModel?.SetGpuPreviewAvailable(true);
            RequestRender();
        }
        catch (Exception exception)
        {
            Fail(exception);
        }
    }

    private void Render(TimeSpan _)
    {
        if (failed || disposed) return;
        try
        {
            if (!initialized) Initialize();
            var width = control.FrameBufferWidth;
            var height = control.FrameBufferHeight;
            if (width < 1 || height < 1) return;

            GL.Viewport(0, 0, width, height);
            GL.ClearColor(0, 0, 0, 0);
            GL.Clear(ClearBufferMask.ColorBufferBit);

            var viewModel = ViewModel;
            if (viewModel is null || !viewModel.UseGpuPreview) return;

            activeTextureKeys.Clear();
            GL.UseProgram(program);
            GL.BindVertexArray(vertexArray);
            GL.Uniform2(GL.GetUniformLocation(program, "uViewport"), (float)width, (float)height);
            GL.Uniform1(GL.GetUniformLocation(program, "uViewportZoom"), (float)viewModel.ViewportZoom);
            GL.Uniform2(
                GL.GetUniformLocation(program, "uViewportPan"),
                (float)(viewModel.ViewportPanX * DpiScaleX),
                (float)(-viewModel.ViewportPanY * DpiScaleY));
            var hasVisibleGeometry = false;
            GL.GetInteger(GetPName.DrawFramebufferBinding, out int targetFramebuffer);
            foreach (var layer in viewModel.SceneLayers.OrderBy(item => item.ZIndex))
            {
                if (!layer.IsVisible || layer.PreviewScene is not { } scene) continue;
                hasVisibleGeometry |= scene.DrawCommands.Any(command => command.Alpha > 0);
                if (layer.Opacity >= 0.999)
                {
                    DrawLayer(scene, layer, width, height, 1);
                    continue;
                }
                if (layer.Opacity <= 0) continue;
                // Layer opacity fades the composed layer, matching CPU capture and
                // export: overlapping slots inside the layer must not add alpha.
                DrawLayerWithOpacity(scene, layer, width, height, targetFramebuffer, viewModel);
            }
            if (!frameValidated && hasVisibleGeometry)
                ValidateFrame(width, height);
            PruneTextures();
            GL.BindVertexArray(0);
            GL.UseProgram(0);
            var error = GL.GetError();
            if (error != ErrorCode.NoError)
                throw new InvalidOperationException($"OpenGL preview failed: {error}.");
        }
        catch (Exception exception)
        {
            Fail(exception);
        }
    }

    private void Initialize()
    {
        program = CreateProgram(VertexShaderSource, FragmentShaderSource);
        vertexArray = GL.GenVertexArray();
        vertexBuffer = GL.GenBuffer();
        indexBuffer = GL.GenBuffer();

        GL.BindVertexArray(vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 16, 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 16, 8);
        GL.BindVertexArray(0);
        GL.Enable(EnableCap.Blend);
        GL.Disable(EnableCap.DepthTest);
        GL.Disable(EnableCap.CullFace);
        initialized = true;
    }

    private void DrawLayerWithOpacity(
        PreviewSceneFrame scene,
        SceneLayerViewModel layer,
        int width,
        int height,
        int targetFramebuffer,
        ShellViewModel viewModel)
    {
        EnsureLayerTarget(width, height);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, layerFramebuffer);
        GL.Viewport(0, 0, width, height);
        GL.ClearColor(0, 0, 0, 0);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        DrawLayer(scene, layer, width, height, 1);

        GL.BindFramebuffer(FramebufferTarget.Framebuffer, targetFramebuffer);
        GL.Viewport(0, 0, width, height);
        // The layer target already holds the transformed, premultiplied layer; draw
        // it back as a screen-aligned quad with identity transforms.
        GL.Uniform2(GL.GetUniformLocation(program, "uCenter"), 0f, 0f);
        GL.Uniform1(GL.GetUniformLocation(program, "uFitScale"), 1f);
        GL.Uniform2(GL.GetUniformLocation(program, "uLayerScale"), 1f, 1f);
        GL.Uniform1(GL.GetUniformLocation(program, "uRotation"), 0f);
        GL.Uniform2(GL.GetUniformLocation(program, "uTranslation"), 0f, 0f);
        GL.Uniform1(GL.GetUniformLocation(program, "uViewportZoom"), 1f);
        GL.Uniform2(GL.GetUniformLocation(program, "uViewportPan"), 0f, 0f);
        var halfWidth = width / 2f;
        var halfHeight = height / 2f;
        float[] quad =
        [
            -halfWidth, -halfHeight, 0, 0,
            halfWidth, -halfHeight, 1, 0,
            halfWidth, halfHeight, 1, 1,
            -halfWidth, halfHeight, 0, 1
        ];
        int[] indices = [0, 1, 2, 0, 2, 3];
        GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, quad.Length * sizeof(float), quad, BufferUsageHint.DynamicDraw);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indices.Length * sizeof(int), indices, BufferUsageHint.DynamicDraw);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.BindTexture(TextureTarget.Texture2D, layerTexture);
        GL.Uniform1(GL.GetUniformLocation(program, "uTexture"), 0);
        GL.Uniform4(GL.GetUniformLocation(program, "uTint"), 1f, 1f, 1f, (float)layer.Opacity);
        GL.Uniform1(GL.GetUniformLocation(program, "uPma"), 1);
        SetBlend(PreviewBlendMode.Normal);
        GL.DrawElements(PrimitiveType.Triangles, indices.Length, DrawElementsType.UnsignedInt, 0);

        GL.Uniform1(GL.GetUniformLocation(program, "uViewportZoom"), (float)viewModel.ViewportZoom);
        GL.Uniform2(
            GL.GetUniformLocation(program, "uViewportPan"),
            (float)(viewModel.ViewportPanX * DpiScaleX),
            (float)(-viewModel.ViewportPanY * DpiScaleY));
    }

    private void EnsureLayerTarget(int width, int height)
    {
        if (layerFramebuffer != 0 && layerTargetWidth == width && layerTargetHeight == height) return;
        if (layerTexture == 0) layerTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, layerTexture);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba8, width, height, 0,
            OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        if (layerFramebuffer == 0) layerFramebuffer = GL.GenFramebuffer();
        GL.GetInteger(GetPName.DrawFramebufferBinding, out int previous);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, layerFramebuffer);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, layerTexture, 0);
        var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, previous);
        if (status != FramebufferErrorCode.FramebufferComplete)
            throw new InvalidOperationException($"OpenGL layer target is incomplete: {status}.");
        layerTargetWidth = width;
        layerTargetHeight = height;
    }

    private void DrawLayer(PreviewSceneFrame scene, SceneLayerViewModel layer, int width, int height, float opacity)
    {
        var (centerX, centerY, fitScale) = ViewportMath.ComputeFit(scene, width, height);

        GL.Uniform2(GL.GetUniformLocation(program, "uCenter"), centerX, centerY);
        GL.Uniform1(GL.GetUniformLocation(program, "uFitScale"), fitScale);
        GL.Uniform2(
            GL.GetUniformLocation(program, "uLayerScale"),
            (float)(layer.ModelScale * (layer.FlipX ? -1 : 1)),
            (float)(layer.ModelScale * (layer.FlipY ? -1 : 1)));
        GL.Uniform1(
            GL.GetUniformLocation(program, "uRotation"),
            MathHelper.DegreesToRadians((float)layer.ModelRotation));
        GL.Uniform2(
            GL.GetUniformLocation(program, "uTranslation"),
            (float)(layer.ModelX * DpiScaleX),
            (float)(-layer.ModelY * DpiScaleY));
        foreach (var command in scene.DrawCommands)
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, vertexBuffer);
            GL.BufferData(
                BufferTarget.ArrayBuffer,
                command.Vertices.Length * 16,
                command.Vertices,
                BufferUsageHint.DynamicDraw);
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, indexBuffer);
            GL.BufferData(
                BufferTarget.ElementArrayBuffer,
                command.Indices.Length * sizeof(int),
                command.Indices,
                BufferUsageHint.DynamicDraw);

            var texture = GetTexture(command.Texture);
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.Uniform1(GL.GetUniformLocation(program, "uTexture"), 0);
            GL.Uniform4(
                GL.GetUniformLocation(program, "uTint"),
                command.Red,
                command.Green,
                command.Blue,
                command.Alpha * opacity);
            GL.Uniform1(GL.GetUniformLocation(program, "uPma"), command.Pma ? 1 : 0);
            SetBlend(command.BlendMode);
            GL.DrawElements(
                PrimitiveType.Triangles,
                command.Indices.Length,
                DrawElementsType.UnsignedInt,
                0);
        }
    }

    private int GetTexture(PreviewTexture texture)
    {
        activeTextureKeys.Add(texture);
        if (textures.TryGetValue(texture, out var existing)) return existing;

        var handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, handle);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
        GL.TexImage2D(
            TextureTarget.Texture2D,
            0,
            PixelInternalFormat.Rgba8,
            texture.Width,
            texture.Height,
            0,
            OpenTK.Graphics.OpenGL4.PixelFormat.Rgba,
            PixelType.UnsignedByte,
            texture.Rgba32);
        textures.Add(texture, handle);
        return handle;
    }

    private void ValidateFrame(int width, int height)
    {
        var pixels = new byte[width * height * 4];
        GL.ReadPixels(0, 0, width, height, OpenTK.Graphics.OpenGL4.PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
        if (!Enumerable.Range(0, width * height).Any(index => pixels[index * 4 + 3] != 0))
            throw new InvalidOperationException("OpenGL rendered an empty frame.");
        frameValidated = true;
    }

    private void PruneTextures()
    {
        if (textures.Count == activeTextureKeys.Count) return;
        foreach (var stale in textures.Keys.Where(key => !activeTextureKeys.Contains(key)).ToArray())
        {
            GL.DeleteTexture(textures[stale]);
            textures.Remove(stale);
        }
    }

    private static void SetBlend(PreviewBlendMode mode)
    {
        var (source, destination) = ColorBlendFactors(mode);
        GL.BlendFuncSeparate(
            source,
            destination,
            BlendingFactorSrc.One,
            mode == PreviewBlendMode.Additive ? BlendingFactorDest.One : BlendingFactorDest.OneMinusSrcAlpha);
    }

    private static (BlendingFactorSrc Source, BlendingFactorDest Destination) ColorBlendFactors(PreviewBlendMode mode) =>
        mode switch
        {
            PreviewBlendMode.Additive => (BlendingFactorSrc.One, BlendingFactorDest.One),
            PreviewBlendMode.Multiply => (BlendingFactorSrc.DstColor, BlendingFactorDest.OneMinusSrcAlpha),
            PreviewBlendMode.Screen => (BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcColor),
            _ => (BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha)
        };

    private static int CreateProgram(string vertexSource, string fragmentSource)
    {
        var vertex = CompileShader(ShaderType.VertexShader, vertexSource);
        var fragment = CompileShader(ShaderType.FragmentShader, fragmentSource);
        var result = GL.CreateProgram();
        GL.AttachShader(result, vertex);
        GL.AttachShader(result, fragment);
        GL.LinkProgram(result);
        GL.GetProgram(result, GetProgramParameterName.LinkStatus, out var linked);
        var log = GL.GetProgramInfoLog(result);
        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);
        if (linked == 0)
        {
            GL.DeleteProgram(result);
            throw new InvalidOperationException($"GPU shader link failed: {log}");
        }
        return result;
    }

    private static int CompileShader(ShaderType type, string source)
    {
        var shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);
        GL.GetShader(shader, ShaderParameter.CompileStatus, out var compiled);
        if (compiled != 0) return shader;
        var log = GL.GetShaderInfoLog(shader);
        GL.DeleteShader(shader);
        throw new InvalidOperationException($"GPU shader compile failed: {log}");
    }

    // TASK-065: content center of a layer as drawn by the last GPU frame layout,
    // relative to the viewport center and excluding pan.
    public bool TryGetLayerContentCenter(SceneLayerViewModel layer, out double centerX, out double centerY)
    {
        centerX = centerY = 0;
        var viewModel = ViewModel;
        if (!started || failed || disposed || viewModel is null || !viewModel.HasGpuPreview
            || layer.PreviewScene is not { } scene)
            return false;
        return ViewportMath.TryGetLayerContentCenter(
            scene,
            layer.ModelX,
            layer.ModelY,
            layer.ModelScale,
            layer.ModelRotation,
            layer.FlipX,
            layer.FlipY,
            control.FrameBufferWidth,
            control.FrameBufferHeight,
            viewModel.ViewportZoom,
            DpiScaleX,
            DpiScaleY,
            out centerX,
            out centerY);
    }

    private ShellViewModel? ViewModel => DataContext as ShellViewModel;
    private double DpiScaleX => VisualTreeHelper.GetDpi(this).DpiScaleX;
    private double DpiScaleY => VisualTreeHelper.GetDpi(this).DpiScaleY;

    private void RequestRender()
    {
        if (disposed || !IsLoaded) return;
        Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() =>
        {
            if (!disposed)
                control.InvalidateVisual();
        }));
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (disposed) return;
        AttachViewModel();
        RequestRender();
    }

    private void AttachViewModel()
    {
        if (ReferenceEquals(subscribedViewModel, ViewModel)) return;
        if (subscribedViewModel is not null)
            subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        subscribedViewModel = ViewModel;
        if (subscribedViewModel is not null)
        {
            subscribedViewModel.PropertyChanged += OnViewModelPropertyChanged;
            if (started && !failed)
                subscribedViewModel.SetGpuPreviewAvailable(true);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ShellViewModel.Position)
            or nameof(ShellViewModel.PlaybackTimeLabel)
            or nameof(ShellViewModel.PreviewPerformanceLabel))
            return;
        RequestRender();
    }

    private void Fail(Exception exception)
    {
        if (failed) return;
        failed = true;
        if (started) control.RenderContinuously = false;
        ViewModel?.SetGpuPreviewAvailable(false, exception.Message);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e) => Dispose();

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (subscribedViewModel is not null)
            subscribedViewModel.PropertyChanged -= OnViewModelPropertyChanged;
        subscribedViewModel = null;
        control.Render -= Render;
        control.Dispose();
        textures.Clear();
    }
}
