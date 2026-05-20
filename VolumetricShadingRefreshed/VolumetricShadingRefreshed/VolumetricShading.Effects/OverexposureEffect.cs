using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;
using VolumetricShadingRefreshed.VolumetricShading.Patch;

namespace VolumetricShadingRefreshed.VolumetricShading.Effects;

public class OverexposureEffect
{
    public OverexposureEffect(VolumetricShadingMod mod)
    {
        mod.Events.PreSunRender += OnRenderSun;
        mod.Events.PostUseShader += OnPostUseShader;
        mod.ShaderInjector.RegisterFloatProperty("VSMOD_OVEREXPOSURE", () => ModSettings.OverexposureIntensity * 0.01f);
        mod.ShaderInjector.RegisterBoolProperty("VSMOD_OVEREXPOSURE_ENABLED", () => ModSettings.OverexposureIntensity > 0);
    }

    private void OnPostUseShader(ShaderProgramBase shader)
    {
    }

    public void OnRenderSun(ShaderProgramStandard shader)
    {
        shader?.Uniform("extraOutGlow", ModSettings.SunBloomIntensity * 0.01f);
    }

    public void OnRenderedSun()
    {
        var standard = ShaderPrograms.Standard;
        standard.Use();
        standard.Uniform("extraOutGlow", 0f);
        standard.Stop();
    }
}
