using VolumetricShadingRefreshed.VolumetricShading.Patch;

namespace VolumetricShadingRefreshed.VolumetricShading.Effects;

public class ScreenSpaceDirectionalOcclusion
{
    public ScreenSpaceDirectionalOcclusion(VolumetricShadingMod mod)
    {
        mod.ShaderInjector.RegisterBoolProperty("SSDO", () => ModSettings.SSDOEnabled);
    }
}