using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace VolumetricShading.Gui;

public abstract class AdvancedOptionsDialog : GuiDialog
{
	public class ConfigOption
	{
		public string SwitchKey;

		public string SliderKey;

		public string Text;

		public string Tooltip;

		public Action<bool> ToggleAction;

		public ActionConsumable<int> SlideAction;

		public bool InstantSlider;
	}

	protected List<ConfigOption> ConfigOptions = new List<ConfigOption>();

	private bool _isSetup;

	protected abstract string DialogKey { get; }

	protected abstract string DialogTitle { get; }

	public override string ToggleKeyCombinationCode => null;

	protected AdvancedOptionsDialog(ICoreClientAPI capi)
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
		ElementBounds val3 = ElementBounds.Fixed(250.0, GuiStyle.TitleBarHeight, 20.0, 20.0);
		ElementBounds val4 = ElementBounds.Fixed(0.0, GuiStyle.TitleBarHeight + 1.0, 240.0, 20.0);
		ElementBounds val5 = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
		val5.BothSizing = (ElementSizing)2;
		GuiComposer val6 = GuiComposerHelpers.AddDialogTitleBar(GuiComposerHelpers.AddShadedDialogBG(base.capi.Gui.CreateCompo(DialogKey, val), val5, true, 5.0, 0.75f), DialogTitle, (Action)OnTitleBarCloseClicked, (CairoFont)null, (ElementBounds)null).BeginChildElements(val5);
		foreach (ConfigOption configOption in ConfigOptions)
		{
			GuiComposerHelpers.AddStaticText(val6, configOption.Text, val2, val4, (string)null);
			if (configOption.Tooltip != null)
			{
				GuiComposerHelpers.AddHoverText(val6, configOption.Tooltip, val2, 260, val4, (string)null);
			}
			if (configOption.SliderKey != null)
			{
				GuiComposerHelpers.AddSlider(val6, configOption.SlideAction, val3.FlatCopy().WithFixedWidth(200.0), configOption.SliderKey);
			}
			else if (configOption.SwitchKey != null)
			{
				GuiComposerHelpers.AddSwitch(val6, configOption.ToggleAction, val3, configOption.SwitchKey, 20.0, 4.0);
			}
			val4 = val4.BelowCopy(0.0, 10.0, 0.0, 0.0);
			val3 = val3.BelowCopy(0.0, 10.0, 0.0, 0.0);
		}
		((GuiDialog)this).SingleComposer = val6.EndChildElements().Compose(true);
		foreach (ConfigOption item in ConfigOptions.Where((ConfigOption option) => option.SliderKey != null && !option.InstantSlider))
		{
			GuiComposerHelpers.GetSlider(((GuiDialog)this).SingleComposer, item.SliderKey).TriggerOnlyOnMouseUp();
		}
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
		VolumetricShadingMod.Instance.CurrentDialog = (GuiDialog)(object)this;
		RefreshValues();
		return true;
	}

	protected abstract void RefreshValues();

	protected void OnTitleBarCloseClicked()
	{
		((GuiDialog)this).TryClose();
		((GuiDialog)VolumetricShadingMod.Instance.ConfigGui).TryOpen();
	}
}
