using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading;

[HarmonyPatch(typeof(SystemRenderShadowMap), "OnRenderShadowNear")]
internal static class SystemRenderShadowMapPatches
{
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> src)
    {
        var codes = new List<CodeInstruction>(src);

        // Replace ldc.r4 16f → 32f
        var constantPatched = false;
        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].opcode == OpCodes.Ldc_R4 &&
                codes[i].operand is float f && Math.Abs(f - 16f) < 0.0001f)
            {
                codes[i] = new CodeInstruction(OpCodes.Ldc_R4, 32f);
                constantPatched = true;
                break;
            }
        }

        if (!constantPatched)
        {
            throw new Exception("[VSR] Could not find 16f literal to patch (shadow near z-extend).");
        }

        var prepareMI = typeof(SystemRenderShadowMap).GetMethod(
            "PrepareForShadowRendering",
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(double), typeof(EnumFrameBuffer), typeof(float) }, // explicit!
            null);

        var callsiteMI = AccessTools.Method(typeof(SystemRenderShadowMapPatches),
            nameof(GetNearShadowBaseWidth));

        var callInjected = false;
        for (var i = 0; i < codes.Count; i++)
        {
            if (codes[i].Calls(prepareMI))
            {
                codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, callsiteMI));
                codes.Insert(i + 2, new CodeInstruction(OpCodes.Pop)); // discard returned int
                callInjected = true;
                break;
            }
        }

        if (!callInjected)
        {
            throw new Exception("[VSR] PrepareForShadowRendering() call not found – injection failed.");
        }

        return codes;
    }

    private static int GetNearShadowBaseWidth()
    {
        return VolumetricShadingMod.Instance?.ShadowTweaks?.NearShadowBaseWidth ?? 0;
    }
}