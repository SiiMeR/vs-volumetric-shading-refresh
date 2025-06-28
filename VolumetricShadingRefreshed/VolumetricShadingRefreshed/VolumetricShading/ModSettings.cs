using Vintagestory.Client.NoObf;
using Vintagestory.Common;

namespace volumetricshadingupdated.VolumetricShading;

public static class ModSettings
{
    public static bool ScreenSpaceReflectionsEnabled
    {
        get { return ((SettingsBase)ClientSettings.Inst).GetBoolSetting("volumetricshading_screenSpaceReflections"); }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Bool["volumetricshading_screenSpaceReflections"] = value; }
    }

    public static int VolumetricLightingFlatness
    {
        get
        {
            return ((SettingsBase)ClientSettings.Inst).GetIntSetting("volumetricshading_volumetricLightingFlatness");
        }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Int["volumetricshading_volumetricLightingFlatness"] = value; }
    }

    public static int VolumetricLightingIntensity
    {
        get
        {
            return ((SettingsBase)ClientSettings.Inst).GetIntSetting("volumetricshading_volumetricLightingIntensity");
        }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Int["volumetricshading_volumetricLightingIntensity"] = value; }
    }

    public static bool SSDOEnabled
    {
        get { return ((SettingsBase)ClientSettings.Inst).GetBoolSetting("volumetricshading_SSDO"); }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Bool["volumetricshading_SSDO"] = value; }
    }

    public static bool SSRRainReflectionsEnabled
    {
        get { return ((SettingsBase)ClientSettings.Inst).GetBoolSetting("volumetricshading_SSRRainReflections"); }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Bool["volumetricshading_SSRRainReflections"] = value; }
    }

    public static bool SSRRainReflectionsEnabledSet =>
        ((SettingsBaseNoObf)ClientSettings.Inst).Bool.Exists("volumetricshading_SSRRainReflections");

    public static bool SSRRefractionsEnabled
    {
        get { return ((SettingsBase)ClientSettings.Inst).GetBoolSetting("volumetricshading_SSRRefractions"); }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Bool["volumetricshading_SSRRefractions"] = value; }
    }

    public static bool SSRRefractionsEnabledSet =>
        ((SettingsBaseNoObf)ClientSettings.Inst).Bool.Exists("volumetricshading_SSRRefractions");

    public static bool SSRCausticsEnabled
    {
        get { return ((SettingsBase)ClientSettings.Inst).GetBoolSetting("volumetricshading_SSRCaustics"); }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Bool["volumetricshading_SSRCaustics"] = value; }
    }

    public static bool SSRCausticsEnabledSet =>
        ((SettingsBaseNoObf)ClientSettings.Inst).Bool.Exists("volumetricshading_SSRCaustics");

    public static int SSRWaterTransparency
    {
        get { return ((SettingsBase)ClientSettings.Inst).GetIntSetting("volumetricshading_SSRWaterTransparency"); }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Int["volumetricshading_SSRWaterTransparency"] = value; }
    }

    public static bool SSRWaterTransparencySet =>
        ((SettingsBaseNoObf)ClientSettings.Inst).Int.Exists("volumetricshading_SSRWaterTransparency");

    public static int SSRSplashTransparency
    {
        get { return ((SettingsBase)ClientSettings.Inst).GetIntSetting("volumetricshading_SSRSplashTransparency"); }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Int["volumetricshading_SSRSplashTransparency"] = value; }
    }

    public static bool SSRSplashTransparencySet =>
        ((SettingsBaseNoObf)ClientSettings.Inst).Int.Exists("volumetricshading_SSRSplashTransparency");

    public static int SSRReflectionDimming
    {
        get { return ((SettingsBase)ClientSettings.Inst).GetIntSetting("volumetricshading_SSRReflectionDimming"); }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Int["volumetricshading_SSRReflectionDimming"] = value; }
    }

    public static int SSRTintInfluence
    {
        get { return ((SettingsBase)ClientSettings.Inst).GetIntSetting("volumetricshading_SSRTintInfluence"); }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Int["volumetricshading_SSRTintInfluence"] = value; }
    }

    public static bool SSRTintInfluenceSet =>
        ((SettingsBaseNoObf)ClientSettings.Inst).Int.Exists("volumetricshading_SSRTintInfluence");

    public static int SSRSkyMixin
    {
        get { return ((SettingsBase)ClientSettings.Inst).GetIntSetting("volumetricshading_SSRSkyMixin"); }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Int["volumetricshading_SSRSkyMixin"] = value; }
    }

    public static bool SSRSkyMixinSet =>
        ((SettingsBaseNoObf)ClientSettings.Inst).Int.Exists("volumetricshading_SSRSkyMixin");

    public static int OverexposureIntensity
    {
        get { return ((SettingsBase)ClientSettings.Inst).GetIntSetting("volumetricshading_overexposureIntensity"); }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Int["volumetricshading_overexposureIntensity"] = value; }
    }

    public static int SunBloomIntensity
    {
        get { return ((SettingsBase)ClientSettings.Inst).GetIntSetting("volumetricshading_sunBloomIntensity"); }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Int["volumetricshading_sunBloomIntensity"] = value; }
    }

    public static int NearShadowBaseWidth
    {
        get { return ((SettingsBase)ClientSettings.Inst).GetIntSetting("volumetricshading_nearShadowBaseWidth"); }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Int["volumetricshading_nearShadowBaseWidth"] = value; }
    }

    public static bool SoftShadowsEnabled
    {
        get { return ((SettingsBase)ClientSettings.Inst).GetBoolSetting("volumetricshading_softShadows"); }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Bool["volumetricshading_softShadows"] = value; }
    }

    public static int SoftShadowSamples
    {
        get { return ((SettingsBase)ClientSettings.Inst).GetIntSetting("volumetricshading_softShadowSamples"); }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Int["volumetricshading_softShadowSamples"] = value; }
    }

    public static int NearPeterPanningAdjustment
    {
        get
        {
            return ((SettingsBase)ClientSettings.Inst).GetIntSetting("volumetricshading_nearPeterPanningAdjustment");
        }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Int["volumetricshading_nearPeterPanningAdjustment"] = value; }
    }

    public static bool NearPeterPanningAdjustmentSet =>
        ((SettingsBaseNoObf)ClientSettings.Inst).Int.Exists("volumetricshading_nearPeterPanningAdjustment");

    public static int FarPeterPanningAdjustment
    {
        get { return ((SettingsBase)ClientSettings.Inst).GetIntSetting("volumetricshading_farPeterPanningAdjustment"); }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Int["volumetricshading_farPeterPanningAdjustment"] = value; }
    }

    public static bool FarPeterPanningAdjustmentSet =>
        ((SettingsBaseNoObf)ClientSettings.Inst).Int.Exists("volumetricshading_farPeterPanningAdjustment");

    public static bool UnderwaterTweaksEnabled
    {
        get { return ((SettingsBase)ClientSettings.Inst).GetBoolSetting("volumetricshading_underwaterTweaks"); }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Bool["volumetricshading_underwaterTweaks"] = value; }
    }

    public static bool UnderwaterTweaksEnabledSet =>
        ((SettingsBaseNoObf)ClientSettings.Inst).Bool.Exists("volumetricshading_underwaterTweaks");

    public static bool DeferredLightingEnabled
    {
        get { return ((SettingsBase)ClientSettings.Inst).GetBoolSetting("volumetricshading_deferredLighting"); }
        set { ((SettingsBaseNoObf)ClientSettings.Inst).Bool["volumetricshading_deferredLighting"] = value; }
    }
    
    public static bool DeferredLightingEnabledSet =>
        ((SettingsBaseNoObf)ClientSettings.Inst).Bool.Exists("volumetricshading_deferredLighting");
}