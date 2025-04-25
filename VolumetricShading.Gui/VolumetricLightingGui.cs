using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShading.Gui;

public class VolumetricLightingGui : AdvancedOptionsDialog
{
	protected override string DialogKey => "vsmodVolumetricLightingConfigure";

	protected override string DialogTitle => "Volumetric Lighting Options";

	public VolumetricLightingGui(ICoreClientAPI capi)
		: base(capi)
	{
		RegisterOption(new ConfigOption
		{
			SwitchKey = "toggleVolumetricLighting",
			Text = "Enable Volumetric Lighting",
			ToggleAction = ToggleVolumetricLighting
		});
		RegisterOption(new ConfigOption
		{
			SliderKey = "intensitySlider",
			Text = "Intensity",
			Tooltip = "The intensity of the Volumetric Lighting effect",
			SlideAction = OnIntensitySliderChanged
		});
		RegisterOption(new ConfigOption
		{
			SliderKey = "flatnessSlider",
			Text = "Flatness",
			Tooltip = "Defines how noticeable the difference between low and high scattering is",
			SlideAction = OnFlatnessSliderChanged
		});
	}

	protected override void RefreshValues()
	{
		if (((GuiDialog)this).IsOpened())
		{
			GuiComposerHelpers.GetSwitch(((GuiDialog)this).SingleComposer, "toggleVolumetricLighting").On = ClientSettings.GodRayQuality > 0;
			GuiComposerHelpers.GetSlider(((GuiDialog)this).SingleComposer, "flatnessSlider").SetValues(ModSettings.VolumetricLightingFlatness, 1, 199, 1, "");
			GuiComposerHelpers.GetSlider(((GuiDialog)this).SingleComposer, "intensitySlider").SetValues(ModSettings.VolumetricLightingIntensity, 1, 100, 1, "");
		}
	}

	private void ToggleVolumetricLighting(bool on)
	{
		ClientSettings.GodRayQuality = (on ? 1 : 0);
		if (on && ClientSettings.ShadowMapQuality == 0)
		{
			ClientSettings.ShadowMapQuality = 1;
		}
		((GuiDialog)this).capi.Shader.ReloadShaders();
		RefreshValues();
	}

	private bool OnFlatnessSliderChanged(int value)
	{
		ModSettings.VolumetricLightingFlatness = value;
		((GuiDialog)this).capi.Shader.ReloadShaders();
		RefreshValues();
		return true;
	}

	private bool OnIntensitySliderChanged(int value)
	{
		ModSettings.VolumetricLightingIntensity = value;
		((GuiDialog)this).capi.Shader.ReloadShaders();
		RefreshValues();
		return true;
	}
}
