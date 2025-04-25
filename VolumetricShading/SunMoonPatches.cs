using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.Client.NoObf;

namespace VolumetricShading;

[HarmonyPatch(typeof(SystemRenderSunMoon))]
internal class SunMoonPatches
{
	private static readonly MethodInfo StandardShaderTextureSetter = typeof(ShaderProgramStandard).GetProperty("Tex2D")?.GetSetMethod();

	private static readonly MethodInfo AddRenderFlagsSetter = typeof(ShaderProgramStandard).GetProperty("AddRenderFlags")?.GetSetMethod();

	private static readonly MethodInfo RenderCallsiteMethod = typeof(SunMoonPatches).GetMethod("RenderCallsite");

	[HarmonyPatch("OnRenderFrame3D")]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> RenderTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
	{
		bool found = false;
		foreach (CodeInstruction instruction in instructions)
		{
			yield return instruction;
			if (CodeInstructionExtensions.Calls(instruction, StandardShaderTextureSetter))
			{
				yield return new CodeInstruction(OpCodes.Dup, (object)null);
				yield return new CodeInstruction(OpCodes.Call, (object)RenderCallsiteMethod);
				found = true;
			}
		}
		if (!found)
		{
			throw new Exception("Could not patch RenderPostprocessingEffects!");
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
	public static IEnumerable<CodeInstruction> RenderPostTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
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
