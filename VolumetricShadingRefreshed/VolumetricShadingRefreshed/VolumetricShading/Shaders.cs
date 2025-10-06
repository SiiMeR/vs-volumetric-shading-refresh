using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShadingRefreshed.VolumetricShading;

public static class Shaders
{
    public static IShaderProgram RegisterShader(this VolumetricShadingMod mod, string name, ref bool success)
    {
        //IL_0010: Unknown result type (might be due to invalid IL or missing references)
        //IL_0016: Expected O, but got Unknown
        var val = (ShaderProgram)mod.CApi.Shader.NewShaderProgram();
        val.AssetDomain = mod.Mod.Info.ModID;
        mod.CApi.Shader.RegisterFileShaderProgram(name, val);
        if (!val.Compile())
        {
            success = false;
        }

        return val;
    }
}