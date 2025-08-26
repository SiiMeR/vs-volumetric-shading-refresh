using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace volumetricshadingupdated.VolumetricShading.Gui;

public abstract class MainConfigDialog : GuiDialog
{
    private bool _isSetup;

    protected List<ConfigOption> ConfigOptions = new();

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
        var dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightBottom)
            .WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, -GuiStyle.DialogToScreenPadding);
        var font = CairoFont.WhiteSmallText();
        var switchBounds = ElementBounds.Fixed(210.0, GuiStyle.TitleBarHeight, 20.0, 20.0);
        var textBounds = ElementBounds.Fixed(0.0, GuiStyle.TitleBarHeight + 1.0, 200.0, 20.0);
        var advancedButtonBounds = ElementBounds.Fixed(240.0, GuiStyle.TitleBarHeight, 110.0, 20.0);
        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        var composer = capi.Gui.CreateCompo("volumetricShadingConfigure", dialogBounds).AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar("Volumetric Shading Configuration", OnTitleBarCloseClicked)
            .BeginChildElements(bgBounds);
        foreach (var option in ConfigOptions)
        {
            composer.AddStaticText(option.Text, font, textBounds);
            if (option.Tooltip != null)
            {
                composer.AddHoverText(option.Tooltip, font, 250, textBounds.FlatCopy());
            }

            if (option.SwitchKey != null)
            {
                composer.AddSwitch(option.ToggleAction, switchBounds, option.SwitchKey, 20.0);
            }

            if (option.AdvancedAction != null)
            {
                composer.AddSmallButton("Advanced...", option.AdvancedAction, advancedButtonBounds);
            }

            switchBounds = switchBounds.BelowCopy(0.0, 10.0);
            textBounds = textBounds.BelowCopy(0.0, 10.0);
            advancedButtonBounds = advancedButtonBounds.BelowCopy(0.0, 10.0);
        }

        SingleComposer = composer.EndChildElements().Compose();
    }

    public override bool TryOpen()
    {
        if (!_isSetup)
        {
            SetupDialog();
        }

        if (!base.TryOpen())
        {
            return false;
        }

        RefreshValues();
        VolumetricShadingMod.Instance.CurrentDialog = this;
        return true;
    }

    private void OnTitleBarCloseClicked()
    {
        TryClose();
    }

    protected abstract void RefreshValues();

    public class ConfigOption
    {
        public ActionConsumable AdvancedAction;

        public string SwitchKey;

        public string Text;

        public Action<bool> ToggleAction;

        public string Tooltip;
    }
}