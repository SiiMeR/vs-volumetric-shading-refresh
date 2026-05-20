using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using VolumetricShadingRefreshed.VolumetricShading.Patch;

namespace VolumetricShadingRefreshed.VolumetricShading.Effects;

public class DepthOfField
{
    private readonly VolumetricShadingMod _mod;
    private readonly ClientPlatformWindows _platform;

    private FrameBufferRef _frameBuffer;
    private MeshRef _screenQuad;
    private ShaderProgram _shader;
    private float _smoothFocusDepth = 1.0f;
    private readonly float[] _depthPixel = new float[1];

    public DepthOfField(VolumetricShadingMod mod)
    {
        _mod = mod;
        _platform = mod.CApi.GetClientPlatformWindows();

        mod.ShaderInjector.RegisterBoolProperty("VSMOD_DOF_ENABLED", () => ModSettings.DepthOfFieldEnabled);
        mod.ShaderInjector.RegisterBoolProperty("VSMOD_DOF_AUTOFOCUS", () => ModSettings.DepthOfFieldAutoFocus);

        mod.Events.PostFinalRender += OnPostFinalRender;
        mod.Events.RebuildFramebuffers += SetupFramebuffers;
        mod.CApi.Event.ReloadShader += OnReloadShaders;

        SetupFramebuffers(_platform.FrameBuffers);
    }

    private bool OnReloadShaders()
    {
        var success = true;
        _shader?.Dispose();
        _shader = null;

        if (!ModSettings.DepthOfFieldEnabled)
            return success;

        _shader = (ShaderProgram)_mod.RegisterShader("dof", ref success);
        return success;
    }

    private void SetupFramebuffers(List<FrameBufferRef> mainBuffers)
    {
        if (_frameBuffer != null)
        {
            _platform.DisposeFrameBuffer(_frameBuffer);
            _frameBuffer = null;
        }

        if (!ModSettings.DepthOfFieldEnabled)
            return;

        var fbPrimary = mainBuffers[0];
        var fbWidth = fbPrimary.Width;
        var fbHeight = fbPrimary.Height;

        if (fbWidth == 0 || fbHeight == 0)
            return;

        var fb = new FrameBufferRef
        {
            FboId = GL.GenFramebuffer(),
            Width = fbWidth,
            Height = fbHeight,
            ColorTextureIds = ArrayUtil.CreateFilled(1, _ => GL.GenTexture())
        };
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, fb.FboId);
        fb.SetupColorTexture(0);
        Framebuffers.CheckStatus();

        _frameBuffer = fb;
        _screenQuad = _platform.GetScreenQuad();
    }

    private void OnPostFinalRender()
    {
        if (_frameBuffer == null || _shader == null)
            return;

        var fbPrimary = _platform.FrameBuffers[0];

        _platform.LoadFrameBuffer(_frameBuffer);
        GL.Viewport(0, 0, _frameBuffer.Width, _frameBuffer.Height);

        var s = _shader;
        s.Use();
        s.BindTexture2D("primaryScene", fbPrimary.ColorTextureIds[0], 0);
        s.BindTexture2D("depthTexture", fbPrimary.DepthTextureId, 1);
        s.UniformMatrix("invProjectionMatrix", _mod.Uniforms.InvProjectionMatrix);
        var input = _mod.CApi.Input;
        float focusU = GameMath.Clamp((float)input.MouseX / fbPrimary.Width, 0.01f, 0.99f);
        float focusV = GameMath.Clamp(1f - (float)input.MouseY / fbPrimary.Height, 0.01f, 0.99f);
        s.Uniform("dofFocusUV", new Vec2f(focusU, focusV));
        s.Uniform("dofStrength", ModSettings.DepthOfFieldStrength * 0.2f);
        s.Uniform("dofFocusRange", 1.0f + ModSettings.DepthOfFieldFocusRange * 1.5f);
        s.Uniform("dofAdaptiveRange", ModSettings.DepthOfFieldAdaptiveRange * 0.001f);
        if (ModSettings.DepthOfFieldAutoFocus)
        {
            int px = GameMath.Clamp((int)(focusU * fbPrimary.Width), 0, fbPrimary.Width - 1);
            int py = GameMath.Clamp((int)(focusV * fbPrimary.Height), 0, fbPrimary.Height - 1);
            GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, fbPrimary.FboId);
            GL.ReadPixels(px, py, 1, 1, PixelFormat.DepthComponent, PixelType.Float, _depthPixel);
            _smoothFocusDepth += (_depthPixel[0] - _smoothFocusDepth) * 0.08f;
            s.Uniform("dofSmoothDepth", _smoothFocusDepth);
        }
        else
        {
            s.Uniform("dofFocusDistance", 2.0f + ModSettings.DepthOfFieldFocusDistance * 1.26f);
        }
        _platform.RenderFullscreenTriangle(_screenQuad);
        s.Stop();

        _platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
        GL.Viewport(0, 0, fbPrimary.Width, fbPrimary.Height);
        GL.DrawBuffers(1, new[] { DrawBuffersEnum.ColorAttachment0 });

        var blit = ShaderPrograms.Blit;
        blit.Use();
        blit.Scene2D = _frameBuffer.ColorTextureIds[0];
        _platform.RenderFullscreenTriangle(_screenQuad);
        blit.Stop();

        GL.DrawBuffers(2, new[] { DrawBuffersEnum.ColorAttachment0, DrawBuffersEnum.ColorAttachment1 });
    }

    public void Dispose()
    {
        _shader?.Dispose();
        _shader = null;

        if (_frameBuffer != null)
        {
            _platform.DisposeFrameBuffer(_frameBuffer);
            _frameBuffer = null;
        }
    }
}
