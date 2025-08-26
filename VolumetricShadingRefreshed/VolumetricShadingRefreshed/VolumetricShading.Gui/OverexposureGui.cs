using Vintagestory.API.Client;

namespace volumetricshadingupdated.VolumetricShading.Gui;

// Token: 0x02000037 RID: 55
public class OverexposureGui : AdvancedOptionsDialog
{
    // Token: 0x06000182 RID: 386 RVA: 0x00006A60 File Offset: 0x00004C60
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

    // Token: 0x17000050 RID: 80
    // (get) Token: 0x06000180 RID: 384 RVA: 0x000032F5 File Offset: 0x000014F5
    protected override string DialogKey => "vsmodOverexposureConfigure";

    // Token: 0x17000051 RID: 81
    // (get) Token: 0x06000181 RID: 385 RVA: 0x000032FC File Offset: 0x000014FC
    protected override string DialogTitle => "Overexposure Options";

    // Token: 0x06000183 RID: 387 RVA: 0x00006AF8 File Offset: 0x00004CF8
    protected override void RefreshValues()
    {
        SingleComposer.GetSlider("intensitySlider")
            .SetValues(ModSettings.OverexposureIntensity, 0, 200, 1);
        SingleComposer.GetSlider("sunBloomSlider").SetValues(ModSettings.SunBloomIntensity, 0, 100, 1);
    }

    // Token: 0x06000184 RID: 388 RVA: 0x00003303 File Offset: 0x00001503
    private bool OnIntensitySliderChanged(int t1)
    {
        ModSettings.OverexposureIntensity = t1;
        capi.Shader.ReloadShaders();
        return true;
    }

    // Token: 0x06000185 RID: 389 RVA: 0x0000331D File Offset: 0x0000151D
    private bool OnSunBloomChanged(int t1)
    {
        ModSettings.SunBloomIntensity = t1;
        capi.Shader.ReloadShaders();
        return true;
    }
}