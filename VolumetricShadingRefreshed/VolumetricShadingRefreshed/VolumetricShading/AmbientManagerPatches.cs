using HarmonyLib;
using Vintagestory.Client.NoObf;

namespace VolumetricShadingRefreshed.VolumetricShading;

[HarmonyPatch(typeof(AmbientManager))]
internal class AmbientManagerPatches
{
    [HarmonyPatch("OnPlayerSightBeingChangedByWater")]
    [HarmonyPostfix]
    public static void OnPlayerSightBeingChangedByWaterPostfix()
    {
        VolumetricShadingMod.Instance.Events.EmitPostWaterChangeSight();
    }
}