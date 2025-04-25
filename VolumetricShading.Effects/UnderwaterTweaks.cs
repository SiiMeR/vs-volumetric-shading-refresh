using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace VolumetricShading.Effects;

public class UnderwaterTweaks
{
	private VolumetricShadingMod _mod;

	private bool _enabled;

	private float _oldFogDensity;

	private float _oldFogMin;

	private WeightedFloatArray _ambient;

	private WeightedFloatArray _oldAmbient;

	public UnderwaterTweaks(VolumetricShadingMod mod)
	{
		_mod = mod;
		mod.CApi.Settings.AddWatcher<bool>("volumetricshading_underwaterTweaks", (OnSettingsChanged<bool>)SetEnabled);
		SetEnabled(ModSettings.UnderwaterTweaksEnabled);
		mod.Events.PostWaterChangeSight += OnWaterModifierChanged;
	}

	private void SetEnabled(bool enabled)
	{
		if (enabled && !_enabled)
		{
			PatchAmbientManager();
		}
		else if (!enabled && _enabled)
		{
			RestoreAmbientManager();
		}
		_enabled = enabled;
	}

	private void RestoreAmbientManager()
	{
		AmbientModifier obj = _mod.CApi.Ambient.CurrentModifiers["water"];
		((WeightedValue<float>)(object)obj.FogDensity).Value = _oldFogDensity;
		((WeightedValue<float>)(object)obj.FogMin).Value = _oldFogMin;
		obj.AmbientColor = _oldAmbient;
	}

	private void PatchAmbientManager()
	{
		AmbientModifier val = _mod.CApi.Ambient.CurrentModifiers["water"];
		_oldFogDensity = ((WeightedValue<float>)(object)val.FogDensity).Value;
		_oldFogMin = ((WeightedValue<float>)(object)val.FogMin).Value;
		_oldAmbient = val.AmbientColor;
		((WeightedValue<float>)(object)val.FogMin).Value = 0.25f;
		((WeightedValue<float>)(object)val.FogDensity).Value = 0.015f;
		_ambient = val.AmbientColor.Clone();
		val.AmbientColor = _ambient;
	}

	private void OnWaterModifierChanged()
	{
		if (_enabled)
		{
			AmbientModifier obj = _mod.CApi.Ambient.CurrentModifiers["water"];
			obj.AmbientColor = _ambient;
			float[] value = ((WeightedValue<float[]>)(object)_ambient).Value;
			value[0] = 1.5f;
			value[1] = 1.5f;
			value[2] = 2f;
			((WeightedValue<float[]>)(object)obj.FogColor).Weight = ((WeightedValue<float[]>)(object)_ambient).Weight;
			float[] value2 = ((WeightedValue<float[]>)(object)obj.FogColor).Value;
			int num = (int)(value2[0] * 255f) | ((int)(value2[1] * 255f) << 8) | ((int)(value2[2] * 255f) << 16);
			num = ColorUtil.ColorOverlay(num, 14057728, 0.5f);
			value2[0] = (float)(num & 0xFF) / 255f;
			value2[1] = (float)((num >> 8) & 0xFF) / 255f;
			value2[2] = (float)((num >> 16) & 0xFF) / 255f;
		}
	}
}
