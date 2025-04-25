using VolumetricShading.Patch;

namespace VolumetricShading.Effects;

public class ScreenSpaceDirectionalOcclusion
{
	public ScreenSpaceDirectionalOcclusion(VolumetricShadingMod mod)
	{
		mod.ShaderInjector.RegisterBoolProperty("SSDO", () => ModSettings.SSDOEnabled);
	}
}
