using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading.Gui
{
    // Token: 0x02000036 RID: 54
    public class ConfigGui : MainConfigDialog
    {
        // Token: 0x06000172 RID: 370 RVA: 0x00006794 File Offset: 0x00004994
        public ConfigGui(ICoreClientAPI capi)
            : base(capi)
        {
            base.RegisterOption(new MainConfigDialog.ConfigOption
            {
                SwitchKey = "toggleVolumetricLighting",
                Text = "Volumetric Lighting",
                Tooltip = "Enables realistic scattering of light",
                ToggleAction = new Action<bool>(this.ToggleVolumetricLighting),
                AdvancedAction = new ActionConsumable(this.OnVolumetricAdvancedClicked)
            });
            base.RegisterOption(new MainConfigDialog.ConfigOption
            {
                SwitchKey = "toggleSSR",
                Text = "Screen Space Reflections",
                Tooltip = "Enables reflections, for example on water",
                ToggleAction = new Action<bool>(this.ToggleScreenSpaceReflections),
                AdvancedAction = new ActionConsumable(this.OnSSRAdvancedClicked)
            });
            base.RegisterOption(new MainConfigDialog.ConfigOption
            {
                SwitchKey = "toggleOverexposure",
                Text = "Overexposure",
                Tooltip = "Adds overexposure at brightly sunlit places",
                ToggleAction = new Action<bool>(this.ToggleOverexposure),
                AdvancedAction = new ActionConsumable(this.OnOverexposureAdvancedClicked)
            });
            base.RegisterOption(new MainConfigDialog.ConfigOption
            {
                Text = "Shadow Tweaks",
                Tooltip = "Allows for some shadow tweaks that might make them look better",
                AdvancedAction = new ActionConsumable(this.OnShadowTweaksAdvancedClicked)
            });
            base.RegisterOption(new MainConfigDialog.ConfigOption
            {
                SwitchKey = "toggleUnderwater",
                Text = "Underwater Tweaks",
                Tooltip = "Better underwater looks",
                ToggleAction = new Action<bool>(this.ToggleUnderwater)
            });
            base.RegisterOption(new MainConfigDialog.ConfigOption
            {
                SwitchKey = "toggleDeferred",
                Text = "Deferred Lighting",
                Tooltip = "Aims to improve lighting performance by deferring lighting operations. Requires SSAO.",
                ToggleAction = new Action<bool>(this.ToggleDeferredLighting)
            });
            base.RegisterOption(new MainConfigDialog.ConfigOption
            {
                SwitchKey = "toggleSSDO",
                Text = "Improve SSAO",
                Tooltip = "Replaces SSAO with SSDO. Results in marginally faster and better looking occlusions.",
                ToggleAction = new Action<bool>(this.ToggleSSDO)
            });
            base.SetupDialog();
            capi.Settings.AddWatcher<int>("godRays", delegate(int _) { this.RefreshValues(); });
        }

        // Token: 0x1700004F RID: 79
        // (get) Token: 0x06000173 RID: 371 RVA: 0x0000315C File Offset: 0x0000135C
        public override string ToggleKeyCombinationCode
        {
            get { return "volumetriclightingconfigure"; }
        }

        // Token: 0x06000174 RID: 372 RVA: 0x000069A8 File Offset: 0x00004BA8
        protected override void RefreshValues()
        {
            if (!this.IsOpened())
            {
                return;
            }

            base.SingleComposer.GetSwitch("toggleVolumetricLighting").On = ClientSettings.GodRayQuality > 0;
            base.SingleComposer.GetSwitch("toggleSSR").On = ModSettings.ScreenSpaceReflectionsEnabled;
            base.SingleComposer.GetSwitch("toggleSSDO").On = ModSettings.SSDOEnabled;
            base.SingleComposer.GetSwitch("toggleOverexposure").On = ModSettings.OverexposureIntensity > 0;
            base.SingleComposer.GetSwitch("toggleUnderwater").On = ModSettings.UnderwaterTweaksEnabled;
            base.SingleComposer.GetSwitch("toggleDeferred").On = ModSettings.DeferredLightingEnabled;
        }

        // Token: 0x06000175 RID: 373 RVA: 0x00003163 File Offset: 0x00001363
        private void ToggleUnderwater(bool enabled)
        {
            ModSettings.UnderwaterTweaksEnabled = enabled;
            this.RefreshValues();
        }

        // Token: 0x06000176 RID: 374 RVA: 0x00003171 File Offset: 0x00001371
        private void ToggleDeferredLighting(bool enabled)
        {
            ModSettings.DeferredLightingEnabled = enabled;
            this.capi.GetClientPlatformAbstract().RebuildFrameBuffers();
            this.capi.Shader.ReloadShaders();
            this.RefreshValues();
        }

        // Token: 0x06000177 RID: 375 RVA: 0x000031A0 File Offset: 0x000013A0
        private void ToggleVolumetricLighting(bool on)
        {
            if (on && ClientSettings.ShadowMapQuality == 0)
            {
                ClientSettings.ShadowMapQuality = 1;
            }

            ClientSettings.GodRayQuality = (on ? 1 : 0);
            this.capi.Shader.ReloadShaders();
            this.RefreshValues();
        }

        // Token: 0x06000178 RID: 376 RVA: 0x000031D5 File Offset: 0x000013D5
        private void ToggleScreenSpaceReflections(bool on)
        {
            ModSettings.ScreenSpaceReflectionsEnabled = on;
            this.capi.GetClientPlatformAbstract().RebuildFrameBuffers();
            this.capi.Shader.ReloadShaders();
            this.RefreshValues();
        }

        // Token: 0x06000179 RID: 377 RVA: 0x00003204 File Offset: 0x00001404
        private void ToggleSSDO(bool on)
        {
            if (on && ClientSettings.SSAOQuality == 0)
            {
                ClientSettings.SSAOQuality = 1;
                this.capi.GetClientPlatformAbstract().RebuildFrameBuffers();
            }

            ModSettings.SSDOEnabled = on;
            this.capi.Shader.ReloadShaders();
            this.RefreshValues();
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

            this.capi.Shader.ReloadShaders();
            this.RefreshValues();
        }

        // Token: 0x0600017B RID: 379 RVA: 0x00003281 File Offset: 0x00001481
        private bool OnVolumetricAdvancedClicked()
        {
            this.TryClose();
            new VolumetricLightingGui(this.capi).TryOpen();
            return true;
        }

        // Token: 0x0600017C RID: 380 RVA: 0x0000329C File Offset: 0x0000149C
        private bool OnSSRAdvancedClicked()
        {
            this.TryClose();
            new ScreenSpaceReflectionsGui(this.capi).TryOpen();
            return true;
        }

        // Token: 0x0600017D RID: 381 RVA: 0x000032B7 File Offset: 0x000014B7
        private bool OnOverexposureAdvancedClicked()
        {
            this.TryClose();
            new OverexposureGui(this.capi).TryOpen();
            return true;
        }

        // Token: 0x0600017E RID: 382 RVA: 0x000032D2 File Offset: 0x000014D2
        private bool OnShadowTweaksAdvancedClicked()
        {
            this.TryClose();
            new ShadowTweaksGui(this.capi).TryOpen();
            return true;
        }
    }
}