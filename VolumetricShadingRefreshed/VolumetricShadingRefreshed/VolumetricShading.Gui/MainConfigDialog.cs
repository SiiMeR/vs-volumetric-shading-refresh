using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace volumetricshadingupdated.VolumetricShading.Gui
{
	// Token: 0x02000034 RID: 52
	public abstract class MainConfigDialog : GuiDialog
	{
		// Token: 0x0600016B RID: 363 RVA: 0x00003105 File Offset: 0x00001305
		protected MainConfigDialog(ICoreClientAPI capi)
			: base(capi)
		{
		}

		// Token: 0x0600016C RID: 364 RVA: 0x00003119 File Offset: 0x00001319
		protected void RegisterOption(MainConfigDialog.ConfigOption option)
		{
			this.ConfigOptions.Add(option);
		}

		// Token: 0x0600016D RID: 365 RVA: 0x00006504 File Offset: 0x00004704
		protected void SetupDialog()
		{
			this._isSetup = true;
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightBottom).WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, -GuiStyle.DialogToScreenPadding);
			CairoFont font = CairoFont.WhiteSmallText();
			ElementBounds switchBounds = ElementBounds.Fixed(210.0, GuiStyle.TitleBarHeight, 20.0, 20.0);
			ElementBounds textBounds = ElementBounds.Fixed(0.0, GuiStyle.TitleBarHeight + 1.0, 200.0, 20.0);
			ElementBounds advancedButtonBounds = ElementBounds.Fixed(240.0, GuiStyle.TitleBarHeight, 110.0, 20.0);
			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
			bgBounds.BothSizing = ElementSizing.FitToChildren;
			GuiComposer composer = this.capi.Gui.CreateCompo("volumetricShadingConfigure", dialogBounds).AddShadedDialogBG(bgBounds, true, 5.0, 0.75f).AddDialogTitleBar("Volumetric Shading Configuration", new Action(this.OnTitleBarCloseClicked), null, null)
				.BeginChildElements(bgBounds);
			foreach (MainConfigDialog.ConfigOption option in this.ConfigOptions)
			{
				composer.AddStaticText(option.Text, font, textBounds, null);
				if (option.Tooltip != null)
				{
					composer.AddHoverText(option.Tooltip, font, 250, textBounds.FlatCopy(), null);
				}
				if (option.SwitchKey != null)
				{
					composer.AddSwitch(option.ToggleAction, switchBounds, option.SwitchKey, 20.0, 4.0);
				}
				if (option.AdvancedAction != null)
				{
					composer.AddSmallButton("Advanced...", option.AdvancedAction, advancedButtonBounds, EnumButtonStyle.Normal, null);
				}
				switchBounds = switchBounds.BelowCopy(0.0, 10.0, 0.0, 0.0);
				textBounds = textBounds.BelowCopy(0.0, 10.0, 0.0, 0.0);
				advancedButtonBounds = advancedButtonBounds.BelowCopy(0.0, 10.0, 0.0, 0.0);
			}
			base.SingleComposer = composer.EndChildElements().Compose(true);
		}

		// Token: 0x0600016E RID: 366 RVA: 0x00003127 File Offset: 0x00001327
		public override bool TryOpen()
		{
			if (!this._isSetup)
			{
				this.SetupDialog();
			}
			if (!base.TryOpen())
			{
				return false;
			}
			this.RefreshValues();
			VolumetricShadingMod.Instance.CurrentDialog = this;
			return true;
		}

		// Token: 0x0600016F RID: 367 RVA: 0x00003153 File Offset: 0x00001353
		private void OnTitleBarCloseClicked()
		{
			this.TryClose();
		}

		// Token: 0x06000170 RID: 368
		protected abstract void RefreshValues();

		// Token: 0x040000AB RID: 171
		private bool _isSetup;

		// Token: 0x040000AC RID: 172
		protected List<MainConfigDialog.ConfigOption> ConfigOptions = new List<MainConfigDialog.ConfigOption>();

		// Token: 0x02000035 RID: 53
		public class ConfigOption
		{
			// Token: 0x040000AD RID: 173
			public ActionConsumable AdvancedAction;

			// Token: 0x040000AE RID: 174
			public string SwitchKey;

			// Token: 0x040000AF RID: 175
			public string Text;

			// Token: 0x040000B0 RID: 176
			public Action<bool> ToggleAction;

			// Token: 0x040000B1 RID: 177
			public string Tooltip;
		}
	}
}
