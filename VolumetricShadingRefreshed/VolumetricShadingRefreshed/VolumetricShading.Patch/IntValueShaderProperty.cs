namespace volumetricshadingupdated.VolumetricShading.Patch;

public class IntValueShaderProperty : ValueShaderProperty
{
    public delegate int IntValueDelegate();

    public IntValueShaderProperty(string name = null, IntValueDelegate intValueGenerator = null)
    {
        ValueGenerator = GenerateValue;
        Name = name;
        IntValueGenerator = intValueGenerator;
    }

    public IntValueDelegate IntValueGenerator { get; set; }

    private string GenerateValue()
    {
        return IntValueGenerator().ToString();
    }
}