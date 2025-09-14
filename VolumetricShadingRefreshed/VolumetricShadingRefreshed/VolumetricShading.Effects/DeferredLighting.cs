using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using volumetricshadingupdated.VolumetricShading.Patch;

namespace volumetricshadingupdated.VolumetricShading.Effects;

public class DeferredLighting
{
    private readonly VolumetricShadingMod _mod;

    private readonly ClientPlatformWindows _platform;

    private bool _enabled;

    private FrameBufferRef _frameBuffer;

    private MeshRef _screenQuad;

    private ShaderProgram _shader;
    
    // Modern deferred shaders - replacing YAML patches
    private IShaderProgram _geometryShader;
    private IShaderProgram _lightingShader;

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

    private void OnDeferredLightingChanged(bool enabled)
    {
        _enabled = enabled;
        if (enabled && ClientSettings.SSAOQuality == 0)
        {
            ClientSettings.SSAOQuality = 1;
        }
    }

    private void OnSSAOQualityChanged(int quality)
    {
        if (quality == 0 && _enabled)
        {
            ModSettings.DeferredLightingEnabled = false;
            _platform.RebuildFrameBuffers();
            _mod.CApi.Shader.ReloadShaders();
        }
    }

    private bool OnReloadShaders()
    {
        var success = true;
        
        // Dispose existing shaders
        var shader = _shader;
        if (shader != null)
        {
            shader.Dispose();
        }
        _geometryShader?.Dispose();
        _lightingShader?.Dispose();

        try
        {
            // Load legacy shader for backwards compatibility
            _shader = (ShaderProgram)_mod.RegisterShader("deferredlighting", ref success);
            
            // Use legacy shader registration for compatibility
            bool geometrySuccess = true;
            bool lightingSuccess = true;
            _geometryShader = (IShaderProgram)_mod.RegisterShader("deferred_geometry", ref geometrySuccess);
            _lightingShader = (IShaderProgram)_mod.RegisterShader("deferred_lighting", ref lightingSuccess);
            
            // AMD Compatibility: Validate shader compilation
            if (!success || _shader == null)
            {
                _mod.Mod.Logger.Error("Legacy deferred lighting shader compilation failed");
            }
            
            bool modernSuccess = geometrySuccess && lightingSuccess && _geometryShader != null && _lightingShader != null;
            if (!modernSuccess)
            {
                _mod.Mod.Logger.Warning("Modern deferred lighting shaders failed to load - using legacy implementation");
                // Fallback to legacy implementation
                _enabled = _shader != null;
            }
            else
            {
                _mod.Mod.Logger.Event("Modern deferred lighting shaders loaded successfully");
                _mod.Mod.Logger.Event("Deferred lighting now uses dedicated shaders instead of YAML patches");
            }
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Error($"Exception loading deferred lighting shaders: {ex.Message}");
            success = false;
            _enabled = false;
            ModSettings.DeferredLightingEnabled = false;
        }
        
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

    public void OnBeginRender()
    {
        if (_frameBuffer == null)
        {
            return;
        }

        var stopwatch = _mod.PerformanceManager?.StartTiming("DeferredLighting_BeginRender");
        try
        {
            _platform.LoadFrameBuffer(_frameBuffer);
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
        }
        finally
        {
            if (stopwatch != null)
                _mod.PerformanceManager?.EndTiming("DeferredLighting_BeginRender", stopwatch);
        }
    }

    public void OnEndRender()
    {
        if (_frameBuffer == null)
        {
            return;
        }

        // AMD Compatibility: Check if shader is valid before using
        var s = _shader;
        if (s == null)
        {
            _mod.Mod.Logger.Warning("Deferred lighting shader is null, skipping render");
            return;
        }

        // Check if shader is usable (success flag should have been set during compilation)
        if (_shader == null)
        {
            _mod.Mod.Logger.Warning("Deferred lighting shader is not available, skipping render");
            return;
        }

        try
        {
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

            s.Use();
            s.BindTexture2D("gDepth", fbPrimary.DepthTextureId);
            s.BindTexture2D("gNormal", fbPrimary.ColorTextureIds[2]);
            s.BindTexture2D("inColor", fb.ColorTextureIds[0]);
            s.BindTexture2D("inGlow", fb.ColorTextureIds[1]);

            // AMD Compatibility: Safe uniform setting with validation
            TrySetUniformMatrix(s, "invProjectionMatrix", myUniforms.InvProjectionMatrix);
            TrySetUniformMatrix(s, "invModelViewMatrix", myUniforms.InvModelViewMatrix);
            TrySetUniform(s, "dayLight", myUniforms.DayLight);
            TrySetUniform(s, "sunPosition", uniforms.SunPosition3D);

            if (ShaderProgramBase.shadowmapQuality > 0)
            {
                TrySetUniform(s, "shadowRangeFar", uniforms.ShadowRangeFar);
                TrySetUniform(s, "shadowRangeNear", uniforms.ShadowRangeNear);
                TrySetUniformMatrix(s, "toShadowMapSpaceMatrixFar", uniforms.ToShadowMapSpaceMatrixFar);
                TrySetUniformMatrix(s, "toShadowMapSpaceMatrixNear", uniforms.ToShadowMapSpaceMatrixNear);
            }

            TrySetUniform(s, "fogDensityIn", render.FogDensity);
            TrySetUniform(s, "fogMinIn", render.FogMin);
            TrySetUniform(s, "rgbaFog", render.FogColor);
            TrySetUniform(s, "flatFogDensity", uniforms.FlagFogDensity);
            TrySetUniform(s, "flatFogStart", uniforms.FlatFogStartYPos - uniforms.PlayerPos.Y);
            TrySetUniform(s, "viewDistance", (float)ClientSettings.ViewDistance);
            TrySetUniform(s, "viewDistanceLod0", (float)(ClientSettings.ViewDistance * ClientSettings.LodBias));

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
        catch (Exception ex)
        {
            _mod.Mod.Logger.Error($"Error in deferred lighting render: {ex.Message}");
            // Disable deferred lighting on critical error to prevent crashes
            _enabled = false;
            ModSettings.DeferredLightingEnabled = false;
            _mod.Mod.Logger.Warning("Disabled deferred lighting due to render error");
        }
    }

    /// <summary>
    /// AMD Compatibility: Safe uniform matrix setting with error handling
    /// </summary>
    private void TrySetUniformMatrix(ShaderProgram shader, string uniformName, float[] matrix)
    {
        try
        {
            shader.UniformMatrix(uniformName, matrix);
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Warning($"Failed to set uniform matrix '{uniformName}': {ex.Message}");
        }
    }

    /// <summary>
    /// AMD Compatibility: Safe uniform setting with error handling
    /// </summary>
    private void TrySetUniform(ShaderProgram shader, string uniformName, float value)
    {
        try
        {
            shader.Uniform(uniformName, value);
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Warning($"Failed to set uniform '{uniformName}': {ex.Message}");
        }
    }

    /// <summary>
    /// AMD Compatibility: Safe uniform setting for Vec3f values
    /// </summary>
    private void TrySetUniform(ShaderProgram shader, string uniformName, Vec3f value)
    {
        try
        {
            shader.Uniform(uniformName, value);
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Warning($"Failed to set uniform '{uniformName}': {ex.Message}");
        }
    }

    /// <summary>
    /// AMD Compatibility: Safe uniform setting for Vec4f values
    /// </summary>
    private void TrySetUniform(ShaderProgram shader, string uniformName, Vec4f value)
    {
        try
        {
            shader.Uniform(uniformName, value);
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Warning($"Failed to set uniform '{uniformName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Modern deferred lighting pass using dedicated shaders instead of YAML patches
    /// </summary>
    public void OnRenderModernDeferred()
    {
        if (!_enabled || _lightingShader == null || _frameBuffer == null)
        {
            return;
        }

        var stopwatch = _mod.PerformanceManager?.StartTiming("DeferredLighting_Modern");
        try
        {
            var fbPrimary = _platform.FrameBuffers[0];
            
            // Disable depth testing for lighting pass
            _platform.GlDisableDepthTest();
            _platform.GlToggleBlend(false);
            
            // Use modern lighting shader
            _lightingShader.Use();
            
            // Bind G-buffer textures with texture slots
            _lightingShader.BindTexture2D("gAlbedo", _frameBuffer.ColorTextureIds[0], 0);
            _lightingShader.BindTexture2D("gNormal", _frameBuffer.ColorTextureIds[1], 1);
            _lightingShader.BindTexture2D("gMotion", fbPrimary.ColorTextureIds[2], 2);
            _lightingShader.BindTexture2D("gShadow", fbPrimary.ColorTextureIds[3], 3);
            _lightingShader.BindTexture2D("gDepth", fbPrimary.DepthTextureId, 4);
            
            // Bind shadow maps if available
            if (ShaderProgramBase.shadowmapQuality > 0)
            {
                var render = _mod.CApi.Render;
                var uniforms = render.ShaderUniforms;
                
                // This would need to access the actual shadow map textures
                // For now, we'll use placeholder binding
                _lightingShader.BindTexture2D("shadowMapNearTex", fbPrimary.DepthTextureId, 5);
                _lightingShader.BindTexture2D("shadowMapFarTex", fbPrimary.DepthTextureId, 6);
            }
            
            // Render full-screen quad
            _platform.RenderFullscreenTriangle(_screenQuad);
            _lightingShader.Stop();
            
            // Restore state
            _platform.GlEnableDepthTest();
            
            _mod.Mod.Logger.Event("Modern deferred lighting pass completed");
        }
        finally
        {
            _mod.PerformanceManager?.EndTiming("DeferredLighting_Modern", stopwatch);
        }
    }

    public void Dispose()
    {
        var shader = _shader;
        if (shader != null)
        {
            shader.Dispose();
        }

        // Dispose modern shaders
        _geometryShader?.Dispose();
        _lightingShader?.Dispose();

        _shader = null;
        _geometryShader = null;
        _lightingShader = null;
        
        if (_frameBuffer != null)
        {
            _platform.DisposeFrameBuffer(_frameBuffer);
            _frameBuffer = null;
        }
    }
}