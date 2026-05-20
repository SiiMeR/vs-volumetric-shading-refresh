using Vintagestory.API.Client;

namespace VolumetricShadingRefreshed.VolumetricShading.Gui;

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
            SlideAction = OnDimmingSliderChanged,
            InstantSlider = true
        });
        RegisterOption(new ConfigOption
        {
            SliderKey = "transparencySlider",
            Text = "Water transparency",
            Tooltip = "Sets the transparency of the vanilla water effect",
            SlideAction = OnTransparencySliderChanged,
            InstantSlider = true
        });
        RegisterOption(new ConfigOption
        {
            SliderKey = "splashTransparencySlider",
            Text = "Splash transparency",
            Tooltip = "The strength of the vanilla splash effect",
            SlideAction = OnSplashTransparencySliderChanged,
            InstantSlider = true
        });
        RegisterOption(new ConfigOption
        {
            SliderKey = "tintSlider",
            Text = "Tint influence",
            Tooltip = "Sets the influence an object's tint has on it's reflection color",
            SlideAction = OnTintSliderChanged,
            InstantSlider = true
        });
        RegisterOption(new ConfigOption
        {
            SliderKey = "skyMixinSlider",
            Text = "Sky mixin",
            Tooltip = "The amount of sky color that is always visible, even when fully reflecting",
            SlideAction = OnSkyMixinSliderChanged,
            InstantSlider = true
        });
        RegisterOption(new ConfigOption
        {
            SliderKey = "strengthSlider",
            Text = "Reflection strength",
            Tooltip = "Overall visibility of reflections. Lower values make water less shiny",
            SlideAction = OnStrengthSliderChanged,
            InstantSlider = true
        });
        RegisterOption(new ConfigOption
        {
            SliderKey = "distortionSlider",
            Text = "Surface distortion",
            Tooltip = "How much waves distort the reflection. Hides SSR artifacts and makes water feel less like polished glass",
            SlideAction = OnDistortionSliderChanged,
            InstantSlider = true
        });
    }


    protected override string DialogKey => "vsmodSSRConfigure";


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
        SingleComposer.GetSlider("strengthSlider").SetValues(ModSettings.SSRStrength, 0, 100, 1);
        SingleComposer.GetSlider("distortionSlider").SetValues(ModSettings.SSRDistortion, 0, 100, 1);
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
        return true;
    }

    private bool OnTransparencySliderChanged(int value)
    {
        ModSettings.SSRWaterTransparency = value;
        return true;
    }

    private bool OnSplashTransparencySliderChanged(int value)
    {
        ModSettings.SSRSplashTransparency = value;
        return true;
    }

    private bool OnTintSliderChanged(int value)
    {
        ModSettings.SSRTintInfluence = value;
        return true;
    }

    private bool OnSkyMixinSliderChanged(int value)
    {
        ModSettings.SSRSkyMixin = value;
        return true;
    }

    private bool OnStrengthSliderChanged(int value)
    {
        ModSettings.SSRStrength = value;
        return true;
    }

    private bool OnDistortionSliderChanged(int value)
    {
        ModSettings.SSRDistortion = value;
        return true;
    }
}