using System;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using volumetricshadingupdated.VolumetricShading;

namespace volumetricshadingupdated.VolumetricShading.Effects;

/// <summary>
/// Modern OverexposureRenderer that replaces YAML-based shader patching with dedicated post-process shader.
/// This implementation provides better performance and compatibility compared to runtime string manipulation.
/// </summary>
public class OverexposureRenderer : IRenderer
{
    private readonly VolumetricShadingMod _mod;
    private readonly ClientPlatformWindows _platform;
    
    private bool _enabled;
    private IShaderProgram _overexposureShader;
    private MeshRef _screenQuad;
    private FrameBufferRef _tempFrameBuffer;
    
    // Performance monitoring
    private readonly string _timingKey = "OverexposureRenderer";

    public double RenderOrder => 1000.0; // After main rendering, before final compositing
    public int RenderRange => 9999;

    public OverexposureRenderer(VolumetricShadingMod mod)
    {
        _mod = mod;
        _platform = _mod.CApi.GetClientPlatformWindows();
        
        // Watch for settings changes
        _mod.CApi.Settings.AddWatcher("volumetricshading_overexposureIntensity", 
            new OnSettingsChanged<int>(OnOverexposureIntensityChanged));
        _mod.CApi.Settings.AddWatcher("volumetricshading_sunBloomIntensity", 
            new OnSettingsChanged<int>(OnSunBloomIntensityChanged));
        
        _enabled = ModSettings.OverexposureIntensity > 0;
        
        // Setup shader loading
        _mod.CApi.Event.ReloadShader += OnReloadShaders;
        _mod.Events.RebuildFramebuffers += SetupFramebuffers;
        
        // Initialize
        SetupFramebuffers(_platform.FrameBuffers);
        OnReloadShaders();
        
        _mod.Mod.Logger.Event("OverexposureRenderer initialized with dedicated shader approach");
    }

    private void OnOverexposureIntensityChanged(int intensity)
    {
        _enabled = intensity > 0;
        if (!_enabled)
        {
            _mod.Mod.Logger.Event("Overexposure effect disabled");
        }
        else
        {
            _mod.Mod.Logger.Event($"Overexposure intensity changed to {intensity}%");
        }
    }

    private void OnSunBloomIntensityChanged(int intensity)
    {
        _mod.Mod.Logger.Event($"Sun bloom intensity changed to {intensity}%");
    }

    private bool OnReloadShaders()
    {
        var success = true;
        
        // Dispose existing shader
        _overexposureShader?.Dispose();
        _overexposureShader = null;
        
        try
        {
            // Use our extended shader registration
            bool shaderSuccess = true;
            _overexposureShader = (IShaderProgram)VSModShaderExtensions.RegisterVSModShader(_mod, "overexposure", ref shaderSuccess);

            success = shaderSuccess && _overexposureShader != null;
            if (success)
            {
                _mod.Mod.Logger.Event("Overexposure shader loaded successfully");
            }
            else
            {
                _mod.Mod.Logger.Error("Failed to load overexposure shader");
            }
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Error($"Exception loading overexposure shader: {ex.Message}");
            _mod.Mod.Logger.Error($"Stack trace: {ex.StackTrace}");
            success = false;
        }
        
        return success;
    }

    private void SetupFramebuffers(System.Collections.Generic.List<FrameBufferRef> mainBuffers)
    {
        // Dispose existing framebuffer
        if (_tempFrameBuffer != null)
        {
            _platform.DisposeFrameBuffer(_tempFrameBuffer);
            _tempFrameBuffer = null;
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

        // Create temporary framebuffer for overexposure processing
        try
        {
            _tempFrameBuffer = new FrameBufferRef
            {
                FboId = GL.GenFramebuffer(),
                Width = fbWidth,
                Height = fbHeight,
                ColorTextureIds = new int[] { GL.GenTexture() },
                DepthTextureId = fbPrimary.DepthTextureId // Share depth buffer
            };

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _tempFrameBuffer.FboId);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                TextureTarget.Texture2D, _tempFrameBuffer.DepthTextureId, 0);
            
            _tempFrameBuffer.SetupColorTexture(0);
            
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            Framebuffers.CheckStatus();
            
            _screenQuad = _platform.GetScreenQuad();
            
            _mod.Mod.Logger.Event($"Overexposure framebuffer created: {fbWidth}x{fbHeight}");
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Error($"Failed to setup overexposure framebuffer: {ex.Message}");
            _tempFrameBuffer = null;
        }
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        // Apply overexposure effect after standard rendering but before final composite
        if (stage == EnumRenderStage.AfterOIT && _enabled && _overexposureShader != null && _tempFrameBuffer != null)
        {
            ApplyOverexposure();
        }
    }

    private void ApplyOverexposure()
    {
        var stopwatch = _mod.PerformanceManager?.StartTiming(_timingKey);
        try
        {
            var fbPrimary = _platform.FrameBuffers[0];
            
            // Bind our temporary framebuffer
            _platform.LoadFrameBuffer(_tempFrameBuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            
            // Disable depth testing for post-process
            _platform.GlDisableDepthTest();
            _platform.GlToggleBlend(false);
            
            // Use our dedicated overexposure shader
            _overexposureShader.Use();
            
            // Bind input textures (3-parameter version)
            _overexposureShader.BindTexture2D("inputTexture", fbPrimary.ColorTextureIds[0], 0);
            _overexposureShader.BindTexture2D("depthTexture", fbPrimary.DepthTextureId, 1);
            _overexposureShader.BindTexture2D("normalTexture", fbPrimary.ColorTextureIds[2], 2); // G-buffer normals
            _overexposureShader.BindTexture2D("glowTexture", fbPrimary.ColorTextureIds[1], 3); // Glow buffer
            
            // Set up screen-space to world-space transformation matrices
            var render = _mod.CApi.Render;
            var uniforms = render.ShaderUniforms;
            
            _overexposureShader.UniformMatrix("invProjectionMatrix", _mod.Uniforms.InvProjectionMatrix);
            _overexposureShader.UniformMatrix("invModelViewMatrix", _mod.Uniforms.InvModelViewMatrix);
            _overexposureShader.Uniform("sunPosition", uniforms.SunPosition3D);
            
            // Render full-screen quad
            _platform.RenderFullscreenTriangle(_screenQuad);
            _overexposureShader.Stop();
            
            // Copy result back to main framebuffer
            _platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
            GL.BlitFramebuffer(
                0, 0, _tempFrameBuffer.Width, _tempFrameBuffer.Height,
                0, 0, fbPrimary.Width, fbPrimary.Height,
                ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            
            // Restore state
            _platform.GlEnableDepthTest();
            _platform.CheckGlError("Overexposure post-process");
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Error($"Error in overexposure post-process: {ex.Message}");
            // Disable on critical error to prevent crashes
            _enabled = false;
            ModSettings.OverexposureIntensity = 0;
        }
        finally
        {
            _mod.PerformanceManager?.EndTiming(_timingKey, stopwatch);
        }
    }

    public void Dispose()
    {
        _overexposureShader?.Dispose();
        _overexposureShader = null;
        
        if (_tempFrameBuffer != null)
        {
            _platform.DisposeFrameBuffer(_tempFrameBuffer);
            _tempFrameBuffer = null;
        }
    }
}
