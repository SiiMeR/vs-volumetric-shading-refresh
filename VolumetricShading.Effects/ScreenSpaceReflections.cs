using System;
using System.Collections.Generic;
using System.Reflection;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using volumetricshadingupdated.VolumetricShading.Patch;

namespace volumetricshadingupdated.VolumetricShading.Effects
{
	// Token: 0x02000045 RID: 69
	public class ScreenSpaceReflections : IRenderer, IDisposable
	{
		// Token: 0x060001C6 RID: 454 RVA: 0x00007860 File Offset: 0x00005A60
		public ScreenSpaceReflections(VolumetricShadingMod mod)
		{
			this._mod = mod;
			this._game = mod.CApi.GetClient();
			this._platform = this._game.GetClientPlatformWindows();
			this.RegisterInjectorProperties();
			mod.CApi.Event.ReloadShader += this.ReloadShaders;
			mod.Events.PreFinalRender += this.OnSetFinalUniforms;
			mod.ShaderPatcher.OnReload += this.RegeneratePatches;
			this._enabled = ModSettings.ScreenSpaceReflectionsEnabled;
			this._rainEnabled = ModSettings.SSRRainReflectionsEnabled;
			this._refractionsEnabled = ModSettings.SSRRefractionsEnabled;
			this._causticsEnabled = ModSettings.SSRCausticsEnabled;
			mod.CApi.Settings.AddWatcher<bool>("volumetricshading_screenSpaceReflections", new OnSettingsChanged<bool>(this.OnEnabledChanged));
			mod.CApi.Settings.AddWatcher<bool>("volumetricshading_SSRRainReflections", new OnSettingsChanged<bool>(this.OnRainReflectionsChanged));
			mod.CApi.Settings.AddWatcher<bool>("volumetricshading_SSRRefractions", new OnSettingsChanged<bool>(this.OnRefractionsChanged));
			mod.CApi.Settings.AddWatcher<bool>("volumetricshading_SSRCaustics", new OnSettingsChanged<bool>(this.OnCausticsChanged));
			mod.CApi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "ssrWorldSpace");
			mod.CApi.Event.RegisterRenderer(this, EnumRenderStage.AfterOIT, "ssrOut");
			this._textureIdsField = typeof(ChunkRenderer).GetField("textureIds", BindingFlags.Instance | BindingFlags.Public);
			mod.Events.RebuildFramebuffers += this.SetupFramebuffers;
			this.SetupFramebuffers(this._platform.FrameBuffers);
		}

		// Token: 0x060001C7 RID: 455 RVA: 0x00007A28 File Offset: 0x00005C28
		private void RegeneratePatches()
		{
			string code = this._mod.CApi.Assets.Get(new AssetLocation("game", "shaders/chunkliquid.fsh")).ToText();
			bool flag = true;
			FunctionExtractor extractor = new FunctionExtractor();
			if (!(flag & extractor.Extract(code, "droplethash3") & extractor.Extract(code, "dropletnoise")))
			{
				throw new InvalidOperationException("Could not extract dropletnoise/droplethash3");
			}
			string content = extractor.ExtractedContent;
			content = content.Replace("waterWaveCounter", "waveCounter");
			content = new TokenPatch("float dropletnoise(in vec2 x)")
			{
				ReplacementString = "float dropletnoise(in vec2 x, in float waveCounter)"
			}.Patch("dropletnoise", content);
			content = new TokenPatch("a = smoothstep(0.99, 0.999, a);")
			{
				ReplacementString = "a = smoothstep(0.97, 0.999, a);"
			}.Patch("dropletnoise", content);
			this._mod.ShaderInjector["dropletnoise"] = content;
		}

		// Token: 0x060001C8 RID: 456 RVA: 0x00007B00 File Offset: 0x00005D00
		private void RegisterInjectorProperties()
		{
			ShaderInjector shaderInjector = this._mod.ShaderInjector;
			shaderInjector.RegisterBoolProperty("VSMOD_SSR", () => ModSettings.ScreenSpaceReflectionsEnabled);
			shaderInjector.RegisterFloatProperty("VSMOD_SSR_WATER_TRANSPARENCY", () => (float)(100 - ModSettings.SSRWaterTransparency) * 0.01f);
			shaderInjector.RegisterFloatProperty("VSMOD_SSR_SPLASH_TRANSPARENCY", () => (float)(100 - ModSettings.SSRSplashTransparency) * 0.01f);
			shaderInjector.RegisterFloatProperty("VSMOD_SSR_REFLECTION_DIMMING", () => (float)ModSettings.SSRReflectionDimming * 0.01f);
			shaderInjector.RegisterFloatProperty("VSMOD_SSR_TINT_INFLUENCE", () => (float)ModSettings.SSRTintInfluence * 0.01f);
			shaderInjector.RegisterFloatProperty("VSMOD_SSR_SKY_MIXIN", () => (float)ModSettings.SSRSkyMixin * 0.01f);
			shaderInjector.RegisterBoolProperty("VSMOD_REFRACT", () => ModSettings.SSRRefractionsEnabled);
			shaderInjector.RegisterBoolProperty("VSMOD_CAUSTICS", () => ModSettings.SSRCausticsEnabled);
		}

		// Token: 0x060001C9 RID: 457 RVA: 0x00003747 File Offset: 0x00001947
		private void OnEnabledChanged(bool enabled)
		{
			this._enabled = enabled;
		}

		// Token: 0x060001CA RID: 458 RVA: 0x00003750 File Offset: 0x00001950
		private void OnRainReflectionsChanged(bool enabled)
		{
			this._rainEnabled = enabled;
		}

		// Token: 0x060001CB RID: 459 RVA: 0x00003759 File Offset: 0x00001959
		private void OnRefractionsChanged(bool enabled)
		{
			this._refractionsEnabled = enabled;
		}

		// Token: 0x060001CC RID: 460 RVA: 0x00003762 File Offset: 0x00001962
		private void OnCausticsChanged(bool enabled)
		{
			this._causticsEnabled = enabled;
		}

		// Token: 0x060001CD RID: 461 RVA: 0x00007C68 File Offset: 0x00005E68
		private bool ReloadShaders()
		{
			bool success = true;
			for (int i = 0; i < this._shaders.Length; i++)
			{
				IShaderProgram shaderProgram = this._shaders[i];
				if (shaderProgram != null)
				{
					shaderProgram.Dispose();
				}
				this._shaders[i] = null;
			}
			this._shaders[0] = this._mod.RegisterShader("ssrliquid", ref success);
			this._shaders[1] = this._mod.RegisterShader("ssropaque", ref success);
			((ShaderProgram)this._shaders[1]).SetCustomSampler("terrainTexLinear", true);
			this._shaders[2] = this._mod.RegisterShader("ssrtransparent", ref success);
			this._shaders[3] = this._mod.RegisterShader("ssrtopsoil", ref success);
			this._shaders[4] = this._mod.RegisterShader("ssrout", ref success);
			this._shaders[5] = this._mod.RegisterShader("ssrcausticsout", ref success);
			return success;
		}

		// Token: 0x060001CE RID: 462 RVA: 0x00007D5C File Offset: 0x00005F5C
		public void SetupFramebuffers(List<FrameBufferRef> mainBuffers)
		{
			this._mod.Mod.Logger.Event("Recreating framebuffers");
			for (int i = 0; i < this._framebuffers.Length; i++)
			{
				if (this._framebuffers[i] != null)
				{
					this._platform.DisposeFrameBuffer(this._framebuffers[i], true);
					this._framebuffers[i] = null;
				}
			}
			this._fbWidth = (int)((float)this._platform.window.Bounds.Size.X * ClientSettings.SSAA);
			this._fbHeight = (int)((float)this._platform.window.Bounds.Size.Y * ClientSettings.SSAA);
			if (this._fbWidth == 0 || this._fbHeight == 0)
			{
				return;
			}
			FrameBufferRef framebuffer = new FrameBufferRef
			{
				FboId = GL.GenFramebuffer(),
				Width = this._fbWidth,
				Height = this._fbHeight,
				DepthTextureId = GL.GenTexture()
			};
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.FboId);
			framebuffer.SetupDepthTexture();
			framebuffer.ColorTextureIds = ArrayUtil.CreateFilled<int>(this._refractionsEnabled ? 4 : 3, (int _) => GL.GenTexture());
			framebuffer.SetupVertexTexture(0);
			framebuffer.SetupVertexTexture(1);
			framebuffer.SetupColorTexture(2);
			if (this._refractionsEnabled)
			{
				framebuffer.SetupVertexTexture(3);
			}
			if (this._refractionsEnabled)
			{
				GL.DrawBuffers(4, new DrawBuffersEnum[]
				{
					DrawBuffersEnum.ColorAttachment0,
					DrawBuffersEnum.ColorAttachment1,
					DrawBuffersEnum.ColorAttachment2,
					DrawBuffersEnum.ColorAttachment3
				});
			}
			else
			{
				GL.DrawBuffers(3, new DrawBuffersEnum[]
				{
					DrawBuffersEnum.ColorAttachment0,
					DrawBuffersEnum.ColorAttachment1,
					DrawBuffersEnum.ColorAttachment2
				});
			}
			Framebuffers.CheckStatus();
			this._framebuffers[0] = framebuffer;
			framebuffer = new FrameBufferRef
			{
				FboId = GL.GenFramebuffer(),
				Width = this._fbWidth,
				Height = this._fbHeight
			};
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.FboId);
			framebuffer.ColorTextureIds = new int[] { GL.GenTexture() };
			framebuffer.SetupColorTexture(0);
			GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
			Framebuffers.CheckStatus();
			this._framebuffers[1] = framebuffer;
			if (this._causticsEnabled)
			{
				framebuffer = new FrameBufferRef
				{
					FboId = GL.GenFramebuffer(),
					Width = this._fbWidth,
					Height = this._fbHeight
				};
				GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.FboId);
				framebuffer.ColorTextureIds = new int[] { GL.GenTexture() };
				framebuffer.SetupSingleColorTexture(0);
				GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
				Framebuffers.CheckStatus();
				this._framebuffers[2] = framebuffer;
			}
			this._screenQuad = this._platform.GetScreenQuad();
		}

		// Token: 0x060001CF RID: 463 RVA: 0x0000376B File Offset: 0x0000196B
		public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
		{
			if (!this._enabled)
			{
				return;
			}
			if (this._chunkRenderer == null)
			{
				this._chunkRenderer = this._game.GetChunkRenderer();
			}
			if (stage == EnumRenderStage.Opaque)
			{
				this.OnPreRender(deltaTime);
				this.OnRenderSsrChunks();
				return;
			}
			if (stage == EnumRenderStage.AfterOIT)
			{
				this.OnRenderSsrOut();
			}
		}

		// Token: 0x060001D0 RID: 464 RVA: 0x00008000 File Offset: 0x00006200
		private void OnPreRender(float dt)
		{
			this._rainAccumulator += dt;
			if (this._rainAccumulator > 5f)
			{
				this._rainAccumulator = 0f;
				ClimateCondition climate = this._game.BlockAccessor.GetClimateAt(this._game.EntityPlayer.Pos.AsBlockPos, EnumGetClimateMode.NowValues, 0.0);
				float rainMul = GameMath.Clamp((climate.Temperature + 1f) / 4f, 0f, 1f);
				this._targetRain = climate.Rainfall * rainMul;
			}
			if (this._targetRain > this._currentRain)
			{
				this._currentRain = Math.Min(this._currentRain + dt * 0.15f, this._targetRain);
				return;
			}
			if (this._targetRain < this._currentRain)
			{
				this._currentRain = Math.Max(this._currentRain - dt * 0.01f, this._targetRain);
			}
		}

		// Token: 0x060001D1 RID: 465
		private void OnRenderSsrOut()
		{
			FrameBufferRef ssrOutFB = this._framebuffers[1];
			FrameBufferRef ssrCausticsFB = this._framebuffers[2];
			FrameBufferRef ssrFB = this._framebuffers[0];
			IShaderProgram ssrOutShader = this._shaders[4];
			IShaderProgram ssrCausticsShader = this._shaders[5];
			if (ssrOutFB == null)
			{
				return;
			}
			if (ssrOutShader == null)
			{
				return;
			}
			GL.Disable(EnableCap.Blend);
			this._platform.LoadFrameBuffer(ssrOutFB);
			GL.ClearBuffer(ClearBuffer.Color, 0, new float[] { 0f, 0f, 0f, 1f });
			Uniforms myUniforms = this._mod.Uniforms;
			DefaultShaderUniforms uniforms = this._mod.CApi.Render.ShaderUniforms;
			IAmbientManager ambient = this._mod.CApi.Ambient;
			IShaderProgram shader = ssrOutShader;
			shader.Use();
			shader.BindTexture2D("primaryScene", this._platform.FrameBuffers[0].ColorTextureIds[0], 0);
			shader.BindTexture2D("gPosition", ssrFB.ColorTextureIds[0], 1);
			shader.BindTexture2D("gNormal", ssrFB.ColorTextureIds[1], 2);
			shader.BindTexture2D("gDepth", this._platform.FrameBuffers[0].DepthTextureId, 3);
			shader.BindTexture2D("gTint", ssrFB.ColorTextureIds[2], 4);
			shader.UniformMatrix("projectionMatrix", this._mod.CApi.Render.CurrentProjectionMatrix);
			shader.UniformMatrix("invProjectionMatrix", myUniforms.InvProjectionMatrix);
			shader.UniformMatrix("invModelViewMatrix", myUniforms.InvModelViewMatrix);
			shader.Uniform("zFar", uniforms.ZNear);
			shader.Uniform("sunPosition", this._mod.CApi.World.Calendar.SunPositionNormalized);
			shader.Uniform("dayLight", myUniforms.DayLight);
			shader.Uniform("horizonFog", ambient.BlendedCloudDensity);
			shader.Uniform("fogDensityIn", ambient.BlendedFogDensity);
			shader.Uniform("fogMinIn", ambient.BlendedFogMin);
			shader.Uniform("rgbaFog", ambient.BlendedFogColor);
			this._platform.RenderFullscreenTriangle(this._screenQuad);
			shader.Stop();
			this._platform.CheckGlError("Error while calculating SSR");
			if (this._causticsEnabled && ssrCausticsFB != null && ssrCausticsShader != null)
			{
				this._platform.LoadFrameBuffer(ssrCausticsFB);
				GL.ClearBuffer(ClearBuffer.Color, 0, new float[] { 0.5f });
				shader = ssrCausticsShader;
				shader.Use();
				shader.BindTexture2D("gDepth", this._platform.FrameBuffers[0].DepthTextureId, 0);
				shader.BindTexture2D("gNormal", ssrFB.ColorTextureIds[1], 1);
				shader.UniformMatrix("invProjectionMatrix", myUniforms.InvProjectionMatrix);
				shader.UniformMatrix("invModelViewMatrix", myUniforms.InvModelViewMatrix);
				shader.Uniform("dayLight", myUniforms.DayLight);
				shader.Uniform("playerPos", uniforms.PlayerPos);
				shader.Uniform("sunPosition", uniforms.SunPosition3D);
				shader.Uniform("waterFlowCounter", uniforms.WaterFlowCounter);
				if (ShaderProgramBase.shadowmapQuality > 0)
				{
					FrameBufferRef fbShadowFar = this._platform.FrameBuffers[11];
					shader.BindTexture2D("shadowMapFar", fbShadowFar.DepthTextureId, 2);
					shader.BindTexture2D("shadowMapNear", this._platform.FrameBuffers[12].DepthTextureId, 3);
					shader.Uniform("shadowMapWidthInv", 1f / (float)fbShadowFar.Width);
					shader.Uniform("shadowMapHeightInv", 1f / (float)fbShadowFar.Height);
					shader.Uniform("shadowRangeFar", uniforms.ShadowRangeFar);
					shader.Uniform("shadowRangeNear", uniforms.ShadowRangeNear);
					shader.UniformMatrix("toShadowMapSpaceMatrixFar", uniforms.ToShadowMapSpaceMatrixFar);
					shader.UniformMatrix("toShadowMapSpaceMatrixNear", uniforms.ToShadowMapSpaceMatrixNear);
				}
				shader.Uniform("fogDensityIn", ambient.BlendedFogDensity);
				shader.Uniform("fogMinIn", ambient.BlendedFogMin);
				shader.Uniform("rgbaFog", ambient.BlendedFogColor);
				this._platform.RenderFullscreenTriangle(this._screenQuad);
				shader.Stop();
				this._platform.CheckGlError("Error while calculating caustics");
			}
			this._platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
			GL.Enable(EnableCap.Blend);
		}

		// Token: 0x060001D2 RID: 466 RVA: 0x00008584 File Offset: 0x00006784
		private void OnRenderSsrChunks()
		{
			FrameBufferRef ssrFB = this._framebuffers[0];
			if (ssrFB == null)
			{
				return;
			}
			if (this._shaders[0] == null)
			{
				return;
			}
			int[] textureIds = this._textureIdsField.GetValue(this._chunkRenderer) as int[];
			if (textureIds == null)
			{
				return;
			}
			float playerUnderwater = ((this._game.playerProperties.EyesInWaterDepth >= 0.1f) ? 0f : 1f);
			FrameBufferRef primaryBuffer = this._platform.FrameBuffers[0];
			GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, primaryBuffer.FboId);
			GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, ssrFB.FboId);
			GL.Clear(ClearBufferMask.DepthBufferBit);
			GL.BlitFramebuffer(0, 0, primaryBuffer.Width, primaryBuffer.Height, 0, 0, this._fbWidth, this._fbHeight, ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);
			this._platform.LoadFrameBuffer(ssrFB);
			GL.ClearBuffer(ClearBuffer.Color, 0, new float[] { 0f, 0f, 0f, 1f });
			GL.ClearBuffer(ClearBuffer.Color, 1, new float[] { 0f, 0f, 0f, playerUnderwater });
			GL.ClearBuffer(ClearBuffer.Color, 2, new float[] { 0f, 0f, 0f, 1f });
			if (this._refractionsEnabled)
			{
				GL.ClearBuffer(ClearBuffer.Color, 3, new float[] { 0f, 0f, 0f, 1f });
			}
			this._platform.GlEnableCullFace();
			this._platform.GlDepthMask(true);
			this._platform.GlEnableDepthTest();
			this._platform.GlToggleBlend(false, EnumBlendMode.Standard);
			ClimateCondition climateAt = this._game.BlockAccessor.GetClimateAt(this._game.EntityPlayer.Pos.AsBlockPos, EnumGetClimateMode.NowValues, 0.0);
			float num = GameMath.Clamp((float)(((double)climateAt.Temperature + 1.0) / 4.0), 0f, 1f);
			float curRainFall = climateAt.Rainfall * num;
			Vec3d cameraPos = this._game.EntityPlayer.CameraPos;
			this._game.GlPushMatrix();
			this._game.GlLoadMatrix(this._mod.CApi.Render.CameraMatrixOrigin);
			IShaderProgram shader = this._shaders[1];
			shader.Use();
			shader.UniformMatrix("projectionMatrix", this._mod.CApi.Render.CurrentProjectionMatrix);
			shader.UniformMatrix("modelViewMatrix", this._mod.CApi.Render.CurrentModelviewMatrix);
			shader.Uniform("playerUnderwater", playerUnderwater);
			MeshDataPoolManager[] pools = this._chunkRenderer.poolsByRenderPass[0];
			for (int i = 0; i < textureIds.Length; i++)
			{
				shader.BindTexture2D("terrainTex", textureIds[i], 0);
				shader.BindTexture2D("terrainTexLinear", textureIds[i], 1);
				pools[i].Render(cameraPos, "origin", EnumFrustumCullMode.CullNormal);
			}
			shader.Stop();
			GL.BindSampler(0, 0);
			GL.BindSampler(1, 0);
			if (this._rainEnabled)
			{
				shader = this._shaders[3];
				shader.Use();
				shader.UniformMatrix("projectionMatrix", this._mod.CApi.Render.CurrentProjectionMatrix);
				shader.UniformMatrix("modelViewMatrix", this._mod.CApi.Render.CurrentModelviewMatrix);
				shader.Uniform("rainStrength", this._currentRain);
				shader.Uniform("playerUnderwater", playerUnderwater);
				pools = this._chunkRenderer.poolsByRenderPass[5];
				for (int j = 0; j < textureIds.Length; j++)
				{
					shader.BindTexture2D("terrainTex", textureIds[j], 0);
					pools[j].Render(cameraPos, "origin", EnumFrustumCullMode.CullNormal);
				}
				shader.Stop();
			}
			this._platform.GlDisableCullFace();
			shader = this._shaders[0];
			shader.Use();
			shader.UniformMatrix("projectionMatrix", this._mod.CApi.Render.CurrentProjectionMatrix);
			shader.UniformMatrix("modelViewMatrix", this._mod.CApi.Render.CurrentModelviewMatrix);
			shader.Uniform("dropletIntensity", curRainFall);
			shader.Uniform("waterFlowCounter", this._platform.ShaderUniforms.WaterFlowCounter);
			shader.Uniform("windSpeed", this._platform.ShaderUniforms.WindSpeed);
			shader.Uniform("playerUnderwater", playerUnderwater);
			shader.Uniform("cameraWorldPosition", this._mod.Uniforms.CameraWorldPosition);
			pools = this._chunkRenderer.poolsByRenderPass[4];
			for (int k = 0; k < textureIds.Length; k++)
			{
				shader.BindTexture2D("terrainTex", textureIds[k], 0);
				pools[k].Render(cameraPos, "origin", EnumFrustumCullMode.CullNormal);
			}
			shader.Stop();
			this._platform.GlEnableCullFace();
			shader = this._shaders[2];
			shader.Use();
			shader.UniformMatrix("projectionMatrix", this._mod.CApi.Render.CurrentProjectionMatrix);
			shader.UniformMatrix("modelViewMatrix", this._mod.CApi.Render.CurrentModelviewMatrix);
			shader.Uniform("playerUnderwater", playerUnderwater);
			pools = this._chunkRenderer.poolsByRenderPass[3];
			for (int l = 0; l < textureIds.Length; l++)
			{
				shader.BindTexture2D("terrainTex", textureIds[l], 0);
				pools[l].Render(cameraPos, "origin", EnumFrustumCullMode.CullNormal);
			}
			shader.Stop();
			this._game.GlPopMatrix();
			this._platform.UnloadFrameBuffer(ssrFB);
			this._platform.GlDepthMask(false);
			this._platform.GlToggleBlend(true, EnumBlendMode.Standard);
			this._platform.CheckGlError("Error while rendering solid liquids");
		}

		// Token: 0x060001D3 RID: 467 RVA: 0x00008B3C File Offset: 0x00006D3C
		public void OnSetFinalUniforms(ShaderProgramFinal final)
		{
			FrameBufferRef ssrOutFB = this._framebuffers[1];
			FrameBufferRef ssrFB = this._framebuffers[0];
			FrameBufferRef causticsFB = this._framebuffers[2];
			if (!this._enabled)
			{
				return;
			}
			if (ssrOutFB == null)
			{
				return;
			}
			final.BindTexture2D("ssrScene", ssrOutFB.ColorTextureIds[0]);
			if ((this._refractionsEnabled || this._causticsEnabled) && ssrFB != null)
			{
				final.UniformMatrix("projectionMatrix", this._mod.CApi.Render.CurrentProjectionMatrix);
				final.BindTexture2D("gpositionScene", ssrFB.ColorTextureIds[0]);
				final.BindTexture2D("gdepthScene", this._platform.FrameBuffers[0].DepthTextureId);
			}
			if (this._refractionsEnabled && ssrFB != null)
			{
				final.BindTexture2D("refractionScene", ssrFB.ColorTextureIds[3]);
			}
			if (this._causticsEnabled && causticsFB != null)
			{
				final.BindTexture2D("causticsScene", causticsFB.ColorTextureIds[0]);
			}
		}

		// Token: 0x060001D4 RID: 468 RVA: 0x00008C28 File Offset: 0x00006E28
		public void Dispose()
		{
			ClientPlatformWindows windowsPlatform = this._mod.CApi.GetClientPlatformWindows();
			for (int i = 0; i < this._framebuffers.Length; i++)
			{
				if (this._framebuffers[i] != null)
				{
					windowsPlatform.DisposeFrameBuffer(this._framebuffers[i], true);
					this._framebuffers[i] = null;
				}
			}
			for (int j = 0; j < this._shaders.Length; j++)
			{
				IShaderProgram shaderProgram = this._shaders[j];
				if (shaderProgram != null)
				{
					shaderProgram.Dispose();
				}
				this._shaders[j] = null;
			}
			this._chunkRenderer = null;
			this._screenQuad = null;
		}

		// Token: 0x1700005C RID: 92
		// (get) Token: 0x060001D5 RID: 469 RVA: 0x0000358C File Offset: 0x0000178C
		public double RenderOrder
		{
			get
			{
				return 1.0;
			}
		}

		// Token: 0x1700005D RID: 93
		// (get) Token: 0x060001D6 RID: 470 RVA: 0x00002A24 File Offset: 0x00000C24
		public int RenderRange
		{
			get
			{
				return int.MaxValue;
			}
		}

		// Token: 0x040000CF RID: 207
		private readonly VolumetricShadingMod _mod;

		// Token: 0x040000D0 RID: 208
		private bool _enabled;

		// Token: 0x040000D1 RID: 209
		private bool _rainEnabled;

		// Token: 0x040000D2 RID: 210
		private bool _refractionsEnabled;

		// Token: 0x040000D3 RID: 211
		private bool _causticsEnabled;

		// Token: 0x040000D4 RID: 212
		private readonly FrameBufferRef[] _framebuffers = new FrameBufferRef[3];

		// Token: 0x040000D5 RID: 213
		private readonly IShaderProgram[] _shaders = new IShaderProgram[6];

		// Token: 0x040000D6 RID: 214
		private readonly ClientMain _game;

		// Token: 0x040000D7 RID: 215
		private readonly ClientPlatformWindows _platform;

		// Token: 0x040000D8 RID: 216
		private ChunkRenderer _chunkRenderer;

		// Token: 0x040000D9 RID: 217
		private MeshRef _screenQuad;

		// Token: 0x040000DA RID: 218
		private readonly FieldInfo _textureIdsField;

		// Token: 0x040000DB RID: 219
		private int _fbWidth;

		// Token: 0x040000DC RID: 220
		private int _fbHeight;

		// Token: 0x040000DD RID: 221
		private float _currentRain;

		// Token: 0x040000DE RID: 222
		private float _targetRain;

		// Token: 0x040000DF RID: 223
		private float _rainAccumulator;
	}
}
