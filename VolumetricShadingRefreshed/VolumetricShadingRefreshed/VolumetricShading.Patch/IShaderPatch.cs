namespace VolumetricShadingRefreshed.VolumetricShading.Patch;

public interface IShaderPatch
{
    bool ShouldPatch(string filename, string code);

    string Patch(string filename, string code);
}