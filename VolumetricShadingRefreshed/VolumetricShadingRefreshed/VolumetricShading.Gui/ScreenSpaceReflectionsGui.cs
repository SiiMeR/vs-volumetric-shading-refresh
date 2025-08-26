using Vintagestory.API.Client;

namespace volumetricshadingupdated.VolumetricShading.Gui;

public class ScreenSpaceReflectionsGui : AdvancedOptionsDialog
{
    public ScreenSpaceReflectionsGui(ICoreClientAPI capi)
        : base(capi)
    {
        RegisterOption(new ConfigOption
        {
            SwitchKey = "toggleSSR",
            Text = "Enable Screen Space Reflections",
            ToggleAction = ToggleSSR
        });
        RegisterOption(new ConfigOption
        {
            SwitchKey = "toggleRefractions",
            Text = "Enable refractions",
            Tooltip = "Enables refractions for water, ice, and glass",
            ToggleAction = ToggleRefractions
        });
        RegisterOption(new ConfigOption
        {
            SwitchKey = "toggleCaustics",
            Text = "Enable caustics",
            Tooltip = "Enables bands of light underwater caused by waves on the surface",
            ToggleAction = ToggleCaustics
        });
        RegisterOption(new ConfigOption
        {
            SwitchKey = "toggleRainReflections",
            Text = "Enable wet grass",
            Tooltip = "Enables reflecting grass when raining",
            ToggleAction = ToggleRainReflections
        });
        RegisterOption(new ConfigOption
        {
            SliderKey = "dimmingSlider",
            Text = "Reflection dimming",
            Tooltip = "The dimming effect strength on the reflected image",
            SlideAction = OnDimmingSliderChanged
        });
        RegisterOption(new ConfigOption
        {
            SliderKey = "transparencySlider",
            Text = "Water transparency",
            Tooltip = "Sets the transparency of the vanilla water effect",
            SlideAction = OnTransparencySliderChanged
        });
        RegisterOption(new ConfigOption
        {
            SliderKey = "splashTransparencySlider",
            Text = "Splash transparency",
            Tooltip = "The strength of the vanilla splash effect",
            SlideAction = OnSplashTransparencySliderChanged
        });
        RegisterOption(new ConfigOption
        {
            SliderKey = "tintSlider",
            Text = "Tint influence",
            Tooltip = "Sets the influence an object's tint has on it's reflection color",
            SlideAction = OnTintSliderChanged
        });
        RegisterOption(new ConfigOption
        {
            SliderKey = "skyMixinSlider",
            Text = "Sky mixin",
            Tooltip = "The amount of sky color that is always visible, even when fully reflecting",
            SlideAction = OnSkyMixinSliderChanged
        });
    }

    // (get) Token: 0x06000186 RID: 390 RVA: 0x00003326 File Offset: 0x00001526
    protected override string DialogKey => "vsmodSSRConfigure";

    // (get) Token: 0x06000187 RID: 391 RVA: 0x0000332D File Offset: 0x0000152D
    protected override string DialogTitle => "Screen Space Reflections Options";

    protected override void RefreshValues()
    {
        SingleComposer.GetSwitch("toggleSSR").SetValue(ModSettings.ScreenSpaceReflectionsEnabled);
        SingleComposer.GetSwitch("toggleRefractions").SetValue(ModSettings.SSRRefractionsEnabled);
        SingleComposer.GetSwitch("toggleCaustics").SetValue(ModSettings.SSRCausticsEnabled);
        SingleComposer.GetSwitch("toggleRainReflections").SetValue(ModSettings.SSRRainReflectionsEnabled);
        SingleComposer.GetSlider("dimmingSlider").SetValues(ModSettings.SSRReflectionDimming, 1, 400, 1);
        SingleComposer.GetSlider("transparencySlider")
            .SetValues(ModSettings.SSRWaterTransparency, 0, 100, 1);
        SingleComposer.GetSlider("tintSlider").SetValues(ModSettings.SSRTintInfluence, 0, 100, 1);
        SingleComposer.GetSlider("skyMixinSlider").SetValues(ModSettings.SSRSkyMixin, 0, 100, 1);
        SingleComposer.GetSlider("splashTransparencySlider")
            .SetValues(ModSettings.SSRSplashTransparency, 0, 100, 1);
    }

    private void ToggleSSR(bool on)
    {
        ModSettings.ScreenSpaceReflectionsEnabled = on;
        capi.GetClientPlatformAbstract().RebuildFrameBuffers();
        capi.Shader.ReloadShaders();
        RefreshValues();
    }

    private void ToggleCaustics(bool enabled)
    {
        ModSettings.SSRCausticsEnabled = enabled;
        capi.GetClientPlatformAbstract().RebuildFrameBuffers();
        capi.Shader.ReloadShaders();
        RefreshValues();
    }

    private void ToggleRefractions(bool on)
    {
        ModSettings.SSRRefractionsEnabled = on;
        capi.GetClientPlatformAbstract().RebuildFrameBuffers();
        capi.Shader.ReloadShaders();
        RefreshValues();
    }

    private void ToggleRainReflections(bool on)
    {
        ModSettings.SSRRainReflectionsEnabled = on;
        RefreshValues();
    }

    private bool OnDimmingSliderChanged(int value)
    {
        ModSettings.SSRReflectionDimming = value;
        capi.Shader.ReloadShaders();
        RefreshValues();
        return true;
    }

    private bool OnTransparencySliderChanged(int value)
    {
        ModSettings.SSRWaterTransparency = value;
        capi.Shader.ReloadShaders();
        RefreshValues();
        return true;
    }

    private bool OnSplashTransparencySliderChanged(int value)
    {
        ModSettings.SSRSplashTransparency = value;
        capi.Shader.ReloadShaders();
        RefreshValues();
        return true;
    }

    private bool OnTintSliderChanged(int value)
    {
        ModSettings.SSRTintInfluence = value;
        capi.Shader.ReloadShaders();
        RefreshValues();
        return true;
    }

    private bool OnSkyMixinSliderChanged(int value)
    {
        ModSettings.SSRSkyMixin = value;
        capi.Shader.ReloadShaders();
        RefreshValues();
        return true;
    }
}