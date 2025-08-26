namespace volumetricshadingupdated.VolumetricShading.Patch;

public class BoolValueShaderProperty : ValueShaderProperty
{
    public delegate bool BoolValueDelegate();

    public BoolValueShaderProperty(string name = null, BoolValueDelegate boolValueGenerator = null)
    {
        ValueGenerator = GenerateValue;
        Name = name;
        BoolValueGenerator = boolValueGenerator;
    }

    public BoolValueDelegate BoolValueGenerator { get; set; }

    private string GenerateValue()
    {
        if (!BoolValueGenerator())
        {
            return "0";
        }

        return "1";
    }
}