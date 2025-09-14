using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading;

/// <summary>
/// Modern ShaderUniformManager that replaces the complex ShaderInjector system.
/// This provides direct uniform management without string manipulation overhead.
/// </summary>
public class ShaderUniformManager
{
    private readonly VolumetricShadingMod _mod;
    private readonly ICoreClientAPI _capi;
    
    // Cached uniform values to avoid redundant GPU calls
    private readonly Dictionary<string, object> _cachedUniforms = new Dictionary<string, object>();
    private readonly Dictionary<string, Func<object>> _uniformProviders = new Dictionary<string, Func<object>>();
    
    // Performance monitoring
    private int _uniformUpdatesThisFrame = 0;
    private readonly int _maxUniformUpdatesPerFrame = 100;
    private readonly Dictionary<string, Stopwatch> _timers = new Dictionary<string, Stopwatch>();
    
    // Store managed shaders
    private readonly Dictionary<string, IShaderProgram> _managedShaders = new Dictionary<string, IShaderProgram>();
    
    // AMD Compatibility - track vendor-specific optimizations
    private bool _isAmd = false;
    private bool _isIntel = false;
    private bool _isNvidia = false;
    private int _vendorMemoryLimit = 0; // MB

    public ShaderUniformManager(VolumetricShadingMod mod)
    {
        _mod = mod;
        _capi = mod.CApi;
        
        // Detect GPU vendor for optimizations
        DetectGpuVendor();
        
        RegisterStandardUniforms();
        RegisterPerformanceUniforms();
        _mod.Mod.Logger.Event("ShaderUniformManager initialized - replacing string-based injection");
    }
    
    /// <summary>
    /// Detect GPU vendor to apply vendor-specific optimizations
    /// </summary>
    private void DetectGpuVendor()
    {
        try
        {
            string vendor = GL.GetString(StringName.Vendor).ToLower();
            string renderer = GL.GetString(StringName.Renderer).ToLower();
            
            _isAmd = vendor.Contains("amd") || vendor.Contains("ati") || renderer.Contains("radeon");
            _isIntel = vendor.Contains("intel") || renderer.Contains("intel");
            _isNvidia = vendor.Contains("nvidia") || renderer.Contains("geforce");
            
            // Estimate VRAM based on renderer string
            if (_isNvidia)
            {
                // Extract from renderer string - safer than using NVX extension
                if (renderer.Contains("3090") || renderer.Contains("3080"))
                    _vendorMemoryLimit = 10240; // 10GB+ cards
                else if (renderer.Contains("3070") || renderer.Contains("2080"))
                    _vendorMemoryLimit = 8192; // 8GB cards
                else if (renderer.Contains("3060") || renderer.Contains("2070") || renderer.Contains("2060"))
                    _vendorMemoryLimit = 6144; // 6GB cards
                else if (renderer.Contains("1660") || renderer.Contains("1060"))
                    _vendorMemoryLimit = 4096; // 4GB cards
                else
                    _vendorMemoryLimit = 2048; // Conservative default
            }
            else
            {
                // Conservative estimate for AMD/Intel
                _vendorMemoryLimit = _isAmd ? 4096 : 2048;
            }
            
            _mod.Mod.Logger.Event($"Detected GPU vendor: {(_isAmd ? "AMD" : _isNvidia ? "NVIDIA" : _isIntel ? "Intel" : "Unknown")} with ~{_vendorMemoryLimit}MB VRAM");
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Warning($"Could not detect GPU vendor: {ex.Message}");
        }
    }

    /// <summary>
    /// Register standard uniforms used across multiple effects
    /// </summary>
    private void RegisterStandardUniforms()
    {
        // Volumetric Shading uniforms (using existing ModSettings properties)
        RegisterUniform("volumetricEnabled", () => ModSettings.VolumetricLightingIntensity > 0);
        RegisterUniform("volumetricIntensity", () => ModSettings.VolumetricLightingIntensity * 0.01f);
        RegisterUniform("volumetricFlatness", () => ModSettings.VolumetricLightingFlatness * 0.01f);
        
        // Screen Space Reflections uniforms
        RegisterUniform("ssrEnabled", () => ModSettings.ScreenSpaceReflectionsEnabled);
        RegisterUniform("waterTransparency", () => (100 - ModSettings.SSRWaterTransparency) * 0.01f);
        RegisterUniform("reflectionDimming", () => ModSettings.SSRReflectionDimming * 0.01f);
        RegisterUniform("tintInfluence", () => ModSettings.SSRTintInfluence * 0.01f);
        RegisterUniform("skyMixin", () => ModSettings.SSRSkyMixin * 0.01f);
        RegisterUniform("splashTransparency", () => ModSettings.SSRSplashTransparency * 0.01f);
        RegisterUniform("rainReflectionsEnabled", () => ModSettings.SSRRainReflectionsEnabled);
        RegisterUniform("refractionsEnabled", () => ModSettings.SSRRefractionsEnabled);
        RegisterUniform("causticsEnabled", () => ModSettings.SSRCausticsEnabled);
        
        // Overexposure uniforms
        RegisterUniform("overexposureEnabled", () => ModSettings.OverexposureIntensity > 0);
        RegisterUniform("overexposureIntensity", () => ModSettings.OverexposureIntensity * 0.01f);
        RegisterUniform("sunBloomIntensity", () => ModSettings.SunBloomIntensity * 0.01f);
        
        // Shadow uniforms
        RegisterUniform("softShadowsEnabled", () => ModSettings.SoftShadowsEnabled);
        RegisterUniform("shadowSamples", () => ModSettings.SoftShadowSamples);
        RegisterUniform("shadowWidth", () => ModSettings.NearShadowBaseWidth * 0.1f);
        RegisterUniform("nearShadowBias", () => ModSettings.NearPeterPanningAdjustment * 0.01f);
        RegisterUniform("farShadowBias", () => ModSettings.FarPeterPanningAdjustment * 0.01f);
        
        // Deferred lighting uniforms
        RegisterUniform("deferredLightingEnabled", () => ModSettings.DeferredLightingEnabled);
        
        // Underwater tweaks
        RegisterUniform("underwaterTweaksEnabled", () => ModSettings.UnderwaterTweaksEnabled);
        
        // For legacy code compatibility - match old VSMOD_ prefix pattern
        // These are gradually being phased out in favor of the shorter names above
        RegisterUniform("VSMOD_VOLUMETRIC_ENABLED", () => ModSettings.VolumetricLightingIntensity > 0);
        RegisterUniform("VSMOD_VOLUMETRIC_INTENSITY", () => ModSettings.VolumetricLightingIntensity * 0.01f);
        RegisterUniform("VSMOD_VOLUMETRIC_FLATNESS", () => ModSettings.VolumetricLightingFlatness * 0.01f);
        RegisterUniform("VSMOD_SSR_ENABLED", () => ModSettings.ScreenSpaceReflectionsEnabled);
        RegisterUniform("VSMOD_SSR_WATER_TRANSPARENCY", () => (100 - ModSettings.SSRWaterTransparency) * 0.01f);
        RegisterUniform("VSMOD_OVEREXPOSURE_ENABLED", () => ModSettings.OverexposureIntensity > 0);
        RegisterUniform("VSMOD_SOFT_SHADOWS_ENABLED", () => ModSettings.SoftShadowsEnabled);
        RegisterUniform("VSMOD_DEFERRED_LIGHTING", () => ModSettings.DeferredLightingEnabled);
        RegisterUniform("VSMOD_UNDERWATER_TWEAKS", () => ModSettings.UnderwaterTweaksEnabled);
        
        _mod.Mod.Logger.Event("Registered standard uniforms for all effects");
    }
    
    /// <summary>
    /// Register performance-related uniforms for quality auto-adjustment
    /// </summary>
    private void RegisterPerformanceUniforms()
    {
        // Auto-adjust quality based on detected GPU
        float qualityMultiplier = _isAmd ? 0.85f : _isIntel ? 0.7f : 1.0f;
        
        // Memory-based quality adjustments
        float memoryQuality = Math.Min(1.0f, _vendorMemoryLimit / 4096.0f);
        
        // General quality level for effects (0.0 - 1.0)
        RegisterUniform("qualityLevel", () => Math.Min(1.0f, qualityMultiplier * memoryQuality));
        
        // Effect-specific quality levels
        RegisterUniform("ssrQuality", () => Math.Max(0.3f, qualityMultiplier * memoryQuality));
        RegisterUniform("volumetricQuality", () => Math.Max(0.5f, qualityMultiplier));
        RegisterUniform("shadowQuality", () => _isAmd ? 0.8f : 1.0f); // AMD-specific shadow quality
        
        // Performance mode detection (reduces quality during lag)
        RegisterUniform("performanceMode", () => IsPerformanceModeActive());
        
        // Legacy compatibility
        RegisterUniform("VSMOD_PERFORMANCE_MODE", () => IsPerformanceModeActive());
        RegisterUniform("VSMOD_QUALITY_LEVEL", () => Math.Min(1.0f, qualityMultiplier * memoryQuality));
        
        _mod.Mod.Logger.Event("Registered performance-adaptive uniforms");
    }
    
    /// <summary>
    /// Determine if performance mode should be active based on frame times
    /// </summary>
    private bool IsPerformanceModeActive()
    {
        // Simple implementation - check if we have a PerformanceManager with actual implementation
        if (_mod.PerformanceManager != null)
        {
            // Use reflection to safely check if the method exists
            var methodInfo = _mod.PerformanceManager.GetType().GetMethod("IsPerformanceModeActive", 
                BindingFlags.Public | BindingFlags.Instance);
            if (methodInfo != null)
            {
                try
                {
                    return (bool)methodInfo.Invoke(_mod.PerformanceManager, null);
                }
                catch
                {
                    // Fall back to simple implementation
                }
            }
        }
        
        // Fallback implementation - simply check if average frame time is above threshold
        foreach (var timer in _timers.Values)
        {
            if (timer.ElapsedMilliseconds > 16) // More than 16ms = below 60fps
            {
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Register a uniform provider function
    /// </summary>
    public void RegisterUniform<T>(string uniformName, Func<T> provider)
    {
        _uniformProviders[uniformName] = () => provider();
    }

    /// <summary>
    /// Update all uniforms for a specific shader program
    /// </summary>
    public void UpdateShaderUniforms(IShaderProgram shader)
    {
        if (shader == null) return;
        
        Stopwatch stopwatch = new Stopwatch();
        stopwatch.Start();
        _uniformUpdatesThisFrame = 0;
        
        try
        {
            foreach (var kvp in _uniformProviders)
            {
                if (_uniformUpdatesThisFrame >= _maxUniformUpdatesPerFrame)
                {
                    _mod.Mod.Logger.Warning("Too many uniform updates in single frame, skipping remaining uniforms");
                    break;
                }
                
                var uniformName = kvp.Key;
                var provider = kvp.Value;
                
                try
                {
                    var newValue = provider();
                    
                    // Check if value has changed to avoid redundant GPU calls
                    if (_cachedUniforms.TryGetValue(uniformName, out var cachedValue) && 
                        Equals(cachedValue, newValue))
                    {
                        continue;
                    }
                    
                    // Update the uniform based on its type
                    SetUniformByType(shader, uniformName, newValue);
                    _cachedUniforms[uniformName] = newValue;
                    _uniformUpdatesThisFrame++;
                }
                catch (Exception ex)
                {
                    _mod.Mod.Logger.Warning($"Failed to update uniform '{uniformName}': {ex.Message}");
                }
            }
        }
        finally
        {
            stopwatch.Stop();
            _timers["ShaderUniformManager_UpdateUniforms"] = stopwatch;
            
            // Log performance data
            if (stopwatch.ElapsedMilliseconds > 5)
            {
                _mod.Mod.Logger.Debug($"Shader uniform update took {stopwatch.ElapsedMilliseconds}ms for {_uniformUpdatesThisFrame} uniforms");
            }
        }
    }
    
    /// <summary>
    /// Register a shader to be managed by this uniform manager
    /// </summary>
    public void RegisterManagedShader(string name, IShaderProgram shader)
    {
        if (shader == null) return;
        
        _managedShaders[name] = shader;
        _mod.Mod.Logger.Event($"Registered managed shader: {name}");
    }
    
    /// <summary>
    /// Update uniforms for all managed shaders
    /// </summary>
    public void UpdateManagedShaders()
    {
        foreach (var shader in _managedShaders.Values)
        {
            UpdateShaderUniforms(shader);
        }
    }

    /// <summary>
    /// Set a uniform value based on its runtime type
    /// </summary>
    private void SetUniformByType(IShaderProgram shader, string uniformName, object value)
    {
        switch (value)
        {
            case bool boolValue:
                shader.Uniform(uniformName, boolValue ? 1 : 0);
                break;
            case int intValue:
                shader.Uniform(uniformName, intValue);
                break;
            case float floatValue:
                shader.Uniform(uniformName, floatValue);
                break;
            case double doubleValue:
                shader.Uniform(uniformName, (float)doubleValue);
                break;
            case Vec2f vec2Value:
                shader.Uniform(uniformName, vec2Value);
                break;
            case Vec3f vec3Value:
                shader.Uniform(uniformName, vec3Value);
                break;
            case Vec4f vec4Value:
                shader.Uniform(uniformName, vec4Value);
                break;
            case float[] arrayValue:
                shader.UniformMatrix(uniformName, arrayValue);
                break;
            default:
                _mod.Mod.Logger.Warning($"Unsupported uniform type '{value.GetType()}' for uniform '{uniformName}'");
                break;
        }
    }

    /// <summary>
    /// Update uniforms for legacy shader programs (ShaderProgram type)
    /// </summary>
    public void UpdateLegacyShaderUniforms(ShaderProgram shader)
    {
        if (shader == null) return;
        
        // Start timing
        Stopwatch stopwatch = null;
        if (_timers.TryGetValue("legacy_uniforms", out stopwatch))
        {
            stopwatch.Restart();
        }
        else
        {
            stopwatch = Stopwatch.StartNew();
            _timers["legacy_uniforms"] = stopwatch;
        }
        
        _uniformUpdatesThisFrame = 0;
        
        try
        {
            // AMD-specific optimization - batch uniform updates
            bool shouldBatch = _isAmd;
            Dictionary<string, object> batch = shouldBatch ? new Dictionary<string, object>() : null;
            
            foreach (var kvp in _uniformProviders)
            {
                if (_uniformUpdatesThisFrame >= _maxUniformUpdatesPerFrame)
                {
                    _mod.Mod.Logger.Warning("Too many legacy uniform updates in single frame, skipping remaining");
                    break;
                }
                
                var uniformName = kvp.Key;
                var provider = kvp.Value;
                
                try
                {
                    var newValue = provider();
                    
                    // Check if value has changed
                    if (_cachedUniforms.TryGetValue(uniformName, out var cachedValue) && 
                        Equals(cachedValue, newValue))
                    {
                        continue;
                    }
                    
                    // For AMD, collect in batch
                    if (shouldBatch)
                    {
                        batch[uniformName] = newValue;
                    }
                    else
                    {
                        // Immediate update for other vendors
                        SetLegacyUniformByType(shader, uniformName, newValue);
                    }
                    
                    _cachedUniforms[uniformName] = newValue;
                    _uniformUpdatesThisFrame++;
                }
                catch (Exception ex)
                {
                    _mod.Mod.Logger.Warning($"Failed to update legacy uniform '{uniformName}': {ex.Message}");
                }
            }
            
            // Apply batched uniforms for AMD
            if (shouldBatch && batch.Count > 0)
            {
                foreach (var kvp in batch)
                {
                    SetLegacyUniformByType(shader, kvp.Key, kvp.Value);
                }
            }
        }
        finally
        {
            stopwatch.Stop();
            long elapsed = stopwatch.ElapsedMilliseconds;
            
            // Log performance issues
            if (elapsed > 5)
            {
                _mod.Mod.Logger.Debug($"Legacy uniform updates took {elapsed}ms for {_uniformUpdatesThisFrame} uniforms");
            }
        }
    }

    /// <summary>
    /// Set a uniform value for legacy shader programs
    /// </summary>
    private void SetLegacyUniformByType(ShaderProgram shader, string uniformName, object value)
    {
        switch (value)
        {
            case bool boolValue:
                shader.Uniform(uniformName, boolValue ? 1 : 0);
                break;
            case int intValue:
                shader.Uniform(uniformName, intValue);
                break;
            case float floatValue:
                shader.Uniform(uniformName, floatValue);
                break;
            case double doubleValue:
                shader.Uniform(uniformName, (float)doubleValue);
                break;
            case Vec2f vec2Value:
                shader.Uniform(uniformName, vec2Value);
                break;
            case Vec3f vec3Value:
                shader.Uniform(uniformName, vec3Value);
                break;
            case Vec4f vec4Value:
                shader.Uniform(uniformName, vec4Value);
                break;
            case float[] arrayValue:
                shader.UniformMatrix(uniformName, arrayValue);
                break;
            default:
                _mod.Mod.Logger.Warning($"Unsupported legacy uniform type '{value.GetType()}' for uniform '{uniformName}'");
                break;
        }
    }

    /// <summary>
    /// Invalidate cached uniforms to force updates
    /// </summary>
    public void InvalidateCache()
    {
        _cachedUniforms.Clear();
        _mod.Mod.Logger.Event("Shader uniform cache invalidated");
    }

    /// <summary>
    /// Get current value of a specific uniform
    /// </summary>
    public T GetUniformValue<T>(string uniformName)
    {
        if (_uniformProviders.TryGetValue(uniformName, out var provider))
        {
            try
            {
                return (T)provider();
            }
            catch (Exception ex)
            {
                _mod.Mod.Logger.Warning($"Failed to get uniform value '{uniformName}': {ex.Message}");
            }
        }
        
        return default(T);
    }

    /// <summary>
    /// Check if a uniform is registered
    /// </summary>
    public bool HasUniform(string uniformName)
    {
        return _uniformProviders.ContainsKey(uniformName);
    }

    /// <summary>
    /// Get statistics about uniform management
    /// </summary>
    public (int RegisteredUniforms, int CachedUniforms, int LastFrameUpdates, long AverageUpdateTime) GetStatistics()
    {
        long avgTime = 0;
        if (_timers.TryGetValue("legacy_uniforms", out var stopwatch))
        {
            avgTime = stopwatch.ElapsedMilliseconds;
        }
        
        return (_uniformProviders.Count, _cachedUniforms.Count, _uniformUpdatesThisFrame, avgTime);
    }
    
    /// <summary>
    /// Get GPU vendor information and compatibility recommendations
    /// </summary>
    public (bool IsAmd, bool IsIntel, bool IsNvidia, int VramMb) GetVendorInfo()
    {
        return (_isAmd, _isIntel, _isNvidia, _vendorMemoryLimit);
    }

    /// <summary>
    /// Dispose of resources
    /// </summary>
    public void Dispose()
    {
        _uniformProviders.Clear();
        _cachedUniforms.Clear();
        _mod.Mod.Logger.Event("ShaderUniformManager disposed");
    }
}
