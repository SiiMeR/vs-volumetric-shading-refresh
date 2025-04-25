using Vintagestory.API.Client;

namespace VolumetricShading.Gui;

public class ShadowTweaksGui : AdvancedOptionsDialog
{
	protected override string DialogKey => "vsmodShadowTweaksConfigure";

	protected override string DialogTitle => "Shadow Tweaks";

	public ShadowTweaksGui(ICoreClientAPI capi)
		: base(capi)
	{
		RegisterOption(new ConfigOption
		{
			SliderKey = "shadowBaseWidthSlider",
			Text = "Near base width",
			Tooltip = "Sets the base width of the near shadow map. Increases sharpness of near shadows,but decreases sharpness of mid-distance ones. Unmodified game value is 30.",
			SlideAction = OnShadowBaseWidthSliderChanged,
			InstantSlider = true
		});
		RegisterOption(new ConfigOption
		{
			SwitchKey = "softShadowsEnabled",
			Text = "Soft shadows (slow)",
			Tooltip = "Soft shadows based on occluder distance to shadow surface. Can be very slow in difficult scenes, highly recommended to use in combination with deferred lighting.",
			ToggleAction = OnSoftShadowsToggled
		});
		RegisterOption(new ConfigOption
		{
			SliderKey = "softShadowSamples",
			Text = "Soft shadow samples",
			Tooltip = "Amount of soft shadow samples. More samples = better looks, but slower.",
			SlideAction = OnSoftShadowSamplesChanged
		});
		RegisterOption(new ConfigOption
		{
			SliderKey = "nearPeterPanningSlider",
			Text = "Near offset adjustment",
			Tooltip = "Adjusts the near shadow map Z offset. Reduces peter panning, but might lead to artifacts.",
			SlideAction = OnNearPeterPanningChanged
		});
		RegisterOption(new ConfigOption
		{
			SliderKey = "farPeterPanningSlider",
			Text = "Far offset adjustment",
			Tooltip = "Adjusts the far shadow map Z offset. Reduces peter panning, but might lead to artifacts.",
			SlideAction = OnFarPeterPanningChanged
		});
	}

	protected override void RefreshValues()
	{
		GuiComposerHelpers.GetSlider(((GuiDialog)this).SingleComposer, "shadowBaseWidthSlider").SetValues(ModSettings.NearShadowBaseWidth, 5, 30, 1, "");
		GuiComposerHelpers.GetSwitch(((GuiDialog)this).SingleComposer, "softShadowsEnabled").SetValue(ModSettings.SoftShadowsEnabled);
		GuiComposerHelpers.GetSlider(((GuiDialog)this).SingleComposer, "softShadowSamples").SetValues(ModSettings.SoftShadowSamples, 1, 64, 1, "");
		GuiComposerHelpers.GetSlider(((GuiDialog)this).SingleComposer, "nearPeterPanningSlider").SetValues(ModSettings.NearPeterPanningAdjustment, 0, 4, 1, "");
		GuiComposerHelpers.GetSlider(((GuiDialog)this).SingleComposer, "farPeterPanningSlider").SetValues(ModSettings.FarPeterPanningAdjustment, 0, 8, 1, "");
	}

	private void OnSoftShadowsToggled(bool enabled)
	{
		ModSettings.SoftShadowsEnabled = enabled;
		((GuiDialog)this).capi.Shader.ReloadShaders();
	}

	private bool OnSoftShadowSamplesChanged(int value)
	{
		ModSettings.SoftShadowSamples = value;
		((GuiDialog)this).capi.Shader.ReloadShaders();
		return true;
	}

	private bool OnShadowBaseWidthSliderChanged(int value)
	{
		ModSettings.NearShadowBaseWidth = value;
		return true;
	}

	private bool OnNearPeterPanningChanged(int value)
	{
		ModSettings.NearPeterPanningAdjustment = value;
		((GuiDialog)this).capi.Shader.ReloadShaders();
		return true;
	}

	private bool OnFarPeterPanningChanged(int value)
	{
		ModSettings.FarPeterPanningAdjustment = value;
		((GuiDialog)this).capi.Shader.ReloadShaders();
		return true;
	}
}
