using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;
using volumetricshadingupdated.VolumetricShading.Patch;

namespace volumetricshadingupdated.VolumetricShading.Effects;

public class OverexposureEffect
{
    private int _currentBloom;

    public OverexposureEffect(VolumetricShadingMod mod)
    {
        mod.CApi.Settings.AddWatcher<int>("volumetricshading_sunBloomIntensity",
            (OnSettingsChanged<int>)OnSunBloomChanged);
        _currentBloom = ModSettings.SunBloomIntensity;
        mod.Events.PreSunRender += OnRenderSun;
        RegisterInjectorProperties(mod);
    }

    private void RegisterInjectorProperties(VolumetricShadingMod mod)
    {
        ShaderInjector shaderInjector = mod.ShaderInjector;
        shaderInjector.RegisterFloatProperty("VSMOD_OVEREXPOSURE",
            () => (float)ModSettings.OverexposureIntensity * 0.01f);
        shaderInjector.RegisterBoolProperty("VSMOD_OVEREXPOSURE_ENABLED", () => ModSettings.OverexposureIntensity > 0);
    }

    private void OnSunBloomChanged(int bloom)
    {
        _currentBloom = bloom;
    }

    public void OnRenderSun(ShaderProgramStandard shader)
    {
        ((ShaderProgramBase)shader)?.Uniform("extraOutGlow", _currentBloom * 0.01f);
    }

    public void OnRenderedSun()
    {
        ShaderProgramStandard standard = ShaderPrograms.Standard;
        ((ShaderProgramBase)standard).Use();
        ((ShaderProgramBase)standard).Uniform("extraOutGlow", 0f);
        ((ShaderProgramBase)standard).Stop();
    }
}