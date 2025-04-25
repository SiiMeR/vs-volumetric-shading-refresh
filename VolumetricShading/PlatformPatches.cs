using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShading;

[HarmonyPatch(typeof(ClientPlatformWindows))]
internal class PlatformPatches
{
	private static readonly MethodInfo PlayerViewVectorSetter = typeof(ShaderProgramGodrays).GetProperty("PlayerViewVector")?.GetSetMethod();

	private static readonly MethodInfo GodrayCallsiteMethod = typeof(PlatformPatches).GetMethod("GodrayCallsite");

	private static readonly MethodInfo PrimaryScene2DSetter = typeof(ShaderProgramFinal).GetProperty("PrimaryScene2D")?.GetSetMethod();

	private static readonly MethodInfo FinalCallsiteMethod = typeof(PlatformPatches).GetMethod("FinalCallsite");

	[HarmonyPatch("RenderPostprocessingEffects")]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> PostprocessingTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		bool found = false;
		foreach (CodeInstruction instruction in instructions)
		{
			yield return instruction;
			if (CodeInstructionExtensions.Calls(instruction, PlayerViewVectorSetter))
			{
				yield return new CodeInstruction(OpCodes.Dup, (object)null);
				yield return new CodeInstruction(OpCodes.Call, (object)GodrayCallsiteMethod);
				found = true;
			}
		}
		if (!found)
		{
			throw new Exception("Could not patch RenderPostprocessingEffects!");
		}
	}

	public static void GodrayCallsite(ShaderProgramGodrays rays)
	{
		VolumetricShadingMod.Instance.Events.EmitPreGodraysRender(rays);
	}

	[HarmonyPatch("RenderFinalComposition")]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> FinalTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		bool found = false;
		CodeInstruction[] previousInstructions = (CodeInstruction[])(object)new CodeInstruction[2];
		foreach (CodeInstruction instruction in instructions)
		{
			CodeInstruction currentOld = previousInstructions[1];
			yield return instruction;
			previousInstructions[1] = previousInstructions[0];
			previousInstructions[0] = instruction;
			if (CodeInstructionExtensions.Calls(instruction, PrimaryScene2DSetter))
			{
				yield return currentOld;
				yield return new CodeInstruction(OpCodes.Call, (object)FinalCallsiteMethod);
				found = true;
			}
		}
		if (!found)
		{
			throw new Exception("Could not patch RenderFinalComposition!");
		}
	}

	public static void FinalCallsite(ShaderProgramFinal final)
	{
		VolumetricShadingMod.Instance.Events.EmitPreFinalRender(final);
	}

	[HarmonyPatch("SetupDefaultFrameBuffers")]
	[HarmonyPostfix]
	public static void SetupDefaultFrameBuffersPostfix(List<FrameBufferRef> __result)
	{
		VolumetricShadingMod.Instance.Events.EmitRebuildFramebuffers(__result);
	}
}
