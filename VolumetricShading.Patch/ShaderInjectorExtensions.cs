namespace VolumetricShading.Patch;

public static class ShaderInjectorExtensions
{
	public static void RegisterStaticProperty(this ShaderInjector injector, string output)
	{
		injector.RegisterShaderProperty(new StaticShaderProperty(output));
	}

	public static void RegisterFloatProperty(this ShaderInjector injector, string name, FloatValueShaderProperty.FloatValueDelegate floatGenerator)
	{
		injector.RegisterShaderProperty(new FloatValueShaderProperty(name, floatGenerator));
	}

	public static void RegisterIntProperty(this ShaderInjector injector, string name, IntValueShaderProperty.IntValueDelegate intGenerator)
	{
		injector.RegisterShaderProperty(new IntValueShaderProperty(name, intGenerator));
	}

	public static void RegisterBoolProperty(this ShaderInjector injector, string name, BoolValueShaderProperty.BoolValueDelegate boolGenerator)
	{
		injector.RegisterShaderProperty(new BoolValueShaderProperty(name, boolGenerator));
	}
}
