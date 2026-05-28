namespace VolumetricShadingRefreshed.VolumetricShading;

public class ModConfig
{
    public bool ScreenSpaceReflectionsEnabled { get; set; } = true;
    public bool SSRRainReflectionsEnabled { get; set; } = true;
    public bool SSRRefractionsEnabled { get; set; } = true;
    public bool SSRCausticsEnabled { get; set; } = true;
    public int SSRWaterTransparency { get; set; } = 25;
    public int SSRSplashTransparency { get; set; } = 55;
    public int SSRReflectionDimming { get; set; } = 130;
    public int SSRTintInfluence { get; set; } = 10;
    public int SSRSkyMixin { get; set; } = 12;
    public int SSRDistortion { get; set; } = 5;
    public int SSRStrength { get; set; } = 100;

    public int VolumetricLightingFlatness { get; set; } = 120;
    public int VolumetricLightingIntensity { get; set; } = 36;

    public int OverexposureIntensity { get; set; } = 0;
    public int SunBloomIntensity { get; set; } = 0;

    public int NearShadowBaseWidth { get; set; } = 15;
    public bool SoftShadowsEnabled { get; set; } = false;
    public int SoftShadowSamples { get; set; } = 16;
    public int NearPeterPanningAdjustment { get; set; } = 2;
    public int FarPeterPanningAdjustment { get; set; } = 5;

    public bool SSDOEnabled { get; set; } = false;
    public bool DeferredLightingEnabled { get; set; } = true;
    public bool UnderwaterTweaksEnabled { get; set; } = true;

    public bool DepthOfFieldEnabled { get; set; } = false;
    public bool DepthOfFieldAutoFocus { get; set; } = true;
    public int DepthOfFieldAdaptiveRange { get; set; } = 20;
    public int DepthOfFieldStrength { get; set; } = 50;
    public int DepthOfFieldFocusDistance { get; set; } = 20;
    public int DepthOfFieldFocusRange { get; set; } = 30;
}
