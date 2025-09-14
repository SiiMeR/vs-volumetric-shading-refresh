using System;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading.Effects;

/// <summary>
/// Modern SoftShadowRenderer that replaces YAML-based soft shadow patches.
/// Implements PCSS (Percentage Closer Soft Shadows) for high-quality soft shadows.
/// </summary>
public class SoftShadowRenderer : IRenderer
{
    private readonly VolumetricShadingMod _mod;
    private readonly ClientPlatformWindows _platform;
    
    private bool _enabled;
    private IShaderProgram _softShadowShader;
    private MeshRef _screenQuad;
    private FrameBufferRef _shadowFrameBuffer;
    
    // Shadow settings
    private int _shadowSamples = 16;
    private float _shadowRadius = 4.0f;
    private float _nearShadowWidth = 1.5f;
    private float _farShadowWidth = 3.0f;
    
    // Performance monitoring
    private readonly string _timingKey = "SoftShadowRenderer";

    public double RenderOrder => 100.0; // Early in the shadow pipeline
    public int RenderRange => 9999;

    public SoftShadowRenderer(VolumetricShadingMod mod)
    {
        _mod = mod;
        _platform = _mod.CApi.GetClientPlatformWindows();
        
        // Watch for shadow quality changes
        _mod.CApi.Settings.AddWatcher("shadowMapQuality", new OnSettingsChanged<int>(OnShadowQualityChanged));
        _mod.CApi.Settings.AddWatcher("volumetricshading_softShadowSamples", 
            new OnSettingsChanged<int>(OnSoftShadowSamplesChanged));
        _mod.CApi.Settings.AddWatcher("volumetricshading_softShadowsEnabled", 
            new OnSettingsChanged<bool>(OnSoftShadowsEnabledChanged));
        
        _enabled = ModSettings.SoftShadowsEnabled;
        _shadowSamples = ModSettings.SoftShadowSamples;
        
        // Setup shader loading
        _mod.CApi.Event.ReloadShader += OnReloadShaders;
        _mod.Events.RebuildFramebuffers += SetupFramebuffers;
        
        // Initialize
        SetupFramebuffers(_platform.FrameBuffers);
        OnReloadShaders();
        
        _mod.Mod.Logger.Event("SoftShadowRenderer initialized with PCSS approach");
    }

    private void OnShadowQualityChanged(int quality)
    {
        _enabled = _enabled && quality > 0;
        if (quality == 0)
        {
            _mod.Mod.Logger.Event("Soft shadows disabled due to shadow quality setting");
        }
    }

    private void OnSoftShadowSamplesChanged(int samples)
    {
        _shadowSamples = Math.Clamp(samples, 4, 64);
        _mod.Mod.Logger.Event($"Soft shadow samples set to {_shadowSamples}");
    }

    private void OnSoftShadowsEnabledChanged(bool enabled)
    {
        _enabled = enabled && ModSettings.SoftShadowsEnabled;
        _mod.Mod.Logger.Event($"Soft shadows {(enabled ? "enabled" : "disabled")}");
    }

    private bool OnReloadShaders()
    {
        var success = true;
        
        // Dispose existing shader
        _softShadowShader?.Dispose();
        _softShadowShader = null;
        
        try
        {
            // Create dedicated soft shadow shader
            _softShadowShader = _mod.CApi.Shader.NewShaderProgram()
                .WithName("soft_shadows")
                .WithVertexShader(_mod.CApi.Assets.Get(new AssetLocation(_mod.Mod.Info.ModID, "shaders/softshadow.vsh")))
                .WithFragmentShader(_mod.CApi.Assets.Get(new AssetLocation(_mod.Mod.Info.ModID, "shaders/softshadow.fsh")))
                .WithUniformProvider(() => {
                    _softShadowShader.Uniform("softShadowSamples", _shadowSamples);
                    _softShadowShader.Uniform("nearShadowWidth", _nearShadowWidth);
                    _softShadowShader.Uniform("farShadowWidth", _farShadowWidth);
                    _softShadowShader.Uniform("shadowRadius", _shadowRadius);
                    
                    // Shadow bias uniforms
                    _softShadowShader.Uniform("nearShadowOffset", ModSettings.NearPeterPanningAdjustment * 0.01f);
                    _softShadowShader.Uniform("farShadowOffset", ModSettings.FarPeterPanningAdjustment * 0.01f);
                    
                    // Time-based dithering for temporal anti-aliasing
                    _softShadowShader.Uniform("frameTime", (float)_mod.CApi.InWorldEllapsedMilliseconds * 0.001f);
                })
                .Compile();

            if (_softShadowShader != null)
            {
                _mod.Mod.Logger.Event("Soft shadow shader compiled successfully");
            }
            else
            {
                _mod.Mod.Logger.Error("Failed to compile soft shadow shader");
                success = false;
            }
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Error($"Exception loading soft shadow shader: {ex.Message}");
            success = false;
        }
        
        return success;
    }

    private void SetupFramebuffers(System.Collections.Generic.List<FrameBufferRef> mainBuffers)
    {
        // Dispose existing framebuffer
        if (_shadowFrameBuffer != null)
        {
            _platform.DisposeFrameBuffer(_shadowFrameBuffer);
            _shadowFrameBuffer = null;
        }

        if (!_enabled || mainBuffers.Count == 0)
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

        try
        {
            // Create shadow processing framebuffer
            _shadowFrameBuffer = new FrameBufferRef
            {
                FboId = GL.GenFramebuffer(),
                Width = fbWidth,
                Height = fbHeight,
                ColorTextureIds = new int[] { GL.GenTexture() },
                DepthTextureId = fbPrimary.DepthTextureId // Share depth buffer
            };

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _shadowFrameBuffer.FboId);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                TextureTarget.Texture2D, _shadowFrameBuffer.DepthTextureId, 0);
            
            _shadowFrameBuffer.SetupColorTexture(0, PixelInternalFormat.R16f); // Single channel for shadow factor
            
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            Framebuffers.CheckStatus();
            
            _screenQuad = _platform.GetScreenQuad();
            
            _mod.Mod.Logger.Event($"Soft shadow framebuffer created: {fbWidth}x{fbHeight}");
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Error($"Failed to setup soft shadow framebuffer: {ex.Message}");
            _shadowFrameBuffer = null;
        }
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        // Apply soft shadows during shadow processing stage
        if (stage == EnumRenderStage.Shadows && _enabled && 
            _softShadowShader != null && _shadowFrameBuffer != null)
        {
            ApplySoftShadows();
        }
    }

    private void ApplySoftShadows()
    {
        var stopwatch = _mod.PerformanceManager?.StartTiming(_timingKey);
        try
        {
            var fbPrimary = _platform.FrameBuffers[0];
            
            // Bind our shadow framebuffer
            _platform.LoadFrameBuffer(_shadowFrameBuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            
            // Disable depth testing for shadow processing
            _platform.GlDisableDepthTest();
            _platform.GlToggleBlend(false);
            
            // Use our dedicated soft shadow shader
            _softShadowShader.Use();
            
            // Bind shadow maps and depth
            if (ShaderProgramBase.shadowmapQuality > 0)
            {
                var render = _mod.CApi.Render;
                var uniforms = render.ShaderUniforms;
                
                // Bind shadow textures
                _softShadowShader.BindTexture2D("shadowMapNearTex", uniforms.ShadowMapNear);
                _softShadowShader.BindTexture2D("shadowMapFarTex", uniforms.ShadowMapFar);
                _softShadowShader.BindTexture2D("depthTexture", fbPrimary.DepthTextureId);
                _softShadowShader.BindTexture2D("normalTexture", fbPrimary.ColorTextureIds[2]); // G-buffer normals
                
                // Set shadow matrices
                _softShadowShader.UniformMatrix("toShadowMapSpaceMatrixNear", uniforms.ToShadowMapSpaceMatrixNear);
                _softShadowShader.UniformMatrix("toShadowMapSpaceMatrixFar", uniforms.ToShadowMapSpaceMatrixFar);
                _softShadowShader.Uniform("shadowRangeNear", uniforms.ShadowRangeNear);
                _softShadowShader.Uniform("shadowRangeFar", uniforms.ShadowRangeFar);
                
                // Set projection matrices for world position reconstruction
                _softShadowShader.UniformMatrix("invProjectionMatrix", _mod.Uniforms.InvProjectionMatrix);
                _softShadowShader.UniformMatrix("invModelViewMatrix", _mod.Uniforms.InvModelViewMatrix);
            }
            
            // Render full-screen quad
            _platform.RenderFullscreenTriangle(_screenQuad);
            _softShadowShader.Stop();
            
            // Copy result back to shadow buffer or blend with existing shadows
            _platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
            GL.BlitFramebuffer(
                0, 0, _shadowFrameBuffer.Width, _shadowFrameBuffer.Height,
                0, 0, fbPrimary.Width, fbPrimary.Height,
                ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            
            // Restore state
            _platform.GlEnableDepthTest();
            _platform.CheckGlError("Soft shadow processing");
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Error($"Error in soft shadow processing: {ex.Message}");
            // Disable on critical error to prevent crashes
            _enabled = false;
            ModSettings.SoftShadowsEnabled = false;
        }
        finally
        {
            _mod.PerformanceManager?.EndTiming(_timingKey, stopwatch);
        }
    }

    /// <summary>
    /// Set soft shadow parameters for dynamic quality adjustment
    /// </summary>
    public void SetSoftShadowParameters(int samples, float radius, float nearWidth, float farWidth)
    {
        _shadowSamples = Math.Clamp(samples, 4, 64);
        _shadowRadius = Math.Max(0.1f, radius);
        _nearShadowWidth = Math.Max(0.1f, nearWidth);
        _farShadowWidth = Math.Max(0.1f, farWidth);
        
        _mod.Mod.Logger.Event($"Soft shadow parameters updated: samples={_shadowSamples}, radius={_shadowRadius}");
    }

    /// <summary>
    /// Enable or disable soft shadow rendering
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        _enabled = enabled && ModSettings.SoftShadowsEnabled;
        _mod.Mod.Logger.Event($"Soft shadow renderer {(enabled ? "enabled" : "disabled")}");
    }

    public void Dispose()
    {
        _softShadowShader?.Dispose();
        _softShadowShader = null;
        
        if (_shadowFrameBuffer != null)
        {
            _platform.DisposeFrameBuffer(_shadowFrameBuffer);
            _shadowFrameBuffer = null;
        }
    }
}
