using System;
using System.Reflection;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using volumetricshadingupdated.VolumetricShading.Effects;
using volumetricshadingupdated.VolumetricShading.Gui;
using volumetricshadingupdated.VolumetricShading.Patch;

namespace volumetricshadingupdated.VolumetricShading;

public class VolumetricShadingMod : ModSystem
{
    private Harmony _harmony;

    public ConfigGui ConfigGui;

    public GuiDialog CurrentDialog;

    public static VolumetricShadingMod Instance { get; private set; }

    public ICoreClientAPI CApi { get; private set; }

    public Events Events { get; private set; }

    public Uniforms Uniforms { get; private set; }

    public bool Debug { get; private set; }

    public ShaderPatcher ShaderPatcher { get; private set; }

    public ShaderInjector ShaderInjector { get; private set; }

    public ScreenSpaceReflections ScreenSpaceReflections { get; private set; }

    public VolumetricLighting VolumetricLighting { get; private set; }

    public OverexposureEffect OverexposureEffect { get; private set; }

    public ScreenSpaceDirectionalOcclusion ScreenSpaceDirectionalOcclusion { get; private set; }

    public ShadowTweaks ShadowTweaks { get; private set; }

    public DeferredLighting DeferredLighting { get; private set; }

    public UnderwaterTweaks UnderwaterTweaks { get; private set; }

    public override bool ShouldLoad(EnumAppSide forSide)
    {
        //IL_0000: Unknown result type (might be due to invalid IL or missing references)
        //IL_0002: Invalid comparison between Unknown and I4
        return (int)forSide == 2;
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        PatchGame();
        RegisterHotkeys();
    }

    public override void StartPre(ICoreAPI api)
    {
        ICoreClientAPI val = (ICoreClientAPI)(object)((api is ICoreClientAPI) ? api : null);
        if (val != null)
        {
            SetConfigDefaults();
            Instance = this;
            CApi = val;
            Events = new Events();
            Uniforms = new Uniforms(this);
            Debug = StringUtil.ToBool(Environment.GetEnvironmentVariable("VOLUMETRICSHADING_DEBUG"), false);
            if (Debug)
            {
                ((ModSystem)this).Mod.Logger.Event("Debugging activated");
            }

            ShaderPatcher = new ShaderPatcher(CApi, ((ModSystem)this).Mod.Info.ModID);
            ShaderInjector = new ShaderInjector(CApi, ((ModSystem)this).Mod.Info.ModID);
            VolumetricLighting = new VolumetricLighting(this);
            ScreenSpaceReflections = new ScreenSpaceReflections(this);
            OverexposureEffect = new OverexposureEffect(this);
            ScreenSpaceDirectionalOcclusion = new ScreenSpaceDirectionalOcclusion(this);
            ShadowTweaks = new ShadowTweaks(this);
            DeferredLighting = new DeferredLighting(this);
            UnderwaterTweaks = new UnderwaterTweaks(this);
            ShaderInjector.Debug = Debug;
        }
    }

    private void RegisterHotkeys()
    {
        CApi.Input.RegisterHotKey("volumetriclightingconfigure", "Volumetric Lighting Configuration", (GlKeys)85,
            (HotkeyType)2, false, true, false);
        CApi.Input.SetHotKeyHandler("volumetriclightingconfigure",
            (ActionConsumable<KeyCombination>)OnConfigurePressed);
    }

    private void PatchGame()
    {
        //IL_0026: Unknown result type (might be due to invalid IL or missing references)
        //IL_0030: Expected O, but got Unknown
        ((ModSystem)this).Mod.Logger.Event("Loading harmony for patching...");
        Harmony.DEBUG = Debug;
        _harmony = new Harmony("com.xxmicloxx.vsvolumetricshading");
        _harmony.PatchAll();
        foreach (MethodBase patchedMethod in _harmony.GetPatchedMethods())
        {
            ((ModSystem)this).Mod.Logger.Event("Patched " + GeneralExtensions.FullDescription(patchedMethod));
        }

        ShaderPatcher.Reload();
    }

    private static void SetConfigDefaults()
    {
        if (ModSettings.VolumetricLightingFlatness == 0)
        {
            ModSettings.VolumetricLightingFlatness = 140;
        }

        if (ModSettings.VolumetricLightingIntensity == 0)
        {
            ModSettings.VolumetricLightingIntensity = 50;
        }

        if (!ModSettings.SSRWaterTransparencySet)
        {
            ModSettings.SSRWaterTransparency = 25;
        }

        if (ModSettings.SSRReflectionDimming == 0)
        {
            ModSettings.SSRReflectionDimming = 110;
        }

        if (!ModSettings.SSRTintInfluenceSet)
        {
            ModSettings.SSRTintInfluence = 35;
        }

        if (!ModSettings.SSRSkyMixinSet)
        {
            ModSettings.SSRSkyMixin = 0;
        }

        if (!ModSettings.SSRSplashTransparencySet)
        {
            ModSettings.SSRSplashTransparency = 65;
        }

        if (ModSettings.NearShadowBaseWidth == 0)
        {
            ModSettings.NearShadowBaseWidth = 15;
        }

        if (ModSettings.SoftShadowSamples == 0)
        {
            ModSettings.SoftShadowSamples = 16;
        }

        if (!ModSettings.NearPeterPanningAdjustmentSet)
        {
            ModSettings.NearPeterPanningAdjustment = 2;
        }

        if (!ModSettings.FarPeterPanningAdjustmentSet)
        {
            ModSettings.FarPeterPanningAdjustment = 5;
        }

        if (!ModSettings.SSRRainReflectionsEnabledSet)
        {
            ModSettings.SSRRainReflectionsEnabled = true;
        }

        if (!ModSettings.SSRRefractionsEnabledSet)
        {
            ModSettings.SSRRefractionsEnabled = true;
        }

        if (!ModSettings.SSRCausticsEnabledSet)
        {
            ModSettings.SSRCausticsEnabled = true;
        }

        if (!ModSettings.UnderwaterTweaksEnabledSet)
        {
            ModSettings.UnderwaterTweaksEnabled = true;
        }
    }

    private bool OnConfigurePressed(KeyCombination cb)
    {
        if (ConfigGui == null)
        {
            ConfigGui = new ConfigGui(CApi);
        }

        if (CurrentDialog != null && CurrentDialog.IsOpened())
        {
            CurrentDialog.TryClose();
            return true;
        }

        ((GuiDialog)ConfigGui).TryOpen();
        return true;
    }

    public override void Dispose()
    {
        if (CApi != null)
        {
            ShadowTweaks.Dispose();
            Harmony harmony = _harmony;
            if (harmony != null)
            {
                harmony.UnpatchAll((string)null);
            }

            Instance = null;
        }
    }
}