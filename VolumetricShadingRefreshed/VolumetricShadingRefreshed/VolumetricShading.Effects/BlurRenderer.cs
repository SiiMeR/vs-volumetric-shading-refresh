using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading.Effects;

/// <summary>
/// Modern BlurRenderer implementing separable Gaussian blur with dedicated shaders.
/// This replaces YAML-based blur.vsh patches with better performance through proper separable blur.
/// </summary>
public class BlurRenderer : IRenderer
{
    private readonly VolumetricShadingMod _mod;
    private readonly ClientPlatformWindows _platform;
    
    private bool _enabled = true;
    private IShaderProgram _blurHorizontalShader;
    private IShaderProgram _blurVerticalShader;
    private MeshRef _screenQuad;
    private FrameBufferRef _tempFrameBuffer1;
    private FrameBufferRef _tempFrameBuffer2;
    
    // Blur settings
    private float _blurRadius = 4.0f;
    private int _blurSamples = 9;
    
    // Performance monitoring
    private readonly string _timingKeyHorizontal = "BlurRenderer_Horizontal";
    private readonly string _timingKeyVertical = "BlurRenderer_Vertical";
    
    public double RenderOrder => 950.0; // Before overexposure but after main effects
    public int RenderRange => 9999;

    public BlurRenderer(VolumetricShadingMod mod)
    {
        _mod = mod;
        _platform = _mod.CApi.GetClientPlatformWindows();
        
        // Watch for blur quality changes
        _mod.CApi.Settings.AddWatcher("bloomQuality", new OnSettingsChanged<int>(OnBloomQualityChanged));
        _mod.CApi.Settings.AddWatcher("volumetricshading_blurQuality", new OnSettingsChanged<int>(OnBlurQualityChanged));
        
        // Setup shader loading
        _mod.CApi.Event.ReloadShader += OnReloadShaders;
        _mod.Events.RebuildFramebuffers += SetupFramebuffers;
        
        // Initialize
        SetupFramebuffers(_platform.FrameBuffers);
        OnReloadShaders();
        
        _mod.Mod.Logger.Event("BlurRenderer initialized with separable blur approach");
    }

    private void OnBloomQualityChanged(int quality)
    {
        // Adjust blur quality based on bloom settings
        _blurSamples = Math.Max(5, quality * 2 + 3); // 5-9 samples based on quality
        _mod.Mod.Logger.Event($"Blur samples adjusted to {_blurSamples} based on bloom quality");
    }

    private void OnBlurQualityChanged(int quality)
    {
        _blurSamples = Math.Clamp(quality, 5, 17); // User-controlled blur quality
        _mod.Mod.Logger.Event($"Blur quality set to {_blurSamples} samples");
    }

    private bool OnReloadShaders()
    {
        var success = true;
        
        // Dispose existing shaders
        _blurHorizontalShader?.Dispose();
        _blurVerticalShader?.Dispose();
        
        try
        {
            // Create horizontal blur shader
            _blurHorizontalShader = _mod.CApi.Shader.NewShaderProgram()
                .WithName("blur_horizontal")
                .WithVertexShader(_mod.CApi.Assets.Get(new AssetLocation(_mod.Mod.Info.ModID, "shaders/blur_horizontal.vsh")))
                .WithFragmentShader(_mod.CApi.Assets.Get(new AssetLocation(_mod.Mod.Info.ModID, "shaders/blur_horizontal.fsh")))
                .WithUniformProvider(() => {
                    if (_tempFrameBuffer1 != null)
                    {
                        _blurHorizontalShader.Uniform("texelSize", 1.0f / _tempFrameBuffer1.Width, 0.0f);
                        _blurHorizontalShader.Uniform("blurRadius", _blurRadius);
                        _blurHorizontalShader.Uniform("blurSamples", _blurSamples);
                    }
                })
                .Compile();
            
            // Create vertical blur shader
            _blurVerticalShader = _mod.CApi.Shader.NewShaderProgram()
                .WithName("blur_vertical")
                .WithVertexShader(_mod.CApi.Assets.Get(new AssetLocation(_mod.Mod.Info.ModID, "shaders/blur_vertical.vsh")))
                .WithFragmentShader(_mod.CApi.Assets.Get(new AssetLocation(_mod.Mod.Info.ModID, "shaders/blur_vertical.fsh")))
                .WithUniformProvider(() => {
                    if (_tempFrameBuffer1 != null)
                    {
                        _blurVerticalShader.Uniform("texelSize", 0.0f, 1.0f / _tempFrameBuffer1.Height);
                        _blurVerticalShader.Uniform("blurRadius", _blurRadius);
                        _blurVerticalShader.Uniform("blurSamples", _blurSamples);
                    }
                })
                .Compile();

            if (_blurHorizontalShader != null && _blurVerticalShader != null)
            {
                _mod.Mod.Logger.Event("Blur shaders compiled successfully");
            }
            else
            {
                _mod.Mod.Logger.Error("Failed to compile blur shaders");
                success = false;
            }
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Error($"Exception loading blur shaders: {ex.Message}");
            success = false;
        }
        
        return success;
    }

    private void SetupFramebuffers(List<FrameBufferRef> mainBuffers)
    {
        // Dispose existing framebuffers
        if (_tempFrameBuffer1 != null)
        {
            _platform.DisposeFrameBuffer(_tempFrameBuffer1);
            _tempFrameBuffer1 = null;
        }
        if (_tempFrameBuffer2 != null)
        {
            _platform.DisposeFrameBuffer(_tempFrameBuffer2);
            _tempFrameBuffer2 = null;
        }

        if (!_enabled || mainBuffers.Count == 0)
        {
            return;
        }

        var fbPrimary = mainBuffers[0];
        
        // Use half resolution for blur to improve performance
        var fbWidth = Math.Max(1, (int)(_platform.window.Bounds.Size.X * ClientSettings.SSAA * 0.5f));
        var fbHeight = Math.Max(1, (int)(_platform.window.Bounds.Size.Y * ClientSettings.SSAA * 0.5f));
        
        try
        {
            // Create first temporary framebuffer (for horizontal pass)
            _tempFrameBuffer1 = new FrameBufferRef
            {
                FboId = GL.GenFramebuffer(),
                Width = fbWidth,
                Height = fbHeight,
                ColorTextureIds = new int[] { GL.GenTexture() }
            };

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _tempFrameBuffer1.FboId);
            _tempFrameBuffer1.SetupColorTexture(0, PixelInternalFormat.Rgba16f); // Higher precision for blur
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            Framebuffers.CheckStatus();
            
            // Create second temporary framebuffer (for vertical pass)
            _tempFrameBuffer2 = new FrameBufferRef
            {
                FboId = GL.GenFramebuffer(),
                Width = fbWidth,
                Height = fbHeight,
                ColorTextureIds = new int[] { GL.GenTexture() }
            };

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, _tempFrameBuffer2.FboId);
            _tempFrameBuffer2.SetupColorTexture(0, PixelInternalFormat.Rgba16f);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            Framebuffers.CheckStatus();
            
            _screenQuad = _platform.GetScreenQuad();
            
            _mod.Mod.Logger.Event($"Blur framebuffers created: {fbWidth}x{fbHeight}");
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Error($"Failed to setup blur framebuffers: {ex.Message}");
            _tempFrameBuffer1 = null;
            _tempFrameBuffer2 = null;
        }
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        // Blur is typically applied to bloom or glow effects
        if (stage == EnumRenderStage.AfterBloom && _enabled && 
            _blurHorizontalShader != null && _blurVerticalShader != null && 
            _tempFrameBuffer1 != null && _tempFrameBuffer2 != null)
        {
            ApplyBlur();
        }
    }

    /// <summary>
    /// Apply separable Gaussian blur to the specified texture.
    /// This is more efficient than single-pass blur and produces better results.
    /// </summary>
    /// <param name="inputTextureId">Texture to blur (if 0, uses current framebuffer)</param>
    /// <param name="outputFrameBuffer">Target framebuffer (if null, renders to current)</param>
    public void ApplyBlur(int inputTextureId = 0, FrameBufferRef outputFrameBuffer = null)
    {
        var fbPrimary = _platform.FrameBuffers[0];
        var sourceTextureId = inputTextureId == 0 ? fbPrimary.ColorTextureIds[1] : inputTextureId; // Default to glow buffer
        
        // Save current state
        _platform.GlDisableDepthTest();
        _platform.GlToggleBlend(false);
        
        // PASS 1: Horizontal blur
        var stopwatchH = _mod.PerformanceManager?.StartTiming(_timingKeyHorizontal);
        try
        {
            _platform.LoadFrameBuffer(_tempFrameBuffer1);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            
            _blurHorizontalShader.Use();
            _blurHorizontalShader.BindTexture2D("inputTexture", sourceTextureId);
            
            _platform.RenderFullscreenTriangle(_screenQuad);
            _blurHorizontalShader.Stop();
        }
        finally
        {
            _mod.PerformanceManager?.EndTiming(_timingKeyHorizontal, stopwatchH);
        }
        
        // PASS 2: Vertical blur
        var stopwatchV = _mod.PerformanceManager?.StartTiming(_timingKeyVertical);
        try
        {
            var targetFrameBuffer = outputFrameBuffer ?? _tempFrameBuffer2;
            _platform.LoadFrameBuffer(targetFrameBuffer);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            
            _blurVerticalShader.Use();
            _blurVerticalShader.BindTexture2D("inputTexture", _tempFrameBuffer1.ColorTextureIds[0]);
            
            _platform.RenderFullscreenTriangle(_screenQuad);
            _blurVerticalShader.Stop();
            
            // If we used our temp buffer, copy result back to main buffer
            if (outputFrameBuffer == null)
            {
                _platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
                GL.BlitFramebuffer(
                    0, 0, targetFrameBuffer.Width, targetFrameBuffer.Height,
                    0, 0, fbPrimary.Width, fbPrimary.Height,
                    ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
            }
        }
        finally
        {
            _mod.PerformanceManager?.EndTiming(_timingKeyVertical, stopwatchV);
        }
        
        // Restore state
        _platform.GlEnableDepthTest();
        _platform.CheckGlError("Blur post-process");
    }

    /// <summary>
    /// Set blur parameters for dynamic quality adjustment
    /// </summary>
    public void SetBlurParameters(float radius, int samples)
    {
        _blurRadius = Math.Max(0.1f, radius);
        _blurSamples = Math.Clamp(samples, 3, 17);
        _mod.Mod.Logger.Event($"Blur parameters updated: radius={_blurRadius}, samples={_blurSamples}");
    }

    /// <summary>
    /// Enable or disable blur rendering
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        _mod.Mod.Logger.Event($"Blur renderer {(enabled ? "enabled" : "disabled")}");
    }

    public void Dispose()
    {
        _blurHorizontalShader?.Dispose();
        _blurVerticalShader?.Dispose();
        _blurHorizontalShader = null;
        _blurVerticalShader = null;
        
        if (_tempFrameBuffer1 != null)
        {
            _platform.DisposeFrameBuffer(_tempFrameBuffer1);
            _tempFrameBuffer1 = null;
        }
        if (_tempFrameBuffer2 != null)
        {
            _platform.DisposeFrameBuffer(_tempFrameBuffer2);
            _tempFrameBuffer2 = null;
        }
    }
}
