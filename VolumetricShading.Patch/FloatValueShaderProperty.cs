using System.Globalization;

namespace volumetricshadingupdated.VolumetricShading.Patch;

public class FloatValueShaderProperty : ValueShaderProperty
{
    public delegate float FloatValueDelegate();

    public FloatValueDelegate FloatValueGenerator { get; set; }

    public FloatValueShaderProperty(string name = null, FloatValueDelegate floatValueGenerator = null)
    {
        base.ValueGenerator = GenerateValue;
        base.Name = name;
        FloatValueGenerator = floatValueGenerator;
    }

    private string GenerateValue()
    {
        return FloatValueGenerator().ToString("0.00", CultureInfo.InvariantCulture);
    }
}