using System;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using volumetricshadingupdated.VolumetricShading.Patch;

namespace volumetricshadingupdated.VolumetricShading.Effects;

public class VolumetricLighting
{
    private readonly FieldInfo _dropShadowIntensityField;

    private readonly ClientMain _game;
    private readonly VolumetricShadingMod _mod;

    private bool _enabled;

    public VolumetricLighting(VolumetricShadingMod mod)
    {
        _mod = mod;
        _game = _mod.CApi.GetClient();
        _dropShadowIntensityField =
            typeof(AmbientManager).GetField("DropShadowIntensity", BindingFlags.Instance | BindingFlags.NonPublic);
        _enabled = ClientSettings.GodRayQuality > 0;
        _mod.CApi.Settings.AddWatcher("shadowMapQuality", (OnSettingsChanged<int>)OnShadowMapChanged);
        _mod.CApi.Settings.AddWatcher("godRays", (OnSettingsChanged<int>)OnGodRaysChanged);
        _mod.Events.PreGodraysRender += OnSetGodrayUniforms;
        _mod.Events.PostUseShader += OnPostUseShader;
        RegisterPatches();
    }

    private void RegisterPatches()
    {
        var shaderInjector = _mod.ShaderInjector;
        shaderInjector.RegisterFloatProperty("VOLUMETRIC_FLATNESS", delegate
        {
            var volumetricLightingFlatness = ModSettings.VolumetricLightingFlatness;
            return (200 - volumetricLightingFlatness) * 0.01f;
        });
        shaderInjector.RegisterFloatProperty("VOLUMETRIC_INTENSITY",
            () => ModSettings.VolumetricLightingIntensity * 0.01f);
    }

    private static void OnShadowMapChanged(int quality)
    {
        if (quality == 0)
        {
            ClientSettings.GodRayQuality = 0;
        }
    }

    private void OnGodRaysChanged(int quality)
    {
        _enabled = quality > 0;
        if (quality == 1 && ClientSettings.ShadowMapQuality == 0)
        {
            ClientSettings.ShadowMapQuality = 1;
            _mod.CApi.GetClientPlatformAbstract().RebuildFrameBuffers();
        }
    }

    public void OnSetGodrayUniforms(ShaderProgramGodrays rays)
    {
        var calendar = _mod.CApi.World.Calendar;
        var ambient = _mod.CApi.Ambient;
        _ = _mod.CApi.Render.ShaderUniforms;
        var uniforms = _mod.Uniforms;
        var obj = _dropShadowIntensityField?.GetValue(_mod.CApi.Ambient);
        if (obj == null)
        {
            _mod.Mod.Logger.Fatal("DropShadowIntensity not found!");
            return;
        }

        var num = (float)obj;
        var eyesInWaterDepth = _game.playerProperties.EyesInWaterDepth;
        TrySetUniform(rays, "moonLightStrength", calendar.MoonLightStrength);
        TrySetUniform(rays, "sunLightStrength", calendar.SunLightStrength);
        TrySetUniform(rays, "dayLightStrength", calendar.DayLightStrength);
        TrySetUniform(rays, "shadowIntensity", num);
        TrySetUniform(rays, "flatFogDensity", ambient.BlendedFlatFogDensity);
        TrySetUniform(rays, "playerWaterDepth", eyesInWaterDepth);
        TrySetUniform(rays, "fogColor", ambient.BlendedFogColor);
        TrySetUniformMatrix(rays, "invProjectionMatrix", uniforms.InvProjectionMatrix);
        TrySetUniformMatrix(rays, "invModelViewMatrix", uniforms.InvModelViewMatrix);
    }

    private void OnPostUseShader(ShaderProgramBase shader)
    {
        if (_enabled && shader.includes.Contains("shadowcoords.vsh"))
        {
            TrySetUniform(shader, "cameraWorldPosition", _mod.Uniforms.CameraWorldPosition);
        }
    }

    /// <summary>
    /// Safe uniform setting with error handling to prevent KeyNotFoundException crashes
    /// </summary>
    private void TrySetUniform(IShaderProgram shader, string uniformName, float value)
    {
        try
        {
            shader.Uniform(uniformName, value);
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            // Uniform doesn't exist in shader, silently ignore
            _mod.Mod.Logger.Debug($"Volumetric uniform '{uniformName}' not found in shader, skipping");
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Warning($"Failed to set volumetric uniform '{uniformName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Safe uniform setting for Vec4f values
    /// </summary>
    private void TrySetUniform(IShaderProgram shader, string uniformName, Vec4f value)
    {
        try
        {
            shader.Uniform(uniformName, value);
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            // Uniform doesn't exist in shader, silently ignore
            _mod.Mod.Logger.Debug($"Volumetric uniform '{uniformName}' not found in shader, skipping");
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Warning($"Failed to set volumetric uniform '{uniformName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Safe uniform matrix setting with error handling
    /// </summary>
    private void TrySetUniformMatrix(IShaderProgram shader, string uniformName, float[] matrix)
    {
        try
        {
            shader.UniformMatrix(uniformName, matrix);
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            // Uniform doesn't exist in shader, silently ignore
            _mod.Mod.Logger.Debug($"Volumetric matrix uniform '{uniformName}' not found in shader, skipping");
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Warning($"Failed to set volumetric matrix uniform '{uniformName}': {ex.Message}");
        }
    }
}