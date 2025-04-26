using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading
{
    /// <summary>
    /// Injects Volumetric-Shading callbacks into the two sun draw passes.
    /// </summary>
    [HarmonyPatch(typeof(SystemRenderSunMoon))]
    internal class SunMoonPatches
    {
        // ———————————————————————————————————————————————————————————— //
        //   cached reflection handles
        // ———————————————————————————————————————————————————————————— //
        private static readonly MethodInfo SetterTex2D =
            typeof(ShaderProgramStandard)
                .GetProperty(nameof(ShaderProgramStandard.Tex2D))!
                .GetSetMethod()!;

        private static readonly MethodInfo SetterAddFlags =
            typeof(ShaderProgramStandard)
                .GetProperty(nameof(ShaderProgramStandard.AddRenderFlags))!
                .GetSetMethod()!;

        private static readonly MethodInfo Callsite =
            typeof(SunMoonPatches)
                .GetMethod(nameof(RenderCallsite),
                           BindingFlags.Static | BindingFlags.NonPublic)!;

        // ====================================================================
        //  1)  OnRenderFrame3D  – first sun pass
        // ====================================================================
        [HarmonyPatch("OnRenderFrame3D")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpile_Render(
            IEnumerable<CodeInstruction> src)
        {
            var buf = new CodeInstruction[4];
            bool injected = false;

            foreach (var il in src)
            {
                yield return il;

                buf[3] = buf[2];
                buf[2] = buf[1];
                buf[1] = buf[0];
                buf[0] = il;

                if (CodeInstructionExtensions.Calls(il, SetterTex2D))
                {
                    // buf[3] == ldloc.s prog   (ShaderProgramStandard)
                    yield return buf[3].Clone();
                    yield return new CodeInstruction(OpCodes.Call, Callsite);
                    injected = true;
                }
            }

            if (!injected)
                throw new Exception("[VolumetricShading] Tex2D setter not found – patch failed");
        }

        // ====================================================================
        //  2)  OnRenderFrame3DPost  – final composition pass
        // ====================================================================
        [HarmonyPatch("OnRenderFrame3DPost")]
        [HarmonyTranspiler]
        private static IEnumerable<CodeInstruction> Transpile_RenderPost(
            IEnumerable<CodeInstruction> src)
        {
            var prev = new CodeInstruction[2];   // two-step look-back
            bool injected = false;

            foreach (var il in src)
            {
                var twoBack = prev[1];
                yield return il;

                prev[1] = prev[0];
                prev[0] = il;

                if (CodeInstructionExtensions.Calls(il, SetterAddFlags))
                {
                    // twoBack = ldloc.s prog  (shader instance)
                    yield return twoBack.Clone();
                    yield return new CodeInstruction(OpCodes.Call, Callsite);
                    injected = true;
                }
            }

            if (!injected)
                throw new Exception("[VolumetricShading] AddRenderFlags setter not found – patch failed");
        }

        // ====================================================================
        //  3)  Postfixes to reset glow state after each pass
        // ====================================================================
        [HarmonyPatch("OnRenderFrame3D")]
        [HarmonyPostfix]
        private static void AfterRender() =>
            VolumetricShadingMod.Instance.OverexposureEffect.OnRenderedSun();

        [HarmonyPatch("OnRenderFrame3DPost")]
        [HarmonyPostfix]
        private static void AfterRenderPost() =>
            VolumetricShadingMod.Instance.OverexposureEffect.OnRenderedSun();

        // ====================================================================
        //  4)  Method invoked from injected IL
        // ====================================================================
        private static void RenderCallsite(ShaderProgramStandard shader) =>
            VolumetricShadingMod.Instance.Events.EmitPreSunRender(shader);
    }
}
