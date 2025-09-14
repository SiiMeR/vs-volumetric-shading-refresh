using System;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;
using volumetricshadingupdated.VolumetricShading.Patch;

namespace volumetricshadingupdated.VolumetricShading.Effects;

public class OverexposureEffect
{
    private int _currentBloom;

    public OverexposureEffect(VolumetricShadingMod mod)
    {
        mod.CApi.Settings.AddWatcher("volumetricshading_sunBloomIntensity",
            (OnSettingsChanged<int>)OnSunBloomChanged);
        _currentBloom = ModSettings.SunBloomIntensity;
        mod.Events.PreSunRender += OnRenderSun;
        RegisterInjectorProperties(mod);
    }

    private void RegisterInjectorProperties(VolumetricShadingMod mod)
    {
        var shaderInjector = mod.ShaderInjector;
        shaderInjector.RegisterFloatProperty("VSMOD_OVEREXPOSURE",
            () => ModSettings.OverexposureIntensity * 0.01f);
        shaderInjector.RegisterBoolProperty("VSMOD_OVEREXPOSURE_ENABLED", () => ModSettings.OverexposureIntensity > 0);
    }

    private void OnSunBloomChanged(int bloom)
    {
        _currentBloom = bloom;
    }

    public void OnRenderSun(ShaderProgramStandard shader)
    {
        if (shader == null) return;
        
        try {
            // Check if uniform exists before setting it
            if (HasUniform(shader, "extraOutGlow")) {
                shader.Uniform("extraOutGlow", _currentBloom * 0.01f);
            }
        } catch (Exception ex) {
            // Log error but don't crash the game
            VolumetricShadingMod.Instance?.Mod.Logger.Warning($"Error setting sun bloom: {ex.Message}");
        }
    }
    
    private bool HasUniform(ShaderProgramBase shader, string name)
    {
        // Use reflection to safely check if uniform exists
        try {
            var uniformLocationsField = typeof(ShaderProgramBase).GetField("uniformLocations", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                
            if (uniformLocationsField != null) {
                var uniformLocations = uniformLocationsField.GetValue(shader) as System.Collections.Generic.Dictionary<string, int>;
                return uniformLocations != null && uniformLocations.ContainsKey(name);
            }
        } catch {}
        
        return false;
    }

    public void OnRenderedSun()
    {
        try {
            var standard = ShaderPrograms.Standard;
            standard.Use();
            
            // Check if uniform exists
            if (HasUniform(standard, "extraOutGlow")) {
                standard.Uniform("extraOutGlow", 0f);
            }
            
            standard.Stop();
        } catch (Exception ex) {
            // Log error but don't crash
            VolumetricShadingMod.Instance?.Mod.Logger.Warning($"Error resetting sun bloom: {ex.Message}");
        }
    }
}