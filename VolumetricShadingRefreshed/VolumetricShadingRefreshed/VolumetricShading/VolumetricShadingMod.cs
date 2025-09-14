using System;
using System.Diagnostics;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using volumetricshadingupdated.VolumetricShading.Effects;
using volumetricshadingupdated.VolumetricShading.Gui;
using volumetricshadingupdated.VolumetricShading.Patch;

namespace volumetricshadingupdated.VolumetricShading;

public class VolumetricShadingMod : ModSystem, IRenderer
{
    private Harmony _harmony;
    private double _lastFrameTime;

    public ConfigGui ConfigGui;

    public GuiDialog CurrentDialog;

    // (get) Token: 0x060000CF RID: 207 RVA: 0x00002A2B File Offset: 0x00000C2B
    // (set) Token: 0x060000D0 RID: 208 RVA: 0x00002A32 File Offset: 0x00000C32
    public static VolumetricShadingMod Instance { get; private set; }

    // (get) Token: 0x060000D1 RID: 209 RVA: 0x00002A3A File Offset: 0x00000C3A
    // (set) Token: 0x060000D2 RID: 210 RVA: 0x00002A42 File Offset: 0x00000C42
    public ICoreClientAPI CApi { get; private set; }

    // (get) Token: 0x060000D3 RID: 211 RVA: 0x00002A4B File Offset: 0x00000C4B
    // (set) Token: 0x060000D4 RID: 212 RVA: 0x00002A53 File Offset: 0x00000C53
    public Events Events { get; private set; }

    // (get) Token: 0x060000D5 RID: 213 RVA: 0x00002A5C File Offset: 0x00000C5C
    // (set) Token: 0x060000D6 RID: 214 RVA: 0x00002A64 File Offset: 0x00000C64
    public Uniforms Uniforms { get; private set; }

    // (get) Token: 0x060000D7 RID: 215 RVA: 0x00002A6D File Offset: 0x00000C6D
    // (set) Token: 0x060000D8 RID: 216 RVA: 0x00002A75 File Offset: 0x00000C75
    public bool Debug { get; private set; }

    // (get) Token: 0x060000D9 RID: 217 RVA: 0x00002A7E File Offset: 0x00000C7E
    // (set) Token: 0x060000DA RID: 218 RVA: 0x00002A86 File Offset: 0x00000C86
    public ShaderPatcher ShaderPatcher { get; private set; }

    // (get) Token: 0x060000DB RID: 219 RVA: 0x00002A8F File Offset: 0x00000C8F
    // (set) Token: 0x060000DC RID: 220 RVA: 0x00002A97 File Offset: 0x00000C97
    public ShaderInjector ShaderInjector { get; private set; }

    /// <summary>
    /// Modern shader uniform manager that replaces string-based injection
    /// </summary>
    public ShaderUniformManager ShaderUniformManager { get; private set; }

    // (get) Token: 0x060000DD RID: 221 RVA: 0x00002AA0 File Offset: 0x00000CA0
    // (set) Token: 0x060000DE RID: 222 RVA: 0x00002AA8 File Offset: 0x00000CA8
    public ScreenSpaceReflections ScreenSpaceReflections { get; private set; }

    /// <summary>
    /// Performance monitoring and automatic quality adjustment
    /// </summary>
    public PerformanceManager PerformanceManager { get; private set; }

    /// <summary>
    /// Debug command manager for performance analysis and testing
    /// </summary>
    public DebugCommandManager DebugCommandManager { get; private set; }

    // (get) Token: 0x060000DF RID: 223 RVA: 0x00002AB1 File Offset: 0x00000CB1
    // (set) Token: 0x060000E0 RID: 224 RVA: 0x00002AB9 File Offset: 0x00000CB9
    public VolumetricLighting VolumetricLighting { get; private set; }

    // (get) Token: 0x060000E1 RID: 225 RVA: 0x00002AC2 File Offset: 0x00000CC2
    // (set) Token: 0x060000E2 RID: 226 RVA: 0x00002ACA File Offset: 0x00000CCA
    public OverexposureEffect OverexposureEffect { get; private set; }

    // (get) Token: 0x060000E3 RID: 227 RVA: 0x00002AD3 File Offset: 0x00000CD3
    // (set) Token: 0x060000E4 RID: 228 RVA: 0x00002ADB File Offset: 0x00000CDB
    public ScreenSpaceDirectionalOcclusion ScreenSpaceDirectionalOcclusion { get; private set; }

    // (get) Token: 0x060000E5 RID: 229 RVA: 0x00002AE4 File Offset: 0x00000CE4
    // (set) Token: 0x060000E6 RID: 230 RVA: 0x00002AEC File Offset: 0x00000CEC
    public ShadowTweaks ShadowTweaks { get; private set; }

    // (get) Token: 0x060000E7 RID: 231 RVA: 0x00002AF5 File Offset: 0x00000CF5
    // (set) Token: 0x060000E8 RID: 232 RVA: 0x00002AFD File Offset: 0x00000CFD
    public DeferredLighting DeferredLighting { get; private set; }

    // (get) Token: 0x060000E9 RID: 233 RVA: 0x00002B06 File Offset: 0x00000D06
    // (set) Token: 0x060000EA RID: 234 RVA: 0x00002B0E File Offset: 0x00000D0E
    public UnderwaterTweaks UnderwaterTweaks { get; private set; }

    /// <summary>
    /// Modern overexposure renderer using dedicated post-process shaders
    /// </summary>
    public OverexposureRenderer OverexposureRenderer { get; private set; }

    /// <summary>
    /// Modern blur renderer with separable Gaussian blur
    /// </summary>
    public BlurRenderer BlurRenderer { get; private set; }

    public override bool ShouldLoad(EnumAppSide forSide)
    {
        return forSide == EnumAppSide.Client;
    }

    public override void StartPre(ICoreAPI api)
    {
        var clientApi = api as ICoreClientAPI;
        if (clientApi == null)
        {
            return;
        }

        SetConfigDefaults();
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
        ShaderUniformManager = new ShaderUniformManager(this);
        
        VolumetricLighting = new VolumetricLighting(this);
        ScreenSpaceReflections = new ScreenSpaceReflections(this);
        
        // Modern effect renderers replacing YAML-based patches
        OverexposureRenderer = new OverexposureRenderer(this);
        BlurRenderer = new BlurRenderer(this);
        
        // Legacy effects (to be modernized in future phases)
        OverexposureEffect = new OverexposureEffect(this);
        ScreenSpaceDirectionalOcclusion = new ScreenSpaceDirectionalOcclusion(this);
        ShadowTweaks = new ShadowTweaks(this);
        DeferredLighting = new DeferredLighting(this);
        UnderwaterTweaks = new UnderwaterTweaks(this);
        
        PerformanceManager = new PerformanceManager(this);
        DebugCommandManager = new DebugCommandManager(this);
        ShaderInjector.Debug = Debug;
    }

    public override void StartClientSide(ICoreClientAPI api)
    {
        // AMD Compatibility: Apply hardware-specific settings after OpenGL context is available
        CompatibilityManager.ApplyCompatibilitySettings();
        
        // Start performance monitoring
        CApi.Event.RegisterRenderer(this, EnumRenderStage.Before, "vsmod-performance-monitor");
        
        // Register modern effect renderers
        CApi.Event.RegisterRenderer(OverexposureRenderer, EnumRenderStage.AfterOIT, "vsmod-overexposure-modern");
        CApi.Event.RegisterRenderer(BlurRenderer, EnumRenderStage.AfterBloom, "vsmod-blur-modern");
        
        // Register debug commands
        DebugCommandManager.RegisterCommands();
        
        RegisterHotkeys();
        PatchGame();
        
        Mod.Logger.Event("Volumetric Shading Mod initialized with performance monitoring");
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

    private static void SetConfigDefaults()
    {
        if (ModSettings.VolumetricLightingFlatness == 0)
        {
            ModSettings.VolumetricLightingFlatness = 120;
        }

        if (ModSettings.VolumetricLightingIntensity == 0)
        {
            ModSettings.VolumetricLightingIntensity = 30;
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
            ModSettings.SSRSkyMixin = 10;
        }

        if (!ModSettings.SSRSplashTransparencySet)
        {
            ModSettings.SSRSplashTransparency = 55;
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

        if (!ModSettings.DeferredLightingEnabledSet)
        {
            // AMD Compatibility: Start with conservative defaults, let users enable manually
            ModSettings.DeferredLightingEnabled = false;
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

        ConfigGui.TryOpen();
        return true;
    }

    #region IRenderer Implementation for Performance Monitoring
    
    public double RenderOrder => -1.0; // Render first to capture frame timing
    public int RenderRange => 9999;

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        if (stage == EnumRenderStage.Before)
        {
            // Update performance metrics
            PerformanceManager?.UpdateFrameTiming(deltaTime);
            _lastFrameTime = deltaTime;
        }
    }
    
    #endregion

    /// <summary>
    /// Modern shader registration helper that integrates with ShaderUniformManager
    /// </summary>
    public IShaderProgram RegisterModernShader(string shaderName)
    {
        try
        {
            var shader = CApi.Shader.NewShaderProgram()
                .WithName(shaderName)
                .WithVertexShader(CApi.Assets.Get(new AssetLocation(Mod.Info.ModID, $"shaders/{shaderName}.vsh")))
                .WithFragmentShader(CApi.Assets.Get(new AssetLocation(Mod.Info.ModID, $"shaders/{shaderName}.fsh")))
                .WithUniformProvider(() => ShaderUniformManager?.UpdateShaderUniforms(shader))
                .Compile();
                
            if (shader != null)
            {
                Mod.Logger.Event($"Modern shader '{shaderName}' registered successfully");
            }
            else
            {
                Mod.Logger.Error($"Failed to register modern shader '{shaderName}'");
            }
            
            return shader;
        }
        catch (Exception ex)
        {
            Mod.Logger.Error($"Exception registering modern shader '{shaderName}': {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Legacy shader registration for compatibility (delegates to existing system)
    /// </summary>
    public object RegisterShader(string shaderName, ref bool success)
    {
        try
        {
            // This maintains compatibility with existing code while we transition
            var shader = CApi.Shader.GetShader(shaderName);
            success = shader != null;
            return shader;
        }
        catch (Exception ex)
        {
            Mod.Logger.Error($"Exception in legacy shader registration '{shaderName}': {ex.Message}");
            success = false;
            return null;
        }
    }

    public override void Dispose()
    {
        if (CApi == null)
        {
            return;
        }

        // Clean up performance manager
        PerformanceManager?.Reset();

        // Dispose modern renderers
        OverexposureRenderer?.Dispose();
        BlurRenderer?.Dispose();
        ShaderUniformManager?.Dispose();

        ShadowTweaks.Dispose();
        var harmony = _harmony;
        if (harmony != null)
        {
            harmony.UnpatchAll();
        }

        Instance = null;
    }
}