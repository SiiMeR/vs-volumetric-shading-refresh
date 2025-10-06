using System.Globalization;

namespace VolumetricShadingRefreshed.VolumetricShading.Patch;

public class FloatValueShaderProperty : ValueShaderProperty
{
    public delegate float FloatValueDelegate();

    public FloatValueShaderProperty(string name = null, FloatValueDelegate floatValueGenerator = null)
    {
        ValueGenerator = GenerateValue;
        Name = name;
        FloatValueGenerator = floatValueGenerator;
    }

    public FloatValueDelegate FloatValueGenerator { get; set; }

    private string GenerateValue()
    {
        return FloatValueGenerator().ToString("0.00", CultureInfo.InvariantCulture);
    }
}