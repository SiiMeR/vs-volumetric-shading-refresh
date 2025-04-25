namespace volumetricshadingupdated.VolumetricShading.Patch;

public class BoolValueShaderProperty : ValueShaderProperty
{
    public delegate bool BoolValueDelegate();

    public BoolValueDelegate BoolValueGenerator { get; set; }

    public BoolValueShaderProperty(string name = null, BoolValueDelegate boolValueGenerator = null)
    {
        base.ValueGenerator = GenerateValue;
        base.Name = name;
        BoolValueGenerator = boolValueGenerator;
    }

    private string GenerateValue()
    {
        if (!BoolValueGenerator())
        {
            return "0";
        }

        return "1";
    }
}