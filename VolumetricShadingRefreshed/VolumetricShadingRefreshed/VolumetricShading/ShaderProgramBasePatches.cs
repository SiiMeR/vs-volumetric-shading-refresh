using HarmonyLib;
using Vintagestory.Client.NoObf;

namespace VolumetricShadingRefreshed.VolumetricShading;

[HarmonyPatch(typeof(ShaderProgramBase))]
internal class ShaderProgramBasePatches
{
    [HarmonyPatch("Use")]
    [HarmonyPostfix]
    public static void UsePostfix(ShaderProgramBase __instance)
    {
        VolumetricShadingMod.Instance.Events.EmitPostUseShader(__instance);
    }
}