using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading;

[HarmonyPatch(typeof(SystemRenderSunMoon))]
internal class SunMoonPatches
{
    private static readonly MethodInfo StandardShaderTextureSetter =
        typeof(ShaderProgramStandard).GetProperty("Tex2D")?.GetSetMethod();

    private static readonly MethodInfo AddRenderFlagsSetter =
        typeof(ShaderProgramStandard).GetProperty("AddRenderFlags")?.GetSetMethod();

    private static readonly MethodInfo RenderCallsiteMethod = typeof(SunMoonPatches).GetMethod("RenderCallsite");

    [HarmonyPatch("OnRenderFrame3D")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> RenderTranspiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator gen)
    {
        // we’ll need a temp local to hold the duplicate
        LocalBuilder shaderLocal = gen.DeclareLocal(typeof(ShaderProgramStandard));

        foreach (var ins in instructions)
        {
            if (CodeInstructionExtensions.Calls(ins, StandardShaderTextureSetter))
            {
                // Stack right now:  ShaderProgramStandard  (shader)   int (texture unit)

                yield return new CodeInstruction(OpCodes.Dup);                  // dup int
                yield return new CodeInstruction(OpCodes.Pop);                  // discard duplicated int
                yield return new CodeInstruction(OpCodes.Dup);                  // dup shader
                yield return new CodeInstruction(OpCodes.Stloc, shaderLocal);   // save copy -> local
                //    (stack unchanged:  shader  int)
            }

            yield return ins;   // original instruction – the property setter consumes shader+int

            if (CodeInstructionExtensions.Calls(ins, StandardShaderTextureSetter))
            {
                // after the call the stack is empty → load our saved shader and call the hook
                yield return new CodeInstruction(OpCodes.Ldloc, shaderLocal);
                yield return new CodeInstruction(OpCodes.Call, RenderCallsiteMethod);
            }
        }
    }

    [HarmonyPatch("OnRenderFrame3DPost")]
    [HarmonyPostfix]
    public static void RenderPostPostfix()
    {
        VolumetricShadingMod.Instance.OverexposureEffect.OnRenderedSun();
    }

    [HarmonyPatch("OnRenderFrame3D")]
    [HarmonyPostfix]
    public static void RenderPostfix()
    {
        VolumetricShadingMod.Instance.OverexposureEffect.OnRenderedSun();
    }

    [HarmonyPatch("OnRenderFrame3DPost")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> RenderPostTranspiler(IEnumerable<CodeInstruction> instructions,
        ILGenerator generator)
    {
        bool found = false;
        CodeInstruction[] previousInstructions = (CodeInstruction[])(object)new CodeInstruction[2];
        foreach (CodeInstruction instruction in instructions)
        {
            CodeInstruction currentOld = previousInstructions[1];
            yield return instruction;
            previousInstructions[1] = previousInstructions[0];
            previousInstructions[0] = instruction;
            if (CodeInstructionExtensions.Calls(instruction, AddRenderFlagsSetter))
            {
                yield return currentOld;
                yield return new CodeInstruction(OpCodes.Call, (object)RenderCallsiteMethod);
                found = true;
            }
        }

        if (!found)
        {
            throw new Exception("Could not patch RenderFinalComposition!");
        }
    }

    public static void RenderCallsite(ShaderProgramStandard standard)
    {
        VolumetricShadingMod.Instance.Events.EmitPreSunRender(standard);
    }
}