namespace volumetricshadingupdated.VolumetricShading.Patch;

public class StaticShaderProperty : IShaderProperty
{
    public string Output { get; set; }

    public StaticShaderProperty(string output = null)
    {
        Output = output;
    }

    public string GenerateOutput()
    {
        return Output;
    }
}