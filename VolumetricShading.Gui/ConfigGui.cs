using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace VolumetricShading.Gui;

public class ConfigGui : MainConfigDialog
{
	public override string ToggleKeyCombinationCode => "volumetriclightingconfigure";

	public ConfigGui(ICoreClientAPI capi)
		: base(capi)
	{
		//IL_0048: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f2: Expected O, but got Unknown
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Expected O, but got Unknown
		RegisterOption(new ConfigOption
		{
			SwitchKey = "toggleVolumetricLighting",
			Text = "Volumetric Lighting",
			Tooltip = "Enables realistic scattering of light",
			ToggleAction = ToggleVolumetricLighting,
			AdvancedAction = new ActionConsumable(OnVolumetricAdvancedClicked)
		});
		RegisterOption(new ConfigOption
		{
			SwitchKey = "toggleSSR",
			Text = "Screen Space Reflections",
			Tooltip = "Enables reflections, for example on water",
			ToggleAction = ToggleScreenSpaceReflections,
			AdvancedAction = new ActionConsumable(OnSSRAdvancedClicked)
		});
		RegisterOption(new ConfigOption
		{
			SwitchKey = "toggleOverexposure",
			Text = "Overexposure",
			Tooltip = "Adds overexposure at brightly sunlit places",
			ToggleAction = ToggleOverexposure,
			AdvancedAction = new ActionConsumable(OnOverexposureAdvancedClicked)
		});
		RegisterOption(new ConfigOption
		{
			Text = "Shadow Tweaks",
			Tooltip = "Allows for some shadow tweaks that might make them look better",
			AdvancedAction = new ActionConsumable(OnShadowTweaksAdvancedClicked)
		});
		RegisterOption(new ConfigOption
		{
			SwitchKey = "toggleUnderwater",
			Text = "Underwater Tweaks",
			Tooltip = "Better underwater looks",
			ToggleAction = ToggleUnderwater
		});
		RegisterOption(new ConfigOption
		{
			SwitchKey = "toggleDeferred",
			Text = "Deferred Lighting",
			Tooltip = "Aims to improve lighting performance by deferring lighting operations. Requires SSAO.",
			ToggleAction = ToggleDeferredLighting
		});
		RegisterOption(new ConfigOption
		{
			SwitchKey = "toggleSSDO",
			Text = "Improve SSAO",
			Tooltip = "Replaces SSAO with SSDO. Results in marginally faster and better looking occlusions.",
			ToggleAction = ToggleSSDO
		});
		SetupDialog();
		capi.Settings.AddWatcher<int>("godRays", (OnSettingsChanged<int>)delegate
		{
			RefreshValues();
		});
	}

	protected override void RefreshValues()
	{
		if (((GuiDialog)this).IsOpened())
		{
			GuiComposerHelpers.GetSwitch(((GuiDialog)this).SingleComposer, "toggleVolumetricLighting").On = ClientSettings.GodRayQuality > 0;
			GuiComposerHelpers.GetSwitch(((GuiDialog)this).SingleComposer, "toggleSSR").On = ModSettings.ScreenSpaceReflectionsEnabled;
			GuiComposerHelpers.GetSwitch(((GuiDialog)this).SingleComposer, "toggleSSDO").On = ModSettings.SSDOEnabled;
			GuiComposerHelpers.GetSwitch(((GuiDialog)this).SingleComposer, "toggleOverexposure").On = ModSettings.OverexposureIntensity > 0;
			GuiComposerHelpers.GetSwitch(((GuiDialog)this).SingleComposer, "toggleUnderwater").On = ModSettings.UnderwaterTweaksEnabled;
			GuiComposerHelpers.GetSwitch(((GuiDialog)this).SingleComposer, "toggleDeferred").On = ModSettings.DeferredLightingEnabled;
		}
	}

	private void ToggleUnderwater(bool enabled)
	{
		ModSettings.UnderwaterTweaksEnabled = enabled;
		RefreshValues();
	}

	private void ToggleDeferredLighting(bool enabled)
	{
		ModSettings.DeferredLightingEnabled = enabled;
		((GuiDialog)this).capi.GetClientPlatformAbstract().RebuildFrameBuffers();
		((GuiDialog)this).capi.Shader.ReloadShaders();
		RefreshValues();
	}

	private void ToggleVolumetricLighting(bool on)
	{
		if (on && ClientSettings.ShadowMapQuality == 0)
		{
			ClientSettings.ShadowMapQuality = 1;
		}
		ClientSettings.GodRayQuality = (on ? 1 : 0);
		((GuiDialog)this).capi.Shader.ReloadShaders();
		RefreshValues();
	}

	private void ToggleScreenSpaceReflections(bool on)
	{
		ModSettings.ScreenSpaceReflectionsEnabled = on;
		((GuiDialog)this).capi.GetClientPlatformAbstract().RebuildFrameBuffers();
		((GuiDialog)this).capi.Shader.ReloadShaders();
		RefreshValues();
	}

	private void ToggleSSDO(bool on)
	{
		if (on && ClientSettings.SSAOQuality == 0)
		{
			ClientSettings.SSAOQuality = 1;
			((GuiDialog)this).capi.GetClientPlatformAbstract().RebuildFrameBuffers();
		}
		ModSettings.SSDOEnabled = on;
		((GuiDialog)this).capi.Shader.ReloadShaders();
		RefreshValues();
	}

	private void ToggleOverexposure(bool on)
	{
		if (on && ModSettings.OverexposureIntensity <= 0)
		{
			ModSettings.OverexposureIntensity = 50;
		}
		else if (!on && ModSettings.OverexposureIntensity > 0)
		{
			ModSettings.OverexposureIntensity = 0;
		}
		((GuiDialog)this).capi.Shader.ReloadShaders();
		RefreshValues();
	}

	private bool OnVolumetricAdvancedClicked()
	{
		((GuiDialog)this).TryClose();
		((GuiDialog)new VolumetricLightingGui(((GuiDialog)this).capi)).TryOpen();
		return true;
	}

	private bool OnSSRAdvancedClicked()
	{
		((GuiDialog)this).TryClose();
		((GuiDialog)new ScreenSpaceReflectionsGui(((GuiDialog)this).capi)).TryOpen();
		return true;
	}

	private bool OnOverexposureAdvancedClicked()
	{
		((GuiDialog)this).TryClose();
		((GuiDialog)new OverexposureGui(((GuiDialog)this).capi)).TryOpen();
		return true;
	}

	private bool OnShadowTweaksAdvancedClicked()
	{
		((GuiDialog)this).TryClose();
		((GuiDialog)new ShadowTweaksGui(((GuiDialog)this).capi)).TryOpen();
		return true;
	}
}
