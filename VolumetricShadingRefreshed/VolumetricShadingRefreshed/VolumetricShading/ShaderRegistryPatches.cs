using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShadingRefreshed.VolumetricShading;

[HarmonyPatch(typeof(ShaderRegistry))]
internal class ShaderRegistryPatches
{
    private static readonly MethodInfo HandleIncludesMethod =
        typeof(ShaderRegistry).GetMethod("HandleIncludes", BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly MethodInfo LoadShaderCallsiteMethod =
        typeof(ShaderRegistryPatches).GetMethod("LoadShaderCallsite");

    private static readonly FieldInfo IncludesField =
        typeof(ShaderRegistry).GetField("includes", BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly MethodInfo LoadRegisteredCallsiteMethod =
        typeof(ShaderRegistryPatches).GetMethod("LoadRegisteredCallsite");

    [HarmonyPatch("LoadShader")]
    [HarmonyPostfix]
    public static void LoadShaderPostfix(ShaderProgram program, EnumShaderType shaderType)
    {
        VolumetricShadingMod.Instance.ShaderInjector.OnShaderLoaded(program, shaderType);
    }

    [HarmonyPatch("HandleIncludes")]
    [HarmonyReversePatch]
    public static string HandleIncludes(ShaderProgram program, string code, HashSet<string> filenames)
    {
        throw new InvalidOperationException("Stub, replaced by Harmony");
    }

    [HarmonyPatch("LoadShader")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> LoadShaderTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var found = false;
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(HandleIncludesMethod))
            {
                found = true;
                yield return new CodeInstruction(OpCodes.Ldarg_1);
                yield return new CodeInstruction(OpCodes.Call, LoadShaderCallsiteMethod);
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

    public static string LoadShaderCallsite(ShaderProgram shader, string code, HashSet<string> filenames,
        EnumShaderType type)
    {
        var text = (int)type == 35632
            ? ".fsh"
            : (int)type == 35633
                ? ".vsh"
                : (int)type != 36313
                    ? ".unknown"
                    : ".gsh";
        var filename = shader.PassName + text;
        code = VolumetricShadingMod.Instance.ShaderPatcher.Patch(filename, code);
        return HandleIncludes(shader, code, filenames);
    }

    [HarmonyPatch("loadRegisteredShaderPrograms")]
    [HarmonyTranspiler]
    public static IEnumerable<CodeInstruction> LoadRegisteredShaderProgramsTranspiler(
        IEnumerable<CodeInstruction> instructions)
    {
        var found = false;
        var generated = false;
        foreach (var instruction in instructions)
        {
            if (found && !generated)
            {
                generated = true;
                yield return new CodeInstruction(OpCodes.Ldsfld, IncludesField).WithLabels(instruction.labels);
                yield return new CodeInstruction(OpCodes.Call, LoadRegisteredCallsiteMethod);
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
        foreach (var item in includes.ToList())
        {
            var value = VolumetricShadingMod.Instance.ShaderPatcher.Patch(item.Key, item.Value, true);
            includes[item.Key] = value;
        }
    }
}