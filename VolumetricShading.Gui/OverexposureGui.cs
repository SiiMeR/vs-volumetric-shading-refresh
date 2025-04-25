using Vintagestory.API.Client;

namespace VolumetricShading.Gui;

public class OverexposureGui : AdvancedOptionsDialog
{
	protected override string DialogKey => "vsmodOverexposureConfigure";

	protected override string DialogTitle => "Overexposure Options";

	public OverexposureGui(ICoreClientAPI capi)
		: base(capi)
	{
		RegisterOption(new ConfigOption
		{
			SliderKey = "intensitySlider",
			Text = "Intensity",
			Tooltip = "The intensity of the overexposure effect",
			SlideAction = OnIntensitySliderChanged
		});
		RegisterOption(new ConfigOption
		{
			SliderKey = "sunBloomSlider",
			Text = "Sun Bloom",
			Tooltip = "Defines how strong the additional sun blooming is",
			SlideAction = OnSunBloomChanged,
			InstantSlider = true
		});
	}

	protected override void RefreshValues()
	{
		GuiComposerHelpers.GetSlider(((GuiDialog)this).SingleComposer, "intensitySlider").SetValues(ModSettings.OverexposureIntensity, 0, 200, 1, "");
		GuiComposerHelpers.GetSlider(((GuiDialog)this).SingleComposer, "sunBloomSlider").SetValues(ModSettings.SunBloomIntensity, 0, 100, 1, "");
	}

	private bool OnIntensitySliderChanged(int t1)
	{
		ModSettings.OverexposureIntensity = t1;
		((GuiDialog)this).capi.Shader.ReloadShaders();
		return true;
	}

	private bool OnSunBloomChanged(int t1)
	{
		ModSettings.SunBloomIntensity = t1;
		return true;
	}
}
