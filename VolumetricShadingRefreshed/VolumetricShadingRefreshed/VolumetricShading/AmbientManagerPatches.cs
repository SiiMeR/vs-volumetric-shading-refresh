using HarmonyLib;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading;

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