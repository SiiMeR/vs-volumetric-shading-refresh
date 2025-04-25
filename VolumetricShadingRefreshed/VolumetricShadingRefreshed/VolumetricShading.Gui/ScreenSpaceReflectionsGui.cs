using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace volumetricshadingupdated.VolumetricShading.Gui
{
    // Token: 0x02000038 RID: 56
    public class ScreenSpaceReflectionsGui : AdvancedOptionsDialog
    {
        // Token: 0x17000052 RID: 82
        // (get) Token: 0x06000186 RID: 390 RVA: 0x00003326 File Offset: 0x00001526
        protected override string DialogKey
        {
            get { return "vsmodSSRConfigure"; }
        }

        // Token: 0x17000053 RID: 83
        // (get) Token: 0x06000187 RID: 391 RVA: 0x0000332D File Offset: 0x0000152D
        protected override string DialogTitle
        {
            get { return "Screen Space Reflections Options"; }
        }

        // Token: 0x06000188 RID: 392 RVA: 0x00006B50 File Offset: 0x00004D50
        public ScreenSpaceReflectionsGui(ICoreClientAPI capi)
            : base(capi)
        {
            base.RegisterOption(new AdvancedOptionsDialog.ConfigOption
            {
                SwitchKey = "toggleSSR",
                Text = "Enable Screen Space Reflections",
                ToggleAction = new Action<bool>(this.ToggleSSR)
            });
            base.RegisterOption(new AdvancedOptionsDialog.ConfigOption
            {
                SwitchKey = "toggleRefractions",
                Text = "Enable refractions",
                Tooltip = "Enables refractions for water, ice, and glass",
                ToggleAction = new Action<bool>(this.ToggleRefractions)
            });
            base.RegisterOption(new AdvancedOptionsDialog.ConfigOption
            {
                SwitchKey = "toggleCaustics",
                Text = "Enable caustics",
                Tooltip = "Enables bands of light underwater caused by waves on the surface",
                ToggleAction = new Action<bool>(this.ToggleCaustics)
            });
            base.RegisterOption(new AdvancedOptionsDialog.ConfigOption
            {
                SwitchKey = "toggleRainReflections",
                Text = "Enable wet grass",
                Tooltip = "Enables reflecting grass when raining",
                ToggleAction = new Action<bool>(this.ToggleRainReflections)
            });
            base.RegisterOption(new AdvancedOptionsDialog.ConfigOption
            {
                SliderKey = "dimmingSlider",
                Text = "Reflection dimming",
                Tooltip = "The dimming effect strength on the reflected image",
                SlideAction = new ActionConsumable<int>(this.OnDimmingSliderChanged)
            });
            base.RegisterOption(new AdvancedOptionsDialog.ConfigOption
            {
                SliderKey = "transparencySlider",
                Text = "Water transparency",
                Tooltip = "Sets the transparency of the vanilla water effect",
                SlideAction = new ActionConsumable<int>(this.OnTransparencySliderChanged)
            });
            base.RegisterOption(new AdvancedOptionsDialog.ConfigOption
            {
                SliderKey = "splashTransparencySlider",
                Text = "Splash transparency",
                Tooltip = "The strength of the vanilla splash effect",
                SlideAction = new ActionConsumable<int>(this.OnSplashTransparencySliderChanged)
            });
            base.RegisterOption(new AdvancedOptionsDialog.ConfigOption
            {
                SliderKey = "tintSlider",
                Text = "Tint influence",
                Tooltip = "Sets the influence an object's tint has on it's reflection color",
                SlideAction = new ActionConsumable<int>(this.OnTintSliderChanged)
            });
            base.RegisterOption(new AdvancedOptionsDialog.ConfigOption
            {
                SliderKey = "skyMixinSlider",
                Text = "Sky mixin",
                Tooltip = "The amount of sky color that is always visible, even when fully reflecting",
                SlideAction = new ActionConsumable<int>(this.OnSkyMixinSliderChanged)
            });
        }

        // Token: 0x06000189 RID: 393 RVA: 0x00006D88 File Offset: 0x00004F88
        protected override void RefreshValues()
        {
            base.SingleComposer.GetSwitch("toggleSSR").SetValue(ModSettings.ScreenSpaceReflectionsEnabled);
            base.SingleComposer.GetSwitch("toggleRefractions").SetValue(ModSettings.SSRRefractionsEnabled);
            base.SingleComposer.GetSwitch("toggleCaustics").SetValue(ModSettings.SSRCausticsEnabled);
            base.SingleComposer.GetSwitch("toggleRainReflections").SetValue(ModSettings.SSRRainReflectionsEnabled);
            base.SingleComposer.GetSlider("dimmingSlider").SetValues(ModSettings.SSRReflectionDimming, 1, 400, 1, "");
            base.SingleComposer.GetSlider("transparencySlider")
                .SetValues(ModSettings.SSRWaterTransparency, 0, 100, 1, "");
            base.SingleComposer.GetSlider("tintSlider").SetValues(ModSettings.SSRTintInfluence, 0, 100, 1, "");
            base.SingleComposer.GetSlider("skyMixinSlider").SetValues(ModSettings.SSRSkyMixin, 0, 100, 1, "");
            base.SingleComposer.GetSlider("splashTransparencySlider")
                .SetValues(ModSettings.SSRSplashTransparency, 0, 100, 1, "");
        }

        // Token: 0x0600018A RID: 394 RVA: 0x00003334 File Offset: 0x00001534
        private void ToggleSSR(bool on)
        {
            ModSettings.ScreenSpaceReflectionsEnabled = on;
            this.capi.GetClientPlatformAbstract().RebuildFrameBuffers();
            this.capi.Shader.ReloadShaders();
            this.RefreshValues();
        }

        // Token: 0x0600018B RID: 395 RVA: 0x00003363 File Offset: 0x00001563
        private void ToggleCaustics(bool enabled)
        {
            ModSettings.SSRCausticsEnabled = enabled;
            this.capi.GetClientPlatformAbstract().RebuildFrameBuffers();
            this.capi.Shader.ReloadShaders();
            this.RefreshValues();
        }

        // Token: 0x0600018C RID: 396 RVA: 0x00003392 File Offset: 0x00001592
        private void ToggleRefractions(bool on)
        {
            ModSettings.SSRRefractionsEnabled = on;
            this.capi.GetClientPlatformAbstract().RebuildFrameBuffers();
            this.capi.Shader.ReloadShaders();
            this.RefreshValues();
        }

        // Token: 0x0600018D RID: 397 RVA: 0x000033C1 File Offset: 0x000015C1
        private void ToggleRainReflections(bool on)
        {
            ModSettings.SSRRainReflectionsEnabled = on;
            this.RefreshValues();
        }

        // Token: 0x0600018E RID: 398 RVA: 0x000033CF File Offset: 0x000015CF
        private bool OnDimmingSliderChanged(int value)
        {
            ModSettings.SSRReflectionDimming = value;
            this.capi.Shader.ReloadShaders();
            this.RefreshValues();
            return true;
        }

        // Token: 0x0600018F RID: 399 RVA: 0x000033EF File Offset: 0x000015EF
        private bool OnTransparencySliderChanged(int value)
        {
            ModSettings.SSRWaterTransparency = value;
            this.capi.Shader.ReloadShaders();
            this.RefreshValues();
            return true;
        }

        // Token: 0x06000190 RID: 400 RVA: 0x0000340F File Offset: 0x0000160F
        private bool OnSplashTransparencySliderChanged(int value)
        {
            ModSettings.SSRSplashTransparency = value;
            this.capi.Shader.ReloadShaders();
            this.RefreshValues();
            return true;
        }

        // Token: 0x06000191 RID: 401 RVA: 0x0000342F File Offset: 0x0000162F
        private bool OnTintSliderChanged(int value)
        {
            ModSettings.SSRTintInfluence = value;
            this.capi.Shader.ReloadShaders();
            this.RefreshValues();
            return true;
        }

        // Token: 0x06000192 RID: 402 RVA: 0x0000344F File Offset: 0x0000164F
        private bool OnSkyMixinSliderChanged(int value)
        {
            ModSettings.SSRSkyMixin = value;
            this.capi.Shader.ReloadShaders();
            this.RefreshValues();
            return true;
        }
    }
}