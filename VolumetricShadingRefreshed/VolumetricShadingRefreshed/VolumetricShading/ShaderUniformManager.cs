using System;
using System.Collections.Generic;
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

    public ShaderUniformManager(VolumetricShadingMod mod)
    {
        _mod = mod;
        _capi = mod.CApi;
        
        RegisterStandardUniforms();
        _mod.Mod.Logger.Event("ShaderUniformManager initialized - replacing string-based injection");
    }

    /// <summary>
    /// Register standard uniforms used across multiple effects
    /// </summary>
    private void RegisterStandardUniforms()
    {
        // Volumetric Shading uniforms
        RegisterUniform("VSMOD_VOLUMETRIC_ENABLED", () => ModSettings.VolumetricLightingEnabled);
        RegisterUniform("VSMOD_VOLUMETRIC_INTENSITY", () => ModSettings.VolumetricLightingIntensity * 0.01f);
        RegisterUniform("VSMOD_VOLUMETRIC_FLATNESS", () => ModSettings.VolumetricLightingFlatness * 0.01f);
        
        // Screen Space Reflections uniforms
        RegisterUniform("VSMOD_SSR_ENABLED", () => ModSettings.ScreenSpaceReflectionsEnabled);
        RegisterUniform("VSMOD_SSR_WATER_TRANSPARENCY", () => (100 - ModSettings.SSRWaterTransparency) * 0.01f);
        RegisterUniform("VSMOD_SSR_REFLECTION_DIMMING", () => ModSettings.SSRReflectionDimming * 0.01f);
        RegisterUniform("VSMOD_SSR_TINT_INFLUENCE", () => ModSettings.SSRTintInfluence * 0.01f);
        RegisterUniform("VSMOD_SSR_SKY_MIXIN", () => ModSettings.SSRSkyMixin * 0.01f);
        RegisterUniform("VSMOD_SSR_SPLASH_TRANSPARENCY", () => ModSettings.SSRSplashTransparency * 0.01f);
        RegisterUniform("VSMOD_SSR_RAIN_REFLECTIONS", () => ModSettings.SSRRainReflectionsEnabled);
        RegisterUniform("VSMOD_SSR_REFRACTIONS", () => ModSettings.SSRRefractionsEnabled);
        RegisterUniform("VSMOD_SSR_CAUSTICS", () => ModSettings.SSRCausticsEnabled);
        
        // Overexposure uniforms
        RegisterUniform("VSMOD_OVEREXPOSURE_ENABLED", () => ModSettings.OverexposureIntensity > 0);
        RegisterUniform("VSMOD_OVEREXPOSURE_INTENSITY", () => ModSettings.OverexposureIntensity * 0.01f);
        RegisterUniform("VSMOD_SUN_BLOOM_INTENSITY", () => ModSettings.SunBloomIntensity * 0.01f);
        
        // Shadow uniforms
        RegisterUniform("VSMOD_SOFT_SHADOWS_ENABLED", () => ModSettings.SoftShadowsEnabled);
        RegisterUniform("VSMOD_SOFT_SHADOW_SAMPLES", () => ModSettings.SoftShadowSamples);
        RegisterUniform("VSMOD_NEAR_SHADOW_WIDTH", () => ModSettings.NearShadowBaseWidth * 0.1f);
        RegisterUniform("VSMOD_NEAR_PETER_PANNING", () => ModSettings.NearPeterPanningAdjustment * 0.01f);
        RegisterUniform("VSMOD_FAR_PETER_PANNING", () => ModSettings.FarPeterPanningAdjustment * 0.01f);
        
        // Deferred lighting uniforms
        RegisterUniform("VSMOD_DEFERRED_LIGHTING", () => ModSettings.DeferredLightingEnabled);
        
        // Underwater tweaks
        RegisterUniform("VSMOD_UNDERWATER_TWEAKS", () => ModSettings.UnderwaterTweaksEnabled);
        
        // Performance-related uniforms
        RegisterUniform("VSMOD_PERFORMANCE_MODE", () => _mod.PerformanceManager?.IsPerformanceModeActive ?? false);
        RegisterUniform("VSMOD_QUALITY_LEVEL", () => _mod.PerformanceManager?.GetCurrentQualityLevel() ?? 1.0f);
        
        _mod.Mod.Logger.Event("Registered standard uniforms for all effects");
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
        
        var stopwatch = _mod.PerformanceManager?.StartTiming("ShaderUniformManager_UpdateUniforms");
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
            _mod.PerformanceManager?.EndTiming("ShaderUniformManager_UpdateUniforms", stopwatch);
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
        
        var stopwatch = _mod.PerformanceManager?.StartTiming("ShaderUniformManager_UpdateLegacyUniforms");
        _uniformUpdatesThisFrame = 0;
        
        try
        {
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
                    
                    // Update the uniform based on its type (legacy API)
                    SetLegacyUniformByType(shader, uniformName, newValue);
                    _cachedUniforms[uniformName] = newValue;
                    _uniformUpdatesThisFrame++;
                }
                catch (Exception ex)
                {
                    _mod.Mod.Logger.Warning($"Failed to update legacy uniform '{uniformName}': {ex.Message}");
                }
            }
        }
        finally
        {
            _mod.PerformanceManager?.EndTiming("ShaderUniformManager_UpdateLegacyUniforms", stopwatch);
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
    public (int RegisteredUniforms, int CachedUniforms, int LastFrameUpdates) GetStatistics()
    {
        return (_uniformProviders.Count, _cachedUniforms.Count, _uniformUpdatesThisFrame);
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
