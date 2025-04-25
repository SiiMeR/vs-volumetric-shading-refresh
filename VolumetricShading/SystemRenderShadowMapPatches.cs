using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading
{
    /// <summary>
    /// • Replaces the literal 16f → 32f in PrepareForShadowRendering
    /// • Calls GetNearShadowBaseWidth() every frame (value is discarded)
    /// </summary>
    [HarmonyPatch(typeof(SystemRenderShadowMap), "OnRenderShadowNear")]
    internal static class SystemRenderShadowMapPatches
    {
        // --------------------------------------------------------------------
        //  TRANSPILER
        // --------------------------------------------------------------------
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> source)
        {
            // Copy to list so we can insert/replace
            var codes = new List<CodeInstruction>(source);

            //-----------------------------------------------------------------
            // 1) Replace ldc.r4 16f  →  32f
            //-----------------------------------------------------------------
            bool replaced = false;
            for (int i = 0; i < codes.Count; i++)
            {
                var c = codes[i];
                if (c.opcode == OpCodes.Ldc_R4 &&
                    c.operand is float f && Math.Abs(f - 16f) < 0.0001f)
                {
                    codes[i] = new CodeInstruction(OpCodes.Ldc_R4, 32f);
                    replaced = true;
                    break;                        // only touch the first
                }
            }
            if (!replaced)
                throw new Exception("Could not find 16f constant to patch.");

            //-----------------------------------------------------------------
            // 2) Inject our call *right after* PrepareForShadowRendering(...)
            //-----------------------------------------------------------------
            var prepareMI = typeof(SystemRenderShadowMap)
                            .GetMethod("PrepareForShadowRendering",
                                       BindingFlags.Instance | BindingFlags.NonPublic);

            var callsiteMI = AccessTools.Method(typeof(SystemRenderShadowMapPatches),
                                                nameof(GetNearShadowBaseWidth));

            bool injected = false;
            for (int i = 0; i < codes.Count; i++)
            {
                if (codes[i].Calls(prepareMI))
                {
                    // insert after the call → i+1
                    codes.Insert(++i, new CodeInstruction(OpCodes.Call, callsiteMI));
                    codes.Insert(++i, new CodeInstruction(OpCodes.Pop)); // discard int
                    injected = true;
                    break;
                }
            }
            if (!injected)
                throw new Exception("Could not find PrepareForShadowRendering call to inject after.");

            return codes;
        }

        //----------------------------------------------------------------------
        //  Helper that fetches the value you want; result is currently unused
        //----------------------------------------------------------------------
        private static int GetNearShadowBaseWidth()
        {
            return VolumetricShadingMod.Instance.ShadowTweaks.NearShadowBaseWidth;
        }
    }
}
