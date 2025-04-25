using volumetricshadingupdated.VolumetricShading.Patch;

namespace volumetricshadingupdated.VolumetricShading.Effects;

public class ScreenSpaceDirectionalOcclusion
{
    public ScreenSpaceDirectionalOcclusion(VolumetricShadingMod mod)
    {
        mod.ShaderInjector.RegisterBoolProperty("SSDO", () => ModSettings.SSDOEnabled);
    }
}