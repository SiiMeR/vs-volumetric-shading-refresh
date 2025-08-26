using System.Reflection;
using Vintagestory.API.Client;
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
        rays.Uniform("moonLightStrength", calendar.MoonLightStrength);
        rays.Uniform("sunLightStrength", calendar.SunLightStrength);
        rays.Uniform("dayLightStrength", calendar.DayLightStrength);
        rays.Uniform("shadowIntensity", num);
        rays.Uniform("flatFogDensity", ambient.BlendedFlatFogDensity);
        rays.Uniform("playerWaterDepth", eyesInWaterDepth);
        rays.Uniform("fogColor", ambient.BlendedFogColor);
        rays.UniformMatrix("invProjectionMatrix", uniforms.InvProjectionMatrix);
        rays.UniformMatrix("invModelViewMatrix", uniforms.InvModelViewMatrix);
    }

    private void OnPostUseShader(ShaderProgramBase shader)
    {
        if (_enabled && shader.includes.Contains("shadowcoords.vsh"))
        {
            shader.Uniform("cameraWorldPosition", _mod.Uniforms.CameraWorldPosition);
        }
    }
}