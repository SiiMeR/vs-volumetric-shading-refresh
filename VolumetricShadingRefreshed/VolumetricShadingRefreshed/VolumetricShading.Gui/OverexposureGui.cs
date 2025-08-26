using Vintagestory.API.Client;

namespace volumetricshadingupdated.VolumetricShading.Gui;

public class OverexposureGui : AdvancedOptionsDialog
{
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

    // (get) Token: 0x06000180 RID: 384 RVA: 0x000032F5 File Offset: 0x000014F5
    protected override string DialogKey => "vsmodOverexposureConfigure";

    // (get) Token: 0x06000181 RID: 385 RVA: 0x000032FC File Offset: 0x000014FC
    protected override string DialogTitle => "Overexposure Options";

    protected override void RefreshValues()
    {
        SingleComposer.GetSlider("intensitySlider")
            .SetValues(ModSettings.OverexposureIntensity, 0, 200, 1);
        SingleComposer.GetSlider("sunBloomSlider").SetValues(ModSettings.SunBloomIntensity, 0, 100, 1);
    }

    private bool OnIntensitySliderChanged(int t1)
    {
        ModSettings.OverexposureIntensity = t1;
        capi.Shader.ReloadShaders();
        return true;
    }

    private bool OnSunBloomChanged(int t1)
    {
        ModSettings.SunBloomIntensity = t1;
        return true;
    }
}