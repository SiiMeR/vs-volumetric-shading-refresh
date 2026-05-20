using System;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using VolumetricShadingRefreshed.VolumetricShading.Effects;
using VolumetricShadingRefreshed.VolumetricShading.Gui;
using VolumetricShadingRefreshed.VolumetricShading.Patch;

namespace VolumetricShadingRefreshed.VolumetricShading;

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


    public DepthOfField DepthOfField { get; private set; }

    public override bool ShouldLoad(EnumAppSide forSide)
    {
        return forSide == EnumAppSide.Client;
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        RegisterHotkeys();
        PatchGame();
    }

    public override void StartPre(ICoreAPI api)
    {
        var clientApi = api as ICoreClientAPI;
        if (clientApi == null)
        {
            return;
        }

        ModSettings.Init(clientApi);
        Instance = this;
        CApi = clientApi;
        Events = new Events();
        Uniforms = new Uniforms(this);
        Debug = Environment.GetEnvironmentVariable("VOLUMETRICSHADING_DEBUG").ToBool();
        if (Debug)
        {
            Mod.Logger.Event("Debugging activated");
        }

        ShaderPatcher = new ShaderPatcher(CApi, Mod.Info.ModID);
        ShaderInjector = new ShaderInjector(CApi, Mod.Info.ModID);
        VolumetricLighting = new VolumetricLighting(this);
        ScreenSpaceReflections = new ScreenSpaceReflections(this);
        OverexposureEffect = new OverexposureEffect(this);
        ScreenSpaceDirectionalOcclusion = new ScreenSpaceDirectionalOcclusion(this);
        ShadowTweaks = new ShadowTweaks(this);
        DeferredLighting = new DeferredLighting(this);
        UnderwaterTweaks = new UnderwaterTweaks(this);
        DepthOfField = new DepthOfField(this);
        ShaderInjector.Debug = Debug;
    }

    private void RegisterHotkeys()
    {
        CApi.Input.RegisterHotKey("volumetriclightingconfigure", "Volumetric Lighting Configuration", GlKeys.C,
            HotkeyType.GUIOrOtherControls, false, true);
        CApi.Input.SetHotKeyHandler("volumetriclightingconfigure", OnConfigurePressed);
    }

    private void PatchGame()
    {
        Mod.Logger.Event("Loading harmony for patching...");
        Harmony.DEBUG = Debug;
        _harmony = new Harmony(Mod.Info.ModID);
        _harmony.PatchAll();
        foreach (var method in _harmony.GetPatchedMethods())
        {
            Mod.Logger.Event("Patched " + method.FullDescription());
        }

        ShaderPatcher.Reload();
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

        ConfigGui.TryOpen();
        return true;
    }

    public override void Dispose()
    {
        if (CApi == null)
        {
            return;
        }

        ShadowTweaks.Dispose();
        DepthOfField?.Dispose();
        var harmony = _harmony;
        if (harmony != null)
        {
            harmony.UnpatchAll();
        }

        Instance = null;
    }
}