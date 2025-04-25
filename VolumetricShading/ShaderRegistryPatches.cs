using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShading;

[HarmonyPatch(typeof(ShaderRegistry))]
internal class ShaderRegistryPatches
{
	private static readonly MethodInfo HandleIncludesMethod = typeof(ShaderRegistry).GetMethod("HandleIncludes", BindingFlags.Static | BindingFlags.NonPublic);

	private static readonly MethodInfo LoadShaderCallsiteMethod = typeof(ShaderRegistryPatches).GetMethod("LoadShaderCallsite");

	private static readonly FieldInfo IncludesField = typeof(ShaderRegistry).GetField("includes", BindingFlags.Static | BindingFlags.NonPublic);

	private static readonly MethodInfo LoadRegisteredCallsiteMethod = typeof(ShaderRegistryPatches).GetMethod("LoadRegisteredCallsite");

	[HarmonyPatch("LoadShader")]
	[HarmonyPostfix]
	public static void LoadShaderPostfix(ShaderProgram program, EnumShaderType shaderType)
	{
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		VolumetricShadingMod.Instance.ShaderInjector.OnShaderLoaded(program, shaderType);
	}

	[HarmonyPatch("HandleIncludes")]
	[HarmonyReversePatch(/*Could not decode attribute arguments.*/)]
	public static string HandleIncludes(ShaderProgram program, string code, HashSet<string> filenames)
	{
		throw new InvalidOperationException("Stub, replaced by Harmony");
	}

	[HarmonyPatch("LoadShader")]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> LoadShaderTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		bool found = false;
		foreach (CodeInstruction instruction in instructions)
		{
			if (CodeInstructionExtensions.Calls(instruction, HandleIncludesMethod))
			{
				found = true;
				yield return new CodeInstruction(OpCodes.Ldarg_1, (object)null);
				yield return new CodeInstruction(OpCodes.Call, (object)LoadShaderCallsiteMethod);
			}
			else
			{
				yield return instruction;
			}
		}
		if (!found)
		{
			throw new Exception("Could not transpile LoadShader");
		}
	}

	public static string LoadShaderCallsite(ShaderProgram shader, string code, HashSet<string> filenames, EnumShaderType type)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Invalid comparison between Unknown and I4
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		string text = (((int)type == 35632) ? ".fsh" : (((int)type == 35633) ? ".vsh" : (((int)type != 36313) ? ".unknown" : ".gsh")));
		string filename = ((ShaderProgramBase)shader).PassName + text;
		code = VolumetricShadingMod.Instance.ShaderPatcher.Patch(filename, code);
		return HandleIncludes(shader, code, filenames);
	}

	[HarmonyPatch("loadRegisteredShaderPrograms")]
	[HarmonyTranspiler]
	public static IEnumerable<CodeInstruction> LoadRegisteredShaderProgramsTranspiler(IEnumerable<CodeInstruction> instructions)
	{
		bool found = false;
		bool generated = false;
		foreach (CodeInstruction instruction in instructions)
		{
			if (found && !generated)
			{
				generated = true;
				yield return CodeInstructionExtensions.WithLabels(new CodeInstruction(OpCodes.Ldsfld, (object)IncludesField), (IEnumerable<Label>)instruction.labels);
				yield return new CodeInstruction(OpCodes.Call, (object)LoadRegisteredCallsiteMethod);
				instruction.labels.Clear();
			}
			yield return instruction;
			if (!(instruction.opcode != OpCodes.Endfinally))
			{
				found = true;
			}
		}
		if (!found)
		{
			throw new Exception("Could not patch loadRegisteredShaderPrograms");
		}
	}

	public static void LoadRegisteredCallsite(Dictionary<string, string> includes)
	{
		VolumetricShadingMod.Instance.ShaderPatcher.Reload();
		foreach (KeyValuePair<string, string> item in includes.ToList())
		{
			string value = VolumetricShadingMod.Instance.ShaderPatcher.Patch(item.Key, item.Value, cache: true);
			includes[item.Key] = value;
		}
	}
}
