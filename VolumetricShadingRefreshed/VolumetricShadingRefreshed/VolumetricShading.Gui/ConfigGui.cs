using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading.Gui;

// Token: 0x02000036 RID: 54
public class ConfigGui : MainConfigDialog
{
    // Token: 0x06000172 RID: 370 RVA: 0x00006794 File Offset: 0x00004994
    public ConfigGui(ICoreClientAPI capi)
        : base(capi)
    {
        RegisterOption(new ConfigOption
        {
            SwitchKey = "toggleVolumetricLighting",
            Text = "Volumetric Lighting",
            Tooltip = "Enables realistic scattering of light",
            ToggleAction = ToggleVolumetricLighting,
            AdvancedAction = OnVolumetricAdvancedClicked
        });
        RegisterOption(new ConfigOption
        {
            SwitchKey = "toggleSSR",
            Text = "Screen Space Reflections",
            Tooltip = "Enables reflections, for example on water",
            ToggleAction = ToggleScreenSpaceReflections,
            AdvancedAction = OnSSRAdvancedClicked
        });
        RegisterOption(new ConfigOption
        {
            SwitchKey = "toggleOverexposure",
            Text = "Overexposure",
            Tooltip = "Adds overexposure at brightly sunlit places",
            ToggleAction = ToggleOverexposure,
            AdvancedAction = OnOverexposureAdvancedClicked
        });
        RegisterOption(new ConfigOption
        {
            Text = "Shadow Tweaks",
            Tooltip = "Allows for some shadow tweaks that might make them look better",
            AdvancedAction = OnShadowTweaksAdvancedClicked
        });
        RegisterOption(new ConfigOption
        {
            SwitchKey = "toggleUnderwater",
            Text = "Underwater Tweaks",
            Tooltip = "Better underwater looks",
            ToggleAction = ToggleUnderwater
        });
        RegisterOption(new ConfigOption
        {
            SwitchKey = "toggleDeferred",
            Text = "Deferred Lighting",
            Tooltip = "Aims to improve lighting performance by deferring lighting operations. Requires SSAO.",
            ToggleAction = ToggleDeferredLighting
        });
        RegisterOption(new ConfigOption
        {
            SwitchKey = "toggleSSDO",
            Text = "Improve SSAO",
            Tooltip = "Replaces SSAO with SSDO. Results in marginally faster and better looking occlusions.",
            ToggleAction = ToggleSSDO
        });
        RegisterOption(new ConfigOption
        {
            SwitchKey = "toggleDOF",
            Text = "Depth of Field",
            Tooltip = "Enable depth-of-field blur effect",
            ToggleAction = ToggleDepthOfField,
            AdvancedAction = OnDofAdvancedClicked
        });
        SetupDialog();
        capi.Settings.AddWatcher<int>("godRays", delegate { RefreshValues(); });
    }


    // Token: 0x1700004F RID: 79
    // (get) Token: 0x06000173 RID: 371 RVA: 0x0000315C File Offset: 0x0000135C
    public override string ToggleKeyCombinationCode => "volumetriclightingconfigure";

    private bool OnDofAdvancedClicked()
    {
        TryClose();
        new DofGui(capi).TryOpen();
        return true;
    }

    // Token: 0x06000174 RID: 372 RVA: 0x000069A8 File Offset: 0x00004BA8
    protected override void RefreshValues()
    {
        if (!IsOpened())
        {
            return;
        }

        SingleComposer.GetSwitch("toggleVolumetricLighting").On = ClientSettings.GodRayQuality > 0;
        SingleComposer.GetSwitch("toggleSSR").On = ModSettings.ScreenSpaceReflectionsEnabled;
        SingleComposer.GetSwitch("toggleSSDO").On = ModSettings.SSDOEnabled;
        SingleComposer.GetSwitch("toggleOverexposure").On = ModSettings.OverexposureIntensity > 0;
        SingleComposer.GetSwitch("toggleUnderwater").On = ModSettings.UnderwaterTweaksEnabled;
        SingleComposer.GetSwitch("toggleDeferred").On = ModSettings.DeferredLightingEnabled;
        SingleComposer.GetSwitch("toggleDOF").On = ModSettings.DepthOfFieldEnabled;
    }

    private void ToggleUnderwater(bool enabled)
    {
        ModSettings.UnderwaterTweaksEnabled = enabled;
        RefreshValues();
    }

    private void ToggleDeferredLighting(bool enabled)
    {
        ModSettings.DeferredLightingEnabled = enabled;
        capi.GetClientPlatformAbstract().RebuildFrameBuffers();
        capi.Shader.ReloadShaders();
        RefreshValues();
    }

    private void ToggleDepthOfField(bool on)
    {
        ModSettings.DepthOfFieldEnabled = on;
        // If using intensity as flag, set intensity >0 when enabled, 0 when disabled:
        // ModSettings.DofBlurIntensity = on ? 50 : 0;
        capi.Shader.ReloadShaders();
        RefreshValues();
    }


    // Token: 0x06000177 RID: 375 RVA: 0x000031A0 File Offset: 0x000013A0
    private void ToggleVolumetricLighting(bool on)
    {
        if (on && ClientSettings.ShadowMapQuality == 0)
        {
            ClientSettings.ShadowMapQuality = 1;
        }

        ClientSettings.GodRayQuality = on ? 1 : 0;
        capi.Shader.ReloadShaders();
        RefreshValues();
    }

    // Token: 0x06000178 RID: 376 RVA: 0x000031D5 File Offset: 0x000013D5
    private void ToggleScreenSpaceReflections(bool on)
    {
        ModSettings.ScreenSpaceReflectionsEnabled = on;
        capi.GetClientPlatformAbstract().RebuildFrameBuffers();
        capi.Shader.ReloadShaders();
        RefreshValues();
    }

    // Token: 0x06000179 RID: 377 RVA: 0x00003204 File Offset: 0x00001404
    private void ToggleSSDO(bool on)
    {
        if (on && ClientSettings.SSAOQuality == 0)
        {
            ClientSettings.SSAOQuality = 1;
            capi.GetClientPlatformAbstract().RebuildFrameBuffers();
        }

        ModSettings.SSDOEnabled = on;
        capi.Shader.ReloadShaders();
        RefreshValues();
    }

    // Token: 0x0600017A RID: 378 RVA: 0x00003243 File Offset: 0x00001443
    private void ToggleOverexposure(bool on)
    {
        if (on && ModSettings.OverexposureIntensity <= 0)
        {
            ModSettings.OverexposureIntensity = 50;
        }
        else if (!on && ModSettings.OverexposureIntensity > 0)
        {
            ModSettings.OverexposureIntensity = 0;
        }

        capi.Shader.ReloadShaders();
        RefreshValues();
    }

    // Token: 0x0600017B RID: 379 RVA: 0x00003281 File Offset: 0x00001481
    private bool OnVolumetricAdvancedClicked()
    {
        TryClose();
        new VolumetricLightingGui(capi).TryOpen();
        return true;
    }

    // Token: 0x0600017C RID: 380 RVA: 0x0000329C File Offset: 0x0000149C
    private bool OnSSRAdvancedClicked()
    {
        TryClose();
        new ScreenSpaceReflectionsGui(capi).TryOpen();
        return true;
    }

    // Token: 0x0600017D RID: 381 RVA: 0x000032B7 File Offset: 0x000014B7
    private bool OnOverexposureAdvancedClicked()
    {
        TryClose();
        new OverexposureGui(capi).TryOpen();
        return true;
    }

    // Token: 0x0600017E RID: 382 RVA: 0x000032D2 File Offset: 0x000014D2
    private bool OnShadowTweaksAdvancedClicked()
    {
        TryClose();
        new ShadowTweaksGui(capi).TryOpen();
        return true;
    }
}