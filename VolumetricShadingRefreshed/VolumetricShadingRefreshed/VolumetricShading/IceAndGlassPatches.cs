using HarmonyLib;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading;

[HarmonyPatch]
internal class IceAndGlassPatches
{
    [HarmonyPatch(typeof(CubeTesselator), "Tesselate")]
    [HarmonyPrefix]
    public static void CubeTesselator(ref TCTCache vars)
    {
        Tesselate(ref vars);
    }

    [HarmonyPatch(typeof(LiquidTesselator), "Tesselate")]
    [HarmonyPrefix]
    public static void LiquidTesselator(ref TCTCache vars)
    {
        Tesselate(ref vars);
    }

    [HarmonyPatch(typeof(TopsoilTesselator), "Tesselate")]
    [HarmonyPrefix]
    public static void TopsoilTesselator(ref TCTCache vars)
    {
        Tesselate(ref vars);
    }

    [HarmonyPatch(typeof(JsonTesselator), "Tesselate")]
    [HarmonyPrefix]
    public static void JsonTesselator(ref TCTCache vars)
    {
        Tesselate(ref vars);
    }

    [HarmonyPatch(typeof(JsonAndSnowLayerTesselator), "Tesselate")]
    [HarmonyPrefix]
    public static void JsonAndSnowLayerTesselator(ref TCTCache vars)
    {
        Tesselate(ref vars);
    }

    [HarmonyPatch(typeof(JsonAndLiquidTesselator), "Tesselate")]
    [HarmonyPrefix]
    public static void JsonAndLiquidTesselator(ref TCTCache vars)
    {
        Tesselate(ref vars);
    }

    public static void Tesselate(ref TCTCache vars)
    {
        //IL_0007: Unknown result type (might be due to invalid IL or missing references)
        //IL_000e: Invalid comparison between Unknown and I4
        //IL_0017: Unknown result type (might be due to invalid IL or missing references)
        //IL_001e: Invalid comparison between Unknown and I4
        if ((int)vars.block.BlockMaterial == 10 || (int)vars.block.BlockMaterial == 14)
        {
            var obj = vars;
            obj.VertexFlags |= 0x800;
        }
    }
}