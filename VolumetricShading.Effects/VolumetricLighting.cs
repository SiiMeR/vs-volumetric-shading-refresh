using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;
using volumetricshadingupdated.VolumetricShading.Patch;

namespace volumetricshadingupdated.VolumetricShading.Effects;

public class VolumetricLighting
{
    private readonly VolumetricShadingMod _mod;

    private readonly ClientMain _game;

    private readonly FieldInfo _dropShadowIntensityField;

    private bool _enabled;

    public VolumetricLighting(VolumetricShadingMod mod)
    {
        _mod = mod;
        _game = _mod.CApi.GetClient();
        _dropShadowIntensityField =
            typeof(AmbientManager).GetField("DropShadowIntensity", BindingFlags.Instance | BindingFlags.NonPublic);
        _enabled = ClientSettings.GodRayQuality > 0;
        _mod.CApi.Settings.AddWatcher<int>("shadowMapQuality", (OnSettingsChanged<int>)OnShadowMapChanged);
        _mod.CApi.Settings.AddWatcher<int>("godRays", (OnSettingsChanged<int>)OnGodRaysChanged);
        _mod.Events.PreGodraysRender += OnSetGodrayUniforms;
        _mod.Events.PostUseShader += OnPostUseShader;
        RegisterPatches();
    }

    private void RegisterPatches()
    {
        ShaderInjector shaderInjector = _mod.ShaderInjector;
        shaderInjector.RegisterFloatProperty("VOLUMETRIC_FLATNESS", delegate
        {
            int volumetricLightingFlatness = ModSettings.VolumetricLightingFlatness;
            return (float)(200 - volumetricLightingFlatness) * 0.01f;
        });
        shaderInjector.RegisterFloatProperty("VOLUMETRIC_INTENSITY",
            () => (float)ModSettings.VolumetricLightingIntensity * 0.01f);
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
        IClientGameCalendar calendar = _mod.CApi.World.Calendar;
        IAmbientManager ambient = _mod.CApi.Ambient;
        _ = _mod.CApi.Render.ShaderUniforms;
        Uniforms uniforms = _mod.Uniforms;
        object obj = _dropShadowIntensityField?.GetValue(_mod.CApi.Ambient);
        if (obj == null)
        {
            ((ModSystem)_mod).Mod.Logger.Fatal("DropShadowIntensity not found!");
            return;
        }

        float num = (float)obj;
        float eyesInWaterDepth = _game.playerProperties.EyesInWaterDepth;
        ((ShaderProgramBase)rays).Uniform("moonLightStrength", calendar.MoonLightStrength);
        ((ShaderProgramBase)rays).Uniform("sunLightStrength", calendar.SunLightStrength);
        ((ShaderProgramBase)rays).Uniform("dayLightStrength", calendar.DayLightStrength);
        ((ShaderProgramBase)rays).Uniform("shadowIntensity", num);
        ((ShaderProgramBase)rays).Uniform("flatFogDensity", ambient.BlendedFlatFogDensity);
        ((ShaderProgramBase)rays).Uniform("playerWaterDepth", eyesInWaterDepth);
        ((ShaderProgramBase)rays).Uniform("fogColor", ambient.BlendedFogColor);
        ((ShaderProgramBase)rays).UniformMatrix("invProjectionMatrix", uniforms.InvProjectionMatrix);
        ((ShaderProgramBase)rays).UniformMatrix("invModelViewMatrix", uniforms.InvModelViewMatrix);
    }

    private void OnPostUseShader(ShaderProgramBase shader)
    {
        if (_enabled && shader.includes.Contains("shadowcoords.vsh"))
        {
            shader.Uniform("cameraWorldPosition", _mod.Uniforms.CameraWorldPosition);
        }
    }
}