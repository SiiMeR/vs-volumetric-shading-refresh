using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading.Gui;

public class VolumetricLightingGui : AdvancedOptionsDialog
{
    public VolumetricLightingGui(ICoreClientAPI capi)
        : base(capi)
    {
        RegisterOption(new ConfigOption
        {
            SwitchKey = "toggleVolumetricLighting",
            Text = "Enable Volumetric Lighting",
            ToggleAction = ToggleVolumetricLighting
        });
        RegisterOption(new ConfigOption
        {
            SliderKey = "intensitySlider",
            Text = "Intensity",
            Tooltip = "The intensity of the Volumetric Lighting effect",
            SlideAction = OnIntensitySliderChanged
        });
        RegisterOption(new ConfigOption
        {
            SliderKey = "flatnessSlider",
            Text = "Flatness",
            Tooltip = "Defines how noticeable the difference between low and high scattering is",
            SlideAction = OnFlatnessSliderChanged
        });
    }

    // (get) Token: 0x0600019C RID: 412 RVA: 0x000034ED File Offset: 0x000016ED
    protected override string DialogKey => "vsmodVolumetricLightingConfigure";

    // (get) Token: 0x0600019D RID: 413 RVA: 0x000034F4 File Offset: 0x000016F4
    protected override string DialogTitle => "Volumetric Lighting Options";

    protected override void RefreshValues()
    {
        if (!IsOpened())
        {
            return;
        }

        SingleComposer.GetSwitch("toggleVolumetricLighting").On = ClientSettings.GodRayQuality > 0;
        SingleComposer.GetSlider("flatnessSlider")
            .SetValues(ModSettings.VolumetricLightingFlatness, 1, 199, 1);
        SingleComposer.GetSlider("intensitySlider")
            .SetValues(ModSettings.VolumetricLightingIntensity, 1, 100, 1);
    }

    private void ToggleVolumetricLighting(bool on)
    {
        ClientSettings.GodRayQuality = on ? 1 : 0;
        if (on && ClientSettings.ShadowMapQuality == 0)
        {
            ClientSettings.ShadowMapQuality = 1;
        }

        capi.Shader.ReloadShaders();
        RefreshValues();
    }

    private bool OnFlatnessSliderChanged(int value)
    {
        ModSettings.VolumetricLightingFlatness = value;
        capi.Shader.ReloadShaders();
        RefreshValues();
        return true;
    }

    private bool OnIntensitySliderChanged(int value)
    {
        ModSettings.VolumetricLightingIntensity = value;
        capi.Shader.ReloadShaders();
        RefreshValues();
        return true;
    }
}