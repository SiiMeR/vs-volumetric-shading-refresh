using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using volumetricshadingupdated.VolumetricShading.Patch;

namespace volumetricshadingupdated.VolumetricShading.Effects;

// Token: 0x0200003D RID: 61
public class DeferredLighting
{
    // Token: 0x040000B4 RID: 180
    private readonly VolumetricShadingMod _mod;

    // Token: 0x040000B5 RID: 181
    private readonly ClientPlatformWindows _platform;

    // Token: 0x040000B9 RID: 185
    private bool _enabled;

    // Token: 0x040000B7 RID: 183
    private FrameBufferRef _frameBuffer;

    // Token: 0x040000B8 RID: 184
    private MeshRef _screenQuad;

    // Token: 0x040000B6 RID: 182
    private ShaderProgram _shader;

    // Token: 0x060001AD RID: 429 RVA: 0x000071F8 File Offset: 0x000053F8
    public DeferredLighting(VolumetricShadingMod mod)
    {
        _mod = mod;
        _platform = _mod.CApi.GetClientPlatformWindows();
        _mod.CApi.Settings.AddWatcher("volumetricshading_deferredLighting",
            new OnSettingsChanged<bool>(OnDeferredLightingChanged));
        _mod.CApi.Settings.AddWatcher("ssaoQuality",
            new OnSettingsChanged<int>(OnSSAOQualityChanged));
        _enabled = ModSettings.DeferredLightingEnabled;
        _mod.CApi.Event.RegisterRenderer(new DeferredLightingPreparer(this), EnumRenderStage.Opaque,
            "vsmod-deferred-lighting-prepare");
        _mod.CApi.Event.RegisterRenderer(new DeferredLightingRenderer(this), EnumRenderStage.Opaque,
            "vsmod-deferred-lighting");
        _mod.ShaderInjector.RegisterBoolProperty("VSMOD_DEFERREDLIGHTING", () => _enabled);
        _mod.CApi.Event.ReloadShader += OnReloadShaders;
        _mod.Events.RebuildFramebuffers += SetupFramebuffers;

        SetupFramebuffers(_platform.FrameBuffers);
    }

    // Token: 0x060001AE RID: 430 RVA: 0x000035CB File Offset: 0x000017CB
    private void OnDeferredLightingChanged(bool enabled)
    {
        _enabled = enabled;
        if (enabled && ClientSettings.SSAOQuality == 0)
        {
            ClientSettings.SSAOQuality = 1;
        }
    }

    // Token: 0x060001AF RID: 431 RVA: 0x000035E4 File Offset: 0x000017E4
    private void OnSSAOQualityChanged(int quality)
    {
        if (quality == 0 && _enabled)
        {
            ModSettings.DeferredLightingEnabled = false;
            _platform.RebuildFrameBuffers();
            _mod.CApi.Shader.ReloadShaders();
        }
    }

    // Token: 0x060001B0 RID: 432 RVA: 0x00007330 File Offset: 0x00005530
    private bool OnReloadShaders()
    {
        var success = true;
        var shader = _shader;
        if (shader != null)
        {
            shader.Dispose();
        }

        _shader = (ShaderProgram)_mod.RegisterShader("deferredlighting", ref success);
        return success;
    }

    private void SetupFramebuffers(List<FrameBufferRef> mainBuffers)
    {
        if (_frameBuffer != null)
        {
            _platform.DisposeFrameBuffer(_frameBuffer);
            _frameBuffer = null;
        }

        if (ClientSettings.SSAOQuality <= 0 || !_enabled)
        {
            return;
        }

        var fbPrimary = mainBuffers[0];

        var fbWidth = (int)(_platform.window.Bounds.Size.X * ClientSettings.SSAA);
        var fbHeight = (int)(_platform.window.Bounds.Size.Y * ClientSettings.SSAA);
        if (fbWidth == 0 || fbHeight == 0)
        {
            return;
        }

        var frameBufferRef = new FrameBufferRef
        {
            FboId = GL.GenFramebuffer(),
            Width = fbWidth,
            Height = fbHeight,
            ColorTextureIds = ArrayUtil.CreateFilled(2, _ => GL.GenTexture())
        };
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, frameBufferRef.FboId);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            TextureTarget.Texture2D, fbPrimary.DepthTextureId, 0);
        frameBufferRef.SetupColorTexture(0);
        frameBufferRef.SetupColorTexture(1);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2,
            TextureTarget.Texture2D, fbPrimary.ColorTextureIds[2], 0);
        GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment3,
            TextureTarget.Texture2D, fbPrimary.ColorTextureIds[3], 0);
        GL.DrawBuffers(4, new[]
        {
            DrawBuffersEnum.ColorAttachment0,
            DrawBuffersEnum.ColorAttachment1,
            DrawBuffersEnum.ColorAttachment2,
            DrawBuffersEnum.ColorAttachment3
        });
        Framebuffers.CheckStatus();
        _frameBuffer = frameBufferRef;
        _screenQuad = _platform.GetScreenQuad();
    }

    // Token: 0x060001B2 RID: 434 RVA: 0x00003618 File Offset: 0x00001818
    public void OnBeginRender()
    {
        if (_frameBuffer == null)
        {
            return;
        }

        _platform.LoadFrameBuffer(_frameBuffer);
        GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
    }

    // Token: 0x060001B3 RID: 435 RVA: 0x00007500 File Offset: 0x00005700
    public void OnEndRender()
    {
        if (_frameBuffer == null)
        {
            return;
        }

        _platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
        GL.ClearBuffer(ClearBuffer.Color, 0, new[] { 0f, 0f, 0f, 1f });
        GL.ClearBuffer(ClearBuffer.Color, 1, new[] { 0f, 0f, 0f, 1f });
        var render = _mod.CApi.Render;
        var uniforms = render.ShaderUniforms;
        var myUniforms = _mod.Uniforms;
        var fb = _frameBuffer;
        var fbPrimary = _platform.FrameBuffers[0];
        _platform.GlDisableDepthTest();
        _platform.GlToggleBlend(false);
        GL.DrawBuffers(2, new[]
        {
            DrawBuffersEnum.ColorAttachment0,
            DrawBuffersEnum.ColorAttachment1
        });
        var s = _shader;
        s.Use();
        s.BindTexture2D("gDepth", fbPrimary.DepthTextureId);
        s.BindTexture2D("gNormal", fbPrimary.ColorTextureIds[2]);
        s.BindTexture2D("inColor", fb.ColorTextureIds[0]);
        s.BindTexture2D("inGlow", fb.ColorTextureIds[1]);
        s.UniformMatrix("invProjectionMatrix", myUniforms.InvProjectionMatrix);
        s.UniformMatrix("invModelViewMatrix", myUniforms.InvModelViewMatrix);
        s.Uniform("dayLight", myUniforms.DayLight);
        s.Uniform("sunPosition", uniforms.SunPosition3D);
        if (ShaderProgramBase.shadowmapQuality > 0)
        {
            s.Uniform("shadowRangeFar", uniforms.ShadowRangeFar);
            s.Uniform("shadowRangeNear", uniforms.ShadowRangeNear);
            s.UniformMatrix("toShadowMapSpaceMatrixFar", uniforms.ToShadowMapSpaceMatrixFar);
            s.UniformMatrix("toShadowMapSpaceMatrixNear", uniforms.ToShadowMapSpaceMatrixNear);
        }

        s.Uniform("fogDensityIn", render.FogDensity);
        s.Uniform("fogMinIn", render.FogMin);
        s.Uniform("rgbaFog", render.FogColor);
        s.Uniform("flatFogDensity", uniforms.FlagFogDensity);
        s.Uniform("flatFogStart", uniforms.FlatFogStartYPos - uniforms.PlayerPos.Y);
        s.Uniform("viewDistance", ClientSettings.ViewDistance);
        s.Uniform("viewDistanceLod0", ClientSettings.ViewDistance * ClientSettings.LodBias);
        _platform.RenderFullscreenTriangle(_screenQuad);
        s.Stop();
        _platform.CheckGlError("Error while calculating deferred lighting");
        GL.DrawBuffers(4, new[]
        {
            DrawBuffersEnum.ColorAttachment0,
            DrawBuffersEnum.ColorAttachment1,
            DrawBuffersEnum.ColorAttachment2,
            DrawBuffersEnum.ColorAttachment3
        });
        _platform.GlEnableDepthTest();
    }

    // Token: 0x060001B4 RID: 436 RVA: 0x0000363E File Offset: 0x0000183E
    public void Dispose()
    {
        var shader = _shader;
        if (shader != null)
        {
            shader.Dispose();
        }

        _shader = null;
        if (_frameBuffer != null)
        {
            _platform.DisposeFrameBuffer(_frameBuffer);
            _frameBuffer = null;
        }
    }
}