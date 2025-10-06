using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;
using VolumetricShadingRefreshed.VolumetricShading.Patch;

namespace VolumetricShadingRefreshed.VolumetricShading.Effects;

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
        shader?.Uniform("extraOutGlow", _currentBloom * 0.01f);
    }

    public void OnRenderedSun()
    {
        var standard = ShaderPrograms.Standard;
        standard.Use();
        standard.Uniform("extraOutGlow", 0f);
        standard.Stop();
    }
}