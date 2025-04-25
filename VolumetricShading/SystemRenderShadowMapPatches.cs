using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading
{
    /// Patches SystemRenderShadowMap.OnRenderShadowNear
    ///   • Replaces the hard-coded 16f with 32f
    ///   • Invokes GetNearShadowBaseWidth() once each frame
    [HarmonyPatch(typeof(SystemRenderShadowMap), "OnRenderShadowNear")]
    internal static class SystemRenderShadowMapPatches
    {
        // ----------------------------  TRANSPILER  ----------------------------
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> src)
        {
            var codes = new List<CodeInstruction>(src);

            //-----------------------------------------------------------------
            // 1) Replace ldc.r4 16f → 32f
            //-----------------------------------------------------------------
            bool constantPatched = false;
            for (int i = 0; i < codes.Count; i++)
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
                throw new Exception("[VSR] Could not find 16f literal to patch (shadow near z-extend).");

            //-----------------------------------------------------------------
            // 2) Insert our call immediately AFTER PrepareForShadowRendering(...)
            //-----------------------------------------------------------------
            var prepareMI = typeof(SystemRenderShadowMap).GetMethod(
                                "PrepareForShadowRendering",
                                BindingFlags.Instance | BindingFlags.NonPublic,
                                null,
                                new Type[] { typeof(double), typeof(EnumFrameBuffer), typeof(float) },   // explicit!
                                null);

            var callsiteMI = AccessTools.Method(typeof(SystemRenderShadowMapPatches),
                                                nameof(GetNearShadowBaseWidth));

            bool callInjected = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(prepareMI))
                {
                    // insert AFTER the call (i points to the call itself)
                    codes.Insert(i + 1, new CodeInstruction(OpCodes.Call, callsiteMI));
                    codes.Insert(i + 2, new CodeInstruction(OpCodes.Pop));   // discard returned int
                    callInjected = true;
                    break;
                }
            }
            if (!callInjected)
                throw new Exception("[VSR] PrepareForShadowRendering() call not found – injection failed.");

            return codes;
        }

        // ------------- helper (fires an event / has side-effects in your mod) -------------
        private static int GetNearShadowBaseWidth()
        {
            // If ShadowTweaks or the mod instance could ever be null, guard here.
            return VolumetricShadingMod.Instance?.ShadowTweaks?.NearShadowBaseWidth ?? 0;
        }
    }
}
