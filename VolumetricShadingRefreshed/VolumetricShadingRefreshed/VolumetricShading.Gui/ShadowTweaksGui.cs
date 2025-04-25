using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace volumetricshadingupdated.VolumetricShading.Gui
{
    // Token: 0x02000039 RID: 57
    public class ShadowTweaksGui : AdvancedOptionsDialog
    {
        // Token: 0x17000054 RID: 84
        // (get) Token: 0x06000193 RID: 403 RVA: 0x0000346F File Offset: 0x0000166F
        protected override string DialogKey
        {
            get { return "vsmodShadowTweaksConfigure"; }
        }

        // Token: 0x17000055 RID: 85
        // (get) Token: 0x06000194 RID: 404 RVA: 0x00003476 File Offset: 0x00001676
        protected override string DialogTitle
        {
            get { return "Shadow Tweaks"; }
        }

        // Token: 0x06000195 RID: 405 RVA: 0x00006EB0 File Offset: 0x000050B0
        public ShadowTweaksGui(ICoreClientAPI capi)
            : base(capi)
        {
            base.RegisterOption(new AdvancedOptionsDialog.ConfigOption
            {
                SliderKey = "shadowBaseWidthSlider",
                Text = "Near base width",
                Tooltip =
                    "Sets the base width of the near shadow map. Increases sharpness of near shadows,but decreases sharpness of mid-distance ones. Unmodified game value is 30.",
                SlideAction = new ActionConsumable<int>(this.OnShadowBaseWidthSliderChanged),
                InstantSlider = true
            });
            base.RegisterOption(new AdvancedOptionsDialog.ConfigOption
            {
                SwitchKey = "softShadowsEnabled",
                Text = "Soft shadows (slow)",
                Tooltip =
                    "Soft shadows based on occluder distance to shadow surface. Can be very slow in difficult scenes, highly recommended to use in combination with deferred lighting.",
                ToggleAction = new Action<bool>(this.OnSoftShadowsToggled)
            });
            base.RegisterOption(new AdvancedOptionsDialog.ConfigOption
            {
                SliderKey = "softShadowSamples",
                Text = "Soft shadow samples",
                Tooltip = "Amount of soft shadow samples. More samples = better looks, but slower.",
                SlideAction = new ActionConsumable<int>(this.OnSoftShadowSamplesChanged)
            });
            base.RegisterOption(new AdvancedOptionsDialog.ConfigOption
            {
                SliderKey = "nearPeterPanningSlider",
                Text = "Near offset adjustment",
                Tooltip = "Adjusts the near shadow map Z offset. Reduces peter panning, but might lead to artifacts.",
                SlideAction = new ActionConsumable<int>(this.OnNearPeterPanningChanged)
            });
            base.RegisterOption(new AdvancedOptionsDialog.ConfigOption
            {
                SliderKey = "farPeterPanningSlider",
                Text = "Far offset adjustment",
                Tooltip = "Adjusts the far shadow map Z offset. Reduces peter panning, but might lead to artifacts.",
                SlideAction = new ActionConsumable<int>(this.OnFarPeterPanningChanged)
            });
        }

        // Token: 0x06000196 RID: 406 RVA: 0x00007004 File Offset: 0x00005204
        protected override void RefreshValues()
        {
            base.SingleComposer.GetSlider("shadowBaseWidthSlider")
                .SetValues(ModSettings.NearShadowBaseWidth, 5, 30, 1, "");
            base.SingleComposer.GetSwitch("softShadowsEnabled").SetValue(ModSettings.SoftShadowsEnabled);
            base.SingleComposer.GetSlider("softShadowSamples").SetValues(ModSettings.SoftShadowSamples, 1, 64, 1, "");
            base.SingleComposer.GetSlider("nearPeterPanningSlider")
                .SetValues(ModSettings.NearPeterPanningAdjustment, 0, 4, 1, "");
            base.SingleComposer.GetSlider("farPeterPanningSlider")
                .SetValues(ModSettings.FarPeterPanningAdjustment, 0, 8, 1, "");
        }

        // Token: 0x06000197 RID: 407 RVA: 0x0000347D File Offset: 0x0000167D
        private void OnSoftShadowsToggled(bool enabled)
        {
            ModSettings.SoftShadowsEnabled = enabled;
            this.capi.Shader.ReloadShaders();
        }

        // Token: 0x06000198 RID: 408 RVA: 0x00003496 File Offset: 0x00001696
        private bool OnSoftShadowSamplesChanged(int value)
        {
            ModSettings.SoftShadowSamples = value;
            this.capi.Shader.ReloadShaders();
            return true;
        }

        // Token: 0x06000199 RID: 409 RVA: 0x000034B0 File Offset: 0x000016B0
        private bool OnShadowBaseWidthSliderChanged(int value)
        {
            ModSettings.NearShadowBaseWidth = value;
            return true;
        }

        // Token: 0x0600019A RID: 410 RVA: 0x000034B9 File Offset: 0x000016B9
        private bool OnNearPeterPanningChanged(int value)
        {
            ModSettings.NearPeterPanningAdjustment = value;
            this.capi.Shader.ReloadShaders();
            return true;
        }

        // Token: 0x0600019B RID: 411 RVA: 0x000034D3 File Offset: 0x000016D3
        private bool OnFarPeterPanningChanged(int value)
        {
            ModSettings.FarPeterPanningAdjustment = value;
            this.capi.Shader.ReloadShaders();
            return true;
        }
    }
}