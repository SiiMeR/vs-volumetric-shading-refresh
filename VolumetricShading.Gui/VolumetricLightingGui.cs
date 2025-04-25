using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading.Gui
{
    // Token: 0x0200003A RID: 58
    public class VolumetricLightingGui : AdvancedOptionsDialog
    {
        // Token: 0x17000056 RID: 86
        // (get) Token: 0x0600019C RID: 412 RVA: 0x000034ED File Offset: 0x000016ED
        protected override string DialogKey
        {
            get { return "vsmodVolumetricLightingConfigure"; }
        }

        // Token: 0x17000057 RID: 87
        // (get) Token: 0x0600019D RID: 413 RVA: 0x000034F4 File Offset: 0x000016F4
        protected override string DialogTitle
        {
            get { return "Volumetric Lighting Options"; }
        }

        // Token: 0x0600019E RID: 414 RVA: 0x000070B8 File Offset: 0x000052B8
        public VolumetricLightingGui(ICoreClientAPI capi)
            : base(capi)
        {
            base.RegisterOption(new AdvancedOptionsDialog.ConfigOption
            {
                SwitchKey = "toggleVolumetricLighting",
                Text = "Enable Volumetric Lighting",
                ToggleAction = new Action<bool>(this.ToggleVolumetricLighting)
            });
            base.RegisterOption(new AdvancedOptionsDialog.ConfigOption
            {
                SliderKey = "intensitySlider",
                Text = "Intensity",
                Tooltip = "The intensity of the Volumetric Lighting effect",
                SlideAction = new ActionConsumable<int>(this.OnIntensitySliderChanged)
            });
            base.RegisterOption(new AdvancedOptionsDialog.ConfigOption
            {
                SliderKey = "flatnessSlider",
                Text = "Flatness",
                Tooltip = "Defines how noticeable the difference between low and high scattering is",
                SlideAction = new ActionConsumable<int>(this.OnFlatnessSliderChanged)
            });
        }

        // Token: 0x0600019F RID: 415 RVA: 0x0000717C File Offset: 0x0000537C
        protected override void RefreshValues()
        {
            if (!this.IsOpened())
            {
                return;
            }

            base.SingleComposer.GetSwitch("toggleVolumetricLighting").On = ClientSettings.GodRayQuality > 0;
            base.SingleComposer.GetSlider("flatnessSlider")
                .SetValues(ModSettings.VolumetricLightingFlatness, 1, 199, 1, "");
            base.SingleComposer.GetSlider("intensitySlider")
                .SetValues(ModSettings.VolumetricLightingIntensity, 1, 100, 1, "");
        }

        // Token: 0x060001A0 RID: 416 RVA: 0x000034FB File Offset: 0x000016FB
        private void ToggleVolumetricLighting(bool on)
        {
            ClientSettings.GodRayQuality = (on ? 1 : 0);
            if (on && ClientSettings.ShadowMapQuality == 0)
            {
                ClientSettings.ShadowMapQuality = 1;
            }

            this.capi.Shader.ReloadShaders();
            this.RefreshValues();
        }

        // Token: 0x060001A1 RID: 417 RVA: 0x00003530 File Offset: 0x00001730
        private bool OnFlatnessSliderChanged(int value)
        {
            ModSettings.VolumetricLightingFlatness = value;
            this.capi.Shader.ReloadShaders();
            this.RefreshValues();
            return true;
        }

        // Token: 0x060001A2 RID: 418 RVA: 0x00003550 File Offset: 0x00001750
        private bool OnIntensitySliderChanged(int value)
        {
            ModSettings.VolumetricLightingIntensity = value;
            this.capi.Shader.ReloadShaders();
            this.RefreshValues();
            return true;
        }
    }
}