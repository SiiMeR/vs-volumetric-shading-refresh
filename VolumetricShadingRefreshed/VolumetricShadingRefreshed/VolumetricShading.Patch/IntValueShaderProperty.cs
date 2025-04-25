namespace volumetricshadingupdated.VolumetricShading.Patch;

public class IntValueShaderProperty : ValueShaderProperty
{
    public delegate int IntValueDelegate();

    public IntValueDelegate IntValueGenerator { get; set; }

    public IntValueShaderProperty(string name = null, IntValueDelegate intValueGenerator = null)
    {
        base.ValueGenerator = GenerateValue;
        base.Name = name;
        IntValueGenerator = intValueGenerator;
    }

    private string GenerateValue()
    {
        return IntValueGenerator().ToString();
    }
}