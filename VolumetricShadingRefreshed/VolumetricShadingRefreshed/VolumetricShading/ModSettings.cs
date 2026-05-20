using Vintagestory.API.Client;

namespace VolumetricShadingRefreshed.VolumetricShading;

public static class ModSettings
{
    private static ModConfig _config = new();
    private static ICoreClientAPI _api;

    public static void Init(ICoreClientAPI api)
    {
        _api = api;
        _config = api.LoadModConfig<ModConfig>("volumetricshadingrefreshed.json") ?? new ModConfig();
        Save();
    }

    private static void Save() => _api?.StoreModConfig(_config, "volumetricshadingrefreshed.json");

    public static bool ScreenSpaceReflectionsEnabled
    {
        get => _config.ScreenSpaceReflectionsEnabled;
        set { _config.ScreenSpaceReflectionsEnabled = value; Save(); }
    }

    public static bool SSRRainReflectionsEnabled
    {
        get => _config.SSRRainReflectionsEnabled;
        set { _config.SSRRainReflectionsEnabled = value; Save(); }
    }

    public static bool SSRRefractionsEnabled
    {
        get => _config.SSRRefractionsEnabled;
        set { _config.SSRRefractionsEnabled = value; Save(); }
    }

    public static bool SSRCausticsEnabled
    {
        get => _config.SSRCausticsEnabled;
        set { _config.SSRCausticsEnabled = value; Save(); }
    }

    public static int SSRWaterTransparency
    {
        get => _config.SSRWaterTransparency;
        set { _config.SSRWaterTransparency = value; Save(); }
    }

    public static int SSRSplashTransparency
    {
        get => _config.SSRSplashTransparency;
        set { _config.SSRSplashTransparency = value; Save(); }
    }

    public static int SSRReflectionDimming
    {
        get => _config.SSRReflectionDimming;
        set { _config.SSRReflectionDimming = value; Save(); }
    }

    public static int SSRTintInfluence
    {
        get => _config.SSRTintInfluence;
        set { _config.SSRTintInfluence = value; Save(); }
    }

    public static int SSRSkyMixin
    {
        get => _config.SSRSkyMixin;
        set { _config.SSRSkyMixin = value; Save(); }
    }

    public static int VolumetricLightingFlatness
    {
        get => _config.VolumetricLightingFlatness;
        set { _config.VolumetricLightingFlatness = value; Save(); }
    }

    public static int VolumetricLightingIntensity
    {
        get => _config.VolumetricLightingIntensity;
        set { _config.VolumetricLightingIntensity = value; Save(); }
    }

    public static int OverexposureIntensity
    {
        get => _config.OverexposureIntensity;
        set { _config.OverexposureIntensity = value; Save(); }
    }

    public static int SunBloomIntensity
    {
        get => _config.SunBloomIntensity;
        set { _config.SunBloomIntensity = value; Save(); }
    }

    public static int NearShadowBaseWidth
    {
        get => _config.NearShadowBaseWidth;
        set { _config.NearShadowBaseWidth = value; Save(); }
    }

    public static bool SoftShadowsEnabled
    {
        get => _config.SoftShadowsEnabled;
        set { _config.SoftShadowsEnabled = value; Save(); }
    }

    public static int SoftShadowSamples
    {
        get => _config.SoftShadowSamples;
        set { _config.SoftShadowSamples = value; Save(); }
    }

    public static int NearPeterPanningAdjustment
    {
        get => _config.NearPeterPanningAdjustment;
        set { _config.NearPeterPanningAdjustment = value; Save(); }
    }

    public static int FarPeterPanningAdjustment
    {
        get => _config.FarPeterPanningAdjustment;
        set { _config.FarPeterPanningAdjustment = value; Save(); }
    }

    public static bool SSDOEnabled
    {
        get => _config.SSDOEnabled;
        set { _config.SSDOEnabled = value; Save(); }
    }

    public static bool DeferredLightingEnabled
    {
        get => _config.DeferredLightingEnabled;
        set { _config.DeferredLightingEnabled = value; Save(); }
    }

    public static bool UnderwaterTweaksEnabled
    {
        get => _config.UnderwaterTweaksEnabled;
        set { _config.UnderwaterTweaksEnabled = value; Save(); }
    }

    public static bool DepthOfFieldEnabled
    {
        get => _config.DepthOfFieldEnabled;
        set { _config.DepthOfFieldEnabled = value; Save(); }
    }

    public static bool DepthOfFieldAutoFocus
    {
        get => _config.DepthOfFieldAutoFocus;
        set { _config.DepthOfFieldAutoFocus = value; Save(); }
    }

    public static int DepthOfFieldAdaptiveRange
    {
        get => _config.DepthOfFieldAdaptiveRange;
        set { _config.DepthOfFieldAdaptiveRange = value; Save(); }
    }

    public static int DepthOfFieldStrength
    {
        get => _config.DepthOfFieldStrength;
        set { _config.DepthOfFieldStrength = value; Save(); }
    }

    public static int DepthOfFieldFocusDistance
    {
        get => _config.DepthOfFieldFocusDistance;
        set { _config.DepthOfFieldFocusDistance = value; Save(); }
    }

    public static int DepthOfFieldFocusRange
    {
        get => _config.DepthOfFieldFocusRange;
        set { _config.DepthOfFieldFocusRange = value; Save(); }
    }
}
