using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading.Effects;

public class DepthOfField : IRenderer
{
    private readonly VolumetricShadingMod _mod;
    private readonly ClientPlatformWindows _platform;
    private readonly MeshRef _screenQuad;
    private FrameBufferRef _frameBuffer;
    private IShaderProgram _shader;

    public DepthOfField(VolumetricShadingMod mod)
    {
        _mod = mod;
        _platform = _mod.CApi.GetClientPlatformWindows();
        _screenQuad = _platform.GetScreenQuad();

        _mod.CApi.Event.ReloadShader += ReloadShaders;
        _mod.Events.RebuildFramebuffers += SetupFramebuffers;
        SetupFramebuffers(_platform.FrameBuffers);


        mod.CApi.Event.RegisterRenderer(this, EnumRenderStage.OIT, "dofBlur");
    }

    public void Dispose()
    {
        _shader?.Dispose();
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        if (!ModSettings.DepthOfFieldEnabled)
        {
            return;
        }

        var sceneTexId = _platform.FrameBuffers[0].ColorTextureIds[0];


        _platform.LoadFrameBuffer(_frameBuffer);
        GL.ClearColor(0, 0, 0, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        var shader = _shader;
        shader.Use();
        shader.BindTexture2D("uScene", sceneTexId, 0);
        shader.BindTexture2D("uDepth", _platform.FrameBuffers[0].DepthTextureId, 1);
        shader.Uniform("uFocusDepth", ModSettings.DofFocusDepth / 100f);
        shader.Uniform("uBlurRange", ModSettings.DofBlurRange / 100f);

        _platform.RenderFullscreenTriangle(_screenQuad);
        shader.Stop();
        _platform.UnloadFrameBuffer(_frameBuffer);
    }

    public double RenderOrder { get; } = 2.0d;
    public int RenderRange { get; } = int.MaxValue;


    private void SetupFramebuffers(List<FrameBufferRef> mainBuffers)
    {
        if (_frameBuffer != null)
        {
            _platform.DisposeFrameBuffer(_frameBuffer);
            _frameBuffer = null;
        }

        var fbPrimary = mainBuffers[0];

        var fbWidth = (int)(_platform.window.Bounds.Size.X * ClientSettings.SSAA);
        var fbHeight = (int)(_platform.window.Bounds.Size.Y * ClientSettings.SSAA);
        if (fbWidth == 0 || fbHeight == 0)
        {
            return;
        }

        _frameBuffer = new FrameBufferRef
        {
            FboId = GL.GenFramebuffer(),
            Width = fbWidth,
            Height = fbWidth,
            DepthTextureId = GL.GenTexture(),
            ColorTextureIds = new[] { GL.GenTexture() }
        };
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, _frameBuffer.FboId);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D, fbPrimary.DepthTextureId, 0);
        _frameBuffer.SetupColorTexture(0);
    }

    private bool ReloadShaders()
    {
        var success = true;

        _shader = _mod.RegisterShader("dof", ref success);

        return success;
    }
}