using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace volumetricshadingupdated.VolumetricShading.Gui;

public abstract class AdvancedOptionsDialog : GuiDialog
{
    private bool _isSetup;

    protected List<ConfigOption> ConfigOptions = new();

    protected AdvancedOptionsDialog(ICoreClientAPI capi)
        : base(capi)
    {
    }

    // (get) Token: 0x06000162 RID: 354
    protected abstract string DialogKey { get; }

    // (get) Token: 0x06000163 RID: 355
    protected abstract string DialogTitle { get; }

    // (get) Token: 0x06000166 RID: 358 RVA: 0x000030E1 File Offset: 0x000012E1
    public override string ToggleKeyCombinationCode => null;

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
        var switchBounds = ElementBounds.Fixed(250.0, GuiStyle.TitleBarHeight, 20.0, 20.0);
        var textBounds = ElementBounds.Fixed(0.0, GuiStyle.TitleBarHeight + 1.0, 240.0, 20.0);
        var bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
        bgBounds.BothSizing = ElementSizing.FitToChildren;
        var composer = capi.Gui.CreateCompo(DialogKey, dialogBounds).AddShadedDialogBG(bgBounds)
            .AddDialogTitleBar(DialogTitle, OnTitleBarCloseClicked)
            .BeginChildElements(bgBounds);
        foreach (var option3 in ConfigOptions)
        {
            composer.AddStaticText(option3.Text, font, textBounds);
            if (option3.Tooltip != null)
            {
                composer.AddHoverText(option3.Tooltip, font, 260, textBounds);
            }

            if (option3.SliderKey != null)
            {
                composer.AddSlider(option3.SlideAction, switchBounds.FlatCopy().WithFixedWidth(200.0),
                    option3.SliderKey);
            }
            else if (option3.SwitchKey != null)
            {
                composer.AddSwitch(option3.ToggleAction, switchBounds, option3.SwitchKey, 20.0);
            }

            textBounds = textBounds.BelowCopy(0.0, 10.0);
            switchBounds = switchBounds.BelowCopy(0.0, 10.0);
        }

        SingleComposer = composer.EndChildElements().Compose();
        foreach (var option2 in ConfigOptions.Where(option => option.SliderKey != null && !option.InstantSlider))
        {
            SingleComposer.GetSlider(option2.SliderKey).TriggerOnlyOnMouseUp();
        }
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

        VolumetricShadingMod.Instance.CurrentDialog = this;
        RefreshValues();
        return true;
    }

    protected abstract void RefreshValues();

    protected void OnTitleBarCloseClicked()
    {
        TryClose();
        VolumetricShadingMod.Instance.ConfigGui.TryOpen();
    }

    public class ConfigOption
    {
        public bool InstantSlider;

        public ActionConsumable<int> SlideAction;

        public string SliderKey;
        public string SwitchKey;

        public string Text;

        public Action<bool> ToggleAction;

        public string Tooltip;
    }
}