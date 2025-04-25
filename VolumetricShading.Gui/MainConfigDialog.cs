using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VolumetricShading.Gui;

public abstract class MainConfigDialog : GuiDialog
{
	public class ConfigOption
	{
		public ActionConsumable AdvancedAction;

		public string SwitchKey;

		public string Text;

		public Action<bool> ToggleAction;

		public string Tooltip;
	}

	private bool _isSetup;

	protected List<ConfigOption> ConfigOptions = new List<ConfigOption>();

	protected MainConfigDialog(ICoreClientAPI capi)
		: base(capi)
	{
	}

	protected void RegisterOption(ConfigOption option)
	{
		ConfigOptions.Add(option);
	}

	protected void SetupDialog()
	{
		_isSetup = true;
		ElementBounds val = ElementStdBounds.AutosizedMainDialog.WithAlignment((EnumDialogArea)11).WithFixedAlignmentOffset(0.0 - GuiStyle.DialogToScreenPadding, 0.0 - GuiStyle.DialogToScreenPadding);
		CairoFont val2 = CairoFont.WhiteSmallText();
		ElementBounds val3 = ElementBounds.Fixed(210.0, GuiStyle.TitleBarHeight, 20.0, 20.0);
		ElementBounds val4 = ElementBounds.Fixed(0.0, GuiStyle.TitleBarHeight + 1.0, 200.0, 20.0);
		ElementBounds val5 = ElementBounds.Fixed(240.0, GuiStyle.TitleBarHeight, 110.0, 20.0);
		ElementBounds val6 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		val6.BothSizing = (ElementSizing)2;
		GuiComposer val7 = GuiComposerHelpers.AddDialogTitleBar(GuiComposerHelpers.AddShadedDialogBG(base.capi.Gui.CreateCompo("volumetricShadingConfigure", val), val6, true, 5.0, 0.75f), "Volumetric Shading Configuration", (Action)OnTitleBarCloseClicked, (CairoFont)null, (ElementBounds)null).BeginChildElements(val6);
		foreach (ConfigOption configOption in ConfigOptions)
		{
			GuiComposerHelpers.AddStaticText(val7, configOption.Text, val2, val4, (string)null);
			if (configOption.Tooltip != null)
			{
				GuiComposerHelpers.AddHoverText(val7, configOption.Tooltip, val2, 250, val4.FlatCopy(), (string)null);
			}
			if (configOption.SwitchKey != null)
			{
				GuiComposerHelpers.AddSwitch(val7, configOption.ToggleAction, val3, configOption.SwitchKey, 20.0, 4.0);
			}
			if (configOption.AdvancedAction != null)
			{
				GuiComposerHelpers.AddSmallButton(val7, "Advanced...", configOption.AdvancedAction, val5, (EnumButtonStyle)2, (string)null);
			}
			val3 = val3.BelowCopy(0.0, 10.0, 0.0, 0.0);
			val4 = val4.BelowCopy(0.0, 10.0, 0.0, 0.0);
			val5 = val5.BelowCopy(0.0, 10.0, 0.0, 0.0);
		}
		((GuiDialog)this).SingleComposer = val7.EndChildElements().Compose(true);
	}

	public override bool TryOpen()
	{
		if (!_isSetup)
		{
			SetupDialog();
		}
		if (!((GuiDialog)this).TryOpen())
		{
			return false;
		}
		RefreshValues();
		VolumetricShadingMod.Instance.CurrentDialog = (GuiDialog)(object)this;
		return true;
	}

	private void OnTitleBarCloseClicked()
	{
		((GuiDialog)this).TryClose();
	}

	protected abstract void RefreshValues();
}
