using Vintagestory.API.Client;

namespace volumetricshadingupdated.VolumetricShading.Gui;

public class DofGui : AdvancedOptionsDialog
{
    public DofGui(ICoreClientAPI capi)
        : base(capi)
    {
        RegisterOption(new ConfigOption
        {
            SliderKey = "dofFocusDepthSlider",
            Text = "Focus Depth",
            Tooltip = "The normalized focal depth",
            SlideAction = OnFocusDepthSliderChanged
        });
        RegisterOption(new ConfigOption
        {
            SliderKey = "dofBlurRangeSlider",
            Text = "Blur Range",
            Tooltip = "The range the blurring starts at",
            SlideAction = OnBlurRangeSliderChanged
        });
    }

    // (get) Token: 0x06000180 RID: 384 RVA: 0x000032F5 File Offset: 0x000014F5
    protected override string DialogKey => "vsmodDofConfigure";


    // (get) Token: 0x06000181 RID: 385 RVA: 0x000032FC File Offset: 0x000014FC
    protected override string DialogTitle => "Dof Options";


    protected override void RefreshValues()
    {
        SingleComposer.GetSlider("dofFocusDepthSlider")
            .SetValues(ModSettings.DofFocusDepth, 0, 100, 1);
        SingleComposer.GetSlider("dofBlurRangeSlider").SetValues(ModSettings.DofBlurRange, 0, 100, 1);
    }


    private bool OnFocusDepthSliderChanged(int t1)
    {
        ModSettings.DofFocusDepth = t1;
        capi.Shader.ReloadShaders();
        return true;
    }


    private bool OnBlurRangeSliderChanged(int t1)
    {
        ModSettings.DofBlurRange = t1;
        capi.Shader.ReloadShaders();
        return true;
    }
}