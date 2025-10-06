namespace VolumetricShadingRefreshed.VolumetricShading.Patch;

public class StaticShaderProperty : IShaderProperty
{
    public StaticShaderProperty(string output = null)
    {
        Output = output;
    }

    public string Output { get; set; }

    public string GenerateOutput()
    {
        return Output;
    }
}