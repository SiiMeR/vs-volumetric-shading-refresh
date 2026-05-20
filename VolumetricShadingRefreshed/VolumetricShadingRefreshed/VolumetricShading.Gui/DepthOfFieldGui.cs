using Vintagestory.API.Client;

namespace VolumetricShadingRefreshed.VolumetricShading.Gui;

public class DepthOfFieldGui : AdvancedOptionsDialog
{
    public DepthOfFieldGui(ICoreClientAPI capi)
        : base(capi)
    {
        RegisterOption(new ConfigOption
        {
            SwitchKey = "autoFocusSwitch",
            Text = "Auto Focus",
            Tooltip = "Camera automatically focuses on whatever is at the center of the screen",
            ToggleAction = OnAutoFocusToggled
        });
        RegisterOption(new ConfigOption
        {
            SliderKey = "strengthSlider",
            Text = "Blur Strength",
            Tooltip = "Maximum blur radius for fully out-of-focus objects",
            SlideAction = OnStrengthChanged,
            InstantSlider = true
        });
        RegisterOption(new ConfigOption
        {
            SliderKey = "focusDistanceSlider",
            Text = "Focus Distance",
            Tooltip = "Distance to the focal point in blocks. Only used when Auto Focus is disabled.",
            SlideAction = OnFocusDistanceChanged,
            InstantSlider = true
        });
        RegisterOption(new ConfigOption
        {
            SliderKey = "focusRangeSlider",
            Text = "Focus Range",
            Tooltip = "Width of the sharp zone around the focal point. Higher values mean a wider area stays in focus.",
            SlideAction = OnFocusRangeChanged,
            InstantSlider = true
        });
        RegisterOption(new ConfigOption
        {
            SliderKey = "adaptiveRangeSlider",
            Text = "Adaptive Range",
            Tooltip = "How much the focus range widens when focusing at greater distances. 0 disables the effect.",
            SlideAction = OnAdaptiveRangeChanged,
            InstantSlider = true
        });
    }


    protected override string DialogKey => "vsmodDepthOfFieldConfigure";


    protected override string DialogTitle => "Depth of Field Options";

    protected override void RefreshValues()
    {
        SingleComposer.GetSwitch("autoFocusSwitch").On = ModSettings.DepthOfFieldAutoFocus;
        SingleComposer.GetSlider("strengthSlider").SetValues(ModSettings.DepthOfFieldStrength, 0, 100, 1);
        SingleComposer.GetSlider("focusDistanceSlider").SetValues(ModSettings.DepthOfFieldFocusDistance, 0, 100, 1);
        SingleComposer.GetSlider("focusRangeSlider").SetValues(ModSettings.DepthOfFieldFocusRange, 0, 100, 1);
        SingleComposer.GetSlider("adaptiveRangeSlider").SetValues(ModSettings.DepthOfFieldAdaptiveRange, 0, 100, 1);
    }

    private void OnAutoFocusToggled(bool on)
    {
        ModSettings.DepthOfFieldAutoFocus = on;
        capi.Shader.ReloadShaders();
    }

    private bool OnStrengthChanged(int value)
    {
        ModSettings.DepthOfFieldStrength = value;
        return true;
    }

    private bool OnFocusDistanceChanged(int value)
    {
        ModSettings.DepthOfFieldFocusDistance = value;
        return true;
    }

    private bool OnFocusRangeChanged(int value)
    {
        ModSettings.DepthOfFieldFocusRange = value;
        return true;
    }

    private bool OnAdaptiveRangeChanged(int value)
    {
        ModSettings.DepthOfFieldAdaptiveRange = value;
        return true;
    }
}
