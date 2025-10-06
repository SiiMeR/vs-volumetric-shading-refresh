using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShadingRefreshed.VolumetricShading.Gui;

public class ConfigGui : MainConfigDialog
{
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
        SetupDialog();
        capi.Settings.AddWatcher<int>("godRays", delegate { RefreshValues(); });
    }


    public override string ToggleKeyCombinationCode => "volumetriclightingconfigure";

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

    private void ToggleScreenSpaceReflections(bool on)
    {
        ModSettings.ScreenSpaceReflectionsEnabled = on;
        capi.GetClientPlatformAbstract().RebuildFrameBuffers();
        capi.Shader.ReloadShaders();
        RefreshValues();
    }

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

    private bool OnVolumetricAdvancedClicked()
    {
        TryClose();
        new VolumetricLightingGui(capi).TryOpen();
        return true;
    }

    private bool OnSSRAdvancedClicked()
    {
        TryClose();
        new ScreenSpaceReflectionsGui(capi).TryOpen();
        return true;
    }

    private bool OnOverexposureAdvancedClicked()
    {
        TryClose();
        new OverexposureGui(capi).TryOpen();
        return true;
    }

    private bool OnShadowTweaksAdvancedClicked()
    {
        TryClose();
        new ShadowTweaksGui(capi).TryOpen();
        return true;
    }
}