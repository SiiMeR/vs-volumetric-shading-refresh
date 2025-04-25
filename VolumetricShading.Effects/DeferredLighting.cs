using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using VolumetricShading.Patch;

namespace VolumetricShading.Effects;

public class DeferredLighting
{
	private readonly VolumetricShadingMod _mod;

	private readonly ClientPlatformWindows _platform;

	private ShaderProgram _shader;

	private FrameBufferRef _frameBuffer;

	private MeshRef _screenQuad;

	private bool _enabled;

	public DeferredLighting(VolumetricShadingMod mod)
	{
		//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fe: Expected O, but got Unknown
		_mod = mod;
		_platform = _mod.CApi.GetClientPlatformWindows();
		_mod.CApi.Settings.AddWatcher<bool>("volumetricshading_deferredLighting", (OnSettingsChanged<bool>)OnDeferredLightingChanged);
		_mod.CApi.Settings.AddWatcher<int>("ssaoQuality", (OnSettingsChanged<int>)OnSSAOQualityChanged);
		_enabled = ModSettings.DeferredLightingEnabled;
		_mod.CApi.Event.RegisterRenderer((IRenderer)(object)new DeferredLightingPreparer(this), (EnumRenderStage)1, "vsmod-deferred-lighting-prepare");
		_mod.CApi.Event.RegisterRenderer((IRenderer)(object)new DeferredLightingRenderer(this), (EnumRenderStage)1, "vsmod-deferred-lighting");
		_mod.ShaderInjector.RegisterBoolProperty("VSMOD_DEFERREDLIGHTING", () => _enabled);
		_mod.CApi.Event.ReloadShader += new ActionBoolReturn(OnReloadShaders);
		_mod.Events.RebuildFramebuffers += SetupFramebuffers;
		SetupFramebuffers(((ClientPlatformAbstract)_platform).FrameBuffers);
	}

	private void OnDeferredLightingChanged(bool enabled)
	{
		_enabled = enabled;
		if (enabled && ClientSettings.SSAOQuality == 0)
		{
			ClientSettings.SSAOQuality = 1;
		}
	}

	private void OnSSAOQualityChanged(int quality)
	{
		if (quality == 0 && _enabled)
		{
			ModSettings.DeferredLightingEnabled = false;
			((ClientPlatformAbstract)_platform).RebuildFrameBuffers();
			_mod.CApi.Shader.ReloadShaders();
		}
	}

	private bool OnReloadShaders()
	{
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Expected O, but got Unknown
		bool success = true;
		ShaderProgram shader = _shader;
		if (shader != null)
		{
			((ShaderProgramBase)shader).Dispose();
		}
		_shader = (ShaderProgram)_mod.RegisterShader("deferredlighting", ref success);
		return success;
	}

	private void SetupFramebuffers(List<FrameBufferRef> mainBuffers)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0091: Unknown result type (might be due to invalid IL or missing references)
		//IL_0096: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00db: Expected O, but got Unknown
		if (_frameBuffer != null)
		{
			((ClientPlatformAbstract)_platform).DisposeFrameBuffer(_frameBuffer, true);
			_frameBuffer = null;
		}
		if (ClientSettings.SSAOQuality <= 0 || !_enabled)
		{
			return;
		}
		FrameBufferRef val = mainBuffers[0];
		Box2i bounds = ((NativeWindow)_platform.window).Bounds;
		int num = (int)((float)((Box2i)(ref bounds)).Size.X * ClientSettings.SSAA);
		bounds = ((NativeWindow)_platform.window).Bounds;
		int num2 = (int)((float)((Box2i)(ref bounds)).Size.Y * ClientSettings.SSAA);
		if (num != 0 && num2 != 0)
		{
			FrameBufferRef val2 = new FrameBufferRef
			{
				FboId = GL.GenFramebuffer(),
				Width = num,
				Height = num2,
				ColorTextureIds = ArrayUtil.CreateFilled<int>(2, (fillCallback<int>)((int _) => GL.GenTexture()))
			};
			GL.BindFramebuffer((FramebufferTarget)36160, val2.FboId);
			GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36096, (TextureTarget)3553, val.DepthTextureId, 0);
			val2.SetupColorTexture(0);
			val2.SetupColorTexture(1);
			GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36066, (TextureTarget)3553, val.ColorTextureIds[2], 0);
			GL.FramebufferTexture2D((FramebufferTarget)36160, (FramebufferAttachment)36067, (TextureTarget)3553, val.ColorTextureIds[3], 0);
			DrawBuffersEnum[] array = new DrawBuffersEnum[4];
			RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
			GL.DrawBuffers(4, (DrawBuffersEnum[])(object)array);
			Framebuffers.CheckStatus();
			_frameBuffer = val2;
			_screenQuad = _platform.GetScreenQuad();
		}
	}

	public void OnBeginRender()
	{
		if (_frameBuffer != null)
		{
			((ClientPlatformAbstract)_platform).LoadFrameBuffer(_frameBuffer);
			GL.Clear((ClearBufferMask)16640);
		}
	}

	public void OnEndRender()
	{
		if (_frameBuffer != null)
		{
			((ClientPlatformAbstract)_platform).LoadFrameBuffer((EnumFrameBuffer)0);
			GL.ClearBuffer((ClearBuffer)6144, 0, new float[4] { 0f, 0f, 0f, 1f });
			GL.ClearBuffer((ClearBuffer)6144, 1, new float[4] { 0f, 0f, 0f, 1f });
			IRenderAPI render = _mod.CApi.Render;
			DefaultShaderUniforms shaderUniforms = render.ShaderUniforms;
			Uniforms uniforms = _mod.Uniforms;
			FrameBufferRef frameBuffer = _frameBuffer;
			FrameBufferRef val = ((ClientPlatformAbstract)_platform).FrameBuffers[0];
			((ClientPlatformAbstract)_platform).GlDisableDepthTest();
			((ClientPlatformAbstract)_platform).GlToggleBlend(false, (EnumBlendMode)0);
			GL.DrawBuffers(2, (DrawBuffersEnum[])(object)new DrawBuffersEnum[2]
			{
				(DrawBuffersEnum)36064,
				(DrawBuffersEnum)36065
			});
			ShaderProgram shader = _shader;
			((ShaderProgramBase)shader).Use();
			((ShaderProgramBase)shader).BindTexture2D("gDepth", val.DepthTextureId);
			((ShaderProgramBase)shader).BindTexture2D("gNormal", val.ColorTextureIds[2]);
			((ShaderProgramBase)shader).BindTexture2D("inColor", frameBuffer.ColorTextureIds[0]);
			((ShaderProgramBase)shader).BindTexture2D("inGlow", frameBuffer.ColorTextureIds[1]);
			((ShaderProgramBase)shader).UniformMatrix("invProjectionMatrix", uniforms.InvProjectionMatrix);
			((ShaderProgramBase)shader).UniformMatrix("invModelViewMatrix", uniforms.InvModelViewMatrix);
			((ShaderProgramBase)shader).Uniform("dayLight", uniforms.DayLight);
			((ShaderProgramBase)shader).Uniform("sunPosition", shaderUniforms.SunPosition3D);
			if (ShaderProgramBase.shadowmapQuality > 0)
			{
				((ShaderProgramBase)shader).Uniform("shadowRangeFar", shaderUniforms.ShadowRangeFar);
				((ShaderProgramBase)shader).Uniform("shadowRangeNear", shaderUniforms.ShadowRangeNear);
				((ShaderProgramBase)shader).UniformMatrix("toShadowMapSpaceMatrixFar", shaderUniforms.ToShadowMapSpaceMatrixFar);
				((ShaderProgramBase)shader).UniformMatrix("toShadowMapSpaceMatrixNear", shaderUniforms.ToShadowMapSpaceMatrixNear);
			}
			((ShaderProgramBase)shader).Uniform("fogDensityIn", render.FogDensity);
			((ShaderProgramBase)shader).Uniform("fogMinIn", render.FogMin);
			((ShaderProgramBase)shader).Uniform("rgbaFog", render.FogColor);
			((ShaderProgramBase)shader).Uniform("flatFogDensity", shaderUniforms.FlagFogDensity);
			((ShaderProgramBase)shader).Uniform("flatFogStart", shaderUniforms.FlatFogStartYPos - shaderUniforms.PlayerPos.Y);
			((ShaderProgramBase)shader).Uniform("viewDistance", ClientSettings.ViewDistance);
			((ShaderProgramBase)shader).Uniform("viewDistanceLod0", (float)ClientSettings.ViewDistance * ClientSettings.LodBias);
			_platform.RenderFullscreenTriangle(_screenQuad);
			((ShaderProgramBase)shader).Stop();
			((ClientPlatformAbstract)_platform).CheckGlError("Error while calculating deferred lighting");
			DrawBuffersEnum[] array = new DrawBuffersEnum[4];
			RuntimeHelpers.InitializeArray(array, (RuntimeFieldHandle)/*OpCode not supported: LdMemberToken*/);
			GL.DrawBuffers(4, (DrawBuffersEnum[])(object)array);
			((ClientPlatformAbstract)_platform).GlEnableDepthTest();
		}
	}

	public void Dispose()
	{
		ShaderProgram shader = _shader;
		if (shader != null)
		{
			((ShaderProgramBase)shader).Dispose();
		}
		_shader = null;
		if (_frameBuffer != null)
		{
			((ClientPlatformAbstract)_platform).DisposeFrameBuffer(_frameBuffer, true);
			_frameBuffer = null;
		}
	}
}
