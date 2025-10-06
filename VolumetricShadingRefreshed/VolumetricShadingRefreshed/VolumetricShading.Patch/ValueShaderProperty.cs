namespace VolumetricShadingRefreshed.VolumetricShading.Patch;

public class ValueShaderProperty : IShaderProperty
{
    public delegate string ValueDelegate();

    public ValueShaderProperty(string name = null, ValueDelegate valueGenerator = null)
    {
        Name = name;
        ValueGenerator = valueGenerator;
    }

    public string Name { get; set; }

    public ValueDelegate ValueGenerator { get; set; }

    public string GenerateOutput()
    {
        return $"#define {Name} {ValueGenerator()}\r\n";
    }
}