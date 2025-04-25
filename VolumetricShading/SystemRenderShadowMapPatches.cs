using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.Client.NoObf;

namespace VolumetricShading;

[HarmonyPatch(typeof(SystemRenderShadowMap))]
internal class SystemRenderShadowMapPatches
{
	private static readonly MethodInfo OnRenderShadowNearBaseWidthCallsiteMethod;

	private static readonly MethodInfo PrepareForShadowRenderingMethod;

	[HarmonyPatch("OnRenderShadowNear")]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> OnRenderShadowNearBaseWidthTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		bool injected = false;
		foreach (CodeInstruction ins in instructions)
		{
			if (!injected && ins.opcode == OpCodes.Ret)
			{
				yield return new CodeInstruction(OpCodes.Ldarg_0, (object)null);
				yield return new CodeInstruction(OpCodes.Call, (object)OnRenderShadowNearBaseWidthCallsiteMethod);
				injected = true;
			}
			yield return ins;
		}
		if (!injected)
		{
			throw new Exception("Failed to patch OnRenderShadowNear – no 'ret' found.");
		}
	}

	public static int OnRenderShadowNearBaseWidthCallsite()
	{
		return VolumetricShadingMod.Instance.ShadowTweaks.NearShadowBaseWidth;
	}

	[HarmonyPatch("OnRenderShadowNear")]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> OnRenderShadowNearZExtend(IEnumerable<CodeInstruction> instructions)
	{
		bool patched = false;
		foreach (CodeInstruction instruction in instructions)
		{
			if (!patched && instruction.opcode == OpCodes.Ldc_R4 && (float)instruction.operand == 16f)
			{
				yield return new CodeInstruction(OpCodes.Ldc_R4, (object)32f);
				patched = true;
			}
			else
			{
				yield return instruction;
			}
		}
		if (!patched)
		{
			throw new Exception("Couldn't find 16f argument to patch.");
		}
	}

	static SystemRenderShadowMapPatches()
	{
		OnRenderShadowNearBaseWidthCallsiteMethod = typeof(SystemRenderShadowMapPatches).GetMethod("OnRenderShadowNearBaseWidthCallsite");
		PrepareForShadowRenderingMethod = typeof(SystemRenderShadowMap).GetMethod("PrepareForShadowRendering", BindingFlags.Instance | BindingFlags.NonPublic);
	}
}
