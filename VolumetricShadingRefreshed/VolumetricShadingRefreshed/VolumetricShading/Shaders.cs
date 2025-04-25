using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading;

public static class Shaders
{
    public static IShaderProgram RegisterShader(this VolumetricShadingMod mod, string name, ref bool success)
    {
        //IL_0010: Unknown result type (might be due to invalid IL or missing references)
        //IL_0016: Expected O, but got Unknown
        ShaderProgram val = (ShaderProgram)mod.CApi.Shader.NewShaderProgram();
        val.AssetDomain = ((ModSystem)mod).Mod.Info.ModID;
        mod.CApi.Shader.RegisterFileShaderProgram(name, (IShaderProgram)(object)val);
        if (!((ShaderProgramBase)val).Compile())
        {
            success = false;
        }

        return (IShaderProgram)(object)val;
    }
}