using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.Client.NoObf;
using volumetricshadingupdated.VolumetricShading.Patch;

namespace volumetricshadingupdated.VolumetricShading.Effects;

public class ShadowTweaks
{
    private readonly VolumetricShadingMod _mod;

    private bool _softShadowsEnabled;

    private int _softShadowSamples;

    public int NearShadowBaseWidth { get; private set; }

    public ISet<string> ExcludedShaders { get; }

    public ShadowTweaks(VolumetricShadingMod mod)
    {
        //IL_01b4: Unknown result type (might be due to invalid IL or missing references)
        //IL_01be: Expected O, but got Unknown
        _mod = mod;
        ExcludedShaders = new HashSet<string> { "sky", "clouds", "gui", "guigear", "guitopsoil", "texture2texture" };
        _mod.CApi.Settings.AddWatcher<int>("volumetricshading_nearShadowBaseWidth",
            (OnSettingsChanged<int>)OnNearShadowBaseWidthChanged);
        _mod.CApi.Settings.AddWatcher<bool>("volumetricshading_softShadows",
            (OnSettingsChanged<bool>)OnSoftShadowsChanged);
        _mod.CApi.Settings.AddWatcher<int>("volumetricshading_softShadowSamples",
            (OnSettingsChanged<int>)OnSoftShadowSamplesChanged);
        NearShadowBaseWidth = ModSettings.NearShadowBaseWidth;
        _softShadowsEnabled = ModSettings.SoftShadowsEnabled;
        _softShadowSamples = ModSettings.SoftShadowSamples;
        _mod.ShaderInjector.RegisterFloatProperty("VSMOD_NEARSHADOWOFFSET",
            () => ModSettings.NearPeterPanningAdjustment);
        _mod.ShaderInjector.RegisterFloatProperty("VSMOD_FARSHADOWOFFSET", () => ModSettings.FarPeterPanningAdjustment);
        _mod.ShaderInjector.RegisterBoolProperty("VSMOD_SOFTSHADOWS", () => _softShadowsEnabled);
        _mod.ShaderInjector.RegisterIntProperty("VSMOD_SOFTSHADOWSAMPLES", () => _softShadowSamples);
        _mod.CApi.Event.ReloadShader += new ActionBoolReturn(OnReloadShaders);
        _mod.Events.PostUseShader += OnUseShader;
    }

    private bool OnReloadShaders()
    {
        return true;
    }

    private void OnNearShadowBaseWidthChanged(int newVal)
    {
        NearShadowBaseWidth = newVal;
    }

    private void OnSoftShadowsChanged(bool enabled)
    {
        _softShadowsEnabled = enabled;
    }

    private void OnSoftShadowSamplesChanged(int samples)
    {
        _softShadowSamples = samples;
    }

    private void OnUseShader(ShaderProgramBase shader)
    {
        if (!_softShadowsEnabled || !shader.includes.Contains("fogandlight.fsh") ||
            ExcludedShaders.Contains(shader.PassName) || ShaderProgramBase.shadowmapQuality <= 0)
        {
            return;
        }

        if (!shader.customSamplers.ContainsKey("shadowMapFarTex"))
        {
            int[] array = new int[2];
            for (int i = 0; i < array.Length; i++)
            {
                int num = (array[i] = GL.GenSampler());
                GL.SamplerParameter(num, (SamplerParameterName)34892, 0);
                GL.SamplerParameter(num, (SamplerParameterName)10241, 9728);
                GL.SamplerParameter(num, (SamplerParameterName)10240, 9728);
                GL.SamplerParameter(num, (SamplerParameterName)4100, new float[4] { 1f, 1f, 1f, 1f });
                GL.SamplerParameter(num, (SamplerParameterName)10242, 33069);
                GL.SamplerParameter(num, (SamplerParameterName)10243, 33069);
            }

            int[] array2 = new int[2];
            for (int j = 0; j < array2.Length; j++)
            {
                int num2 = (array2[j] = GL.GenSampler());
                GL.SamplerParameter(num2, (SamplerParameterName)34892, 34894);
                GL.SamplerParameter(num2, (SamplerParameterName)34893, 515);
                GL.SamplerParameter(num2, (SamplerParameterName)10241, 9729);
                GL.SamplerParameter(num2, (SamplerParameterName)10240, 9729);
                GL.SamplerParameter(num2, (SamplerParameterName)4100, new float[4] { 1f, 1f, 1f, 1f });
                GL.SamplerParameter(num2, (SamplerParameterName)10242, 33069);
                GL.SamplerParameter(num2, (SamplerParameterName)10243, 33069);
            }

            shader.customSamplers["shadowMapFarTex"] = array[0];
            shader.customSamplers["shadowMapNearTex"] = array[1];
            shader.customSamplers["shadowMapFar"] = array2[0];
            shader.customSamplers["shadowMapNear"] = array2[1];
        }

        List<FrameBufferRef> frameBuffers = _mod.CApi.Render.FrameBuffers;
        FrameBufferRef val = frameBuffers[11];
        FrameBufferRef val2 = frameBuffers[12];
        shader.BindTexture2D("shadowMapFarTex", val.DepthTextureId);
        shader.BindTexture2D("shadowMapNearTex", val2.DepthTextureId);
        shader.BindTexture2D("shadowMapFar", val.DepthTextureId);
        shader.BindTexture2D("shadowMapNear", val2.DepthTextureId);
    }

    public void Dispose()
    {
    }
}