using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace volumetricshadingupdated.VolumetricShading.Gui
{
	// Token: 0x02000031 RID: 49
	public abstract class AdvancedOptionsDialog : GuiDialog
	{
		// Token: 0x0600015E RID: 350 RVA: 0x0000307A File Offset: 0x0000127A
		protected AdvancedOptionsDialog(ICoreClientAPI capi)
			: base(capi)
		{
		}

		// Token: 0x0600015F RID: 351 RVA: 0x0000308E File Offset: 0x0000128E
		protected void RegisterOption(AdvancedOptionsDialog.ConfigOption option)
		{
			this.ConfigOptions.Add(option);
		}

		// Token: 0x06000160 RID: 352 RVA: 0x00006234 File Offset: 0x00004434
		protected void SetupDialog()
		{
			this._isSetup = true;
			ElementBounds dialogBounds = ElementStdBounds.AutosizedMainDialog.WithAlignment(EnumDialogArea.RightBottom).WithFixedAlignmentOffset(-GuiStyle.DialogToScreenPadding, -GuiStyle.DialogToScreenPadding);
			CairoFont font = CairoFont.WhiteSmallText();
			ElementBounds switchBounds = ElementBounds.Fixed(250.0, GuiStyle.TitleBarHeight, 20.0, 20.0);
			ElementBounds textBounds = ElementBounds.Fixed(0.0, GuiStyle.TitleBarHeight + 1.0, 240.0, 20.0);
			ElementBounds bgBounds = ElementBounds.Fill.WithFixedPadding(GuiStyle.ElementToDialogPadding);
			bgBounds.BothSizing = ElementSizing.FitToChildren;
			GuiComposer composer = this.capi.Gui.CreateCompo(this.DialogKey, dialogBounds).AddShadedDialogBG(bgBounds, true, 5.0, 0.75f).AddDialogTitleBar(this.DialogTitle, new Action(this.OnTitleBarCloseClicked), null, null)
				.BeginChildElements(bgBounds);
			foreach (AdvancedOptionsDialog.ConfigOption option3 in this.ConfigOptions)
			{
				composer.AddStaticText(option3.Text, font, textBounds, null);
				if (option3.Tooltip != null)
				{
					composer.AddHoverText(option3.Tooltip, font, 260, textBounds, null);
				}
				if (option3.SliderKey != null)
				{
					composer.AddSlider(option3.SlideAction, switchBounds.FlatCopy().WithFixedWidth(200.0), option3.SliderKey);
				}
				else if (option3.SwitchKey != null)
				{
					composer.AddSwitch(option3.ToggleAction, switchBounds, option3.SwitchKey, 20.0, 4.0);
				}
				textBounds = textBounds.BelowCopy(0.0, 10.0, 0.0, 0.0);
				switchBounds = switchBounds.BelowCopy(0.0, 10.0, 0.0, 0.0);
			}
			base.SingleComposer = composer.EndChildElements().Compose(true);
			foreach (AdvancedOptionsDialog.ConfigOption option2 in this.ConfigOptions.Where((AdvancedOptionsDialog.ConfigOption option) => option.SliderKey != null && !option.InstantSlider))
			{
				base.SingleComposer.GetSlider(option2.SliderKey).TriggerOnlyOnMouseUp(true);
			}
		}

		// Token: 0x06000161 RID: 353 RVA: 0x0000309C File Offset: 0x0000129C
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
			VolumetricShadingMod.Instance.CurrentDialog = this;
			this.RefreshValues();
			return true;
		}

		// Token: 0x1700004C RID: 76
		// (get) Token: 0x06000162 RID: 354
		protected abstract string DialogKey { get; }

		// Token: 0x1700004D RID: 77
		// (get) Token: 0x06000163 RID: 355
		protected abstract string DialogTitle { get; }

		// Token: 0x06000164 RID: 356
		protected abstract void RefreshValues();

		// Token: 0x06000165 RID: 357 RVA: 0x000030C8 File Offset: 0x000012C8
		protected void OnTitleBarCloseClicked()
		{
			this.TryClose();
			VolumetricShadingMod.Instance.ConfigGui.TryOpen();
		}

		// Token: 0x1700004E RID: 78
		// (get) Token: 0x06000166 RID: 358 RVA: 0x000030E1 File Offset: 0x000012E1
		public override string ToggleKeyCombinationCode
		{
			get
			{
				return null;
			}
		}

		// Token: 0x040000A0 RID: 160
		protected List<AdvancedOptionsDialog.ConfigOption> ConfigOptions = new List<AdvancedOptionsDialog.ConfigOption>();

		// Token: 0x040000A1 RID: 161
		private bool _isSetup;

		// Token: 0x02000032 RID: 50
		public class ConfigOption
		{
			// Token: 0x040000A2 RID: 162
			public string SwitchKey;

			// Token: 0x040000A3 RID: 163
			public string SliderKey;

			// Token: 0x040000A4 RID: 164
			public string Text;

			// Token: 0x040000A5 RID: 165
			public string Tooltip;

			// Token: 0x040000A6 RID: 166
			public Action<bool> ToggleAction;

			// Token: 0x040000A7 RID: 167
			public ActionConsumable<int> SlideAction;

			// Token: 0x040000A8 RID: 168
			public bool InstantSlider;
		}
	}
}
