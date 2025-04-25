using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using volumetricshadingupdated.VolumetricShading.Patch;

namespace volumetricshadingupdated.VolumetricShading.Effects
{
    // Token: 0x0200003D RID: 61
    public class DeferredLighting
    {
        // Token: 0x060001AD RID: 429 RVA: 0x000071F8 File Offset: 0x000053F8
        public DeferredLighting(VolumetricShadingMod mod)
        {
            this._mod = mod;
            this._platform = this._mod.CApi.GetClientPlatformWindows();
            this._mod.CApi.Settings.AddWatcher<bool>("volumetricshading_deferredLighting",
                new OnSettingsChanged<bool>(this.OnDeferredLightingChanged));
            this._mod.CApi.Settings.AddWatcher<int>("ssaoQuality",
                new OnSettingsChanged<int>(this.OnSSAOQualityChanged));
            this._enabled = ModSettings.DeferredLightingEnabled;
            this._mod.CApi.Event.RegisterRenderer(new DeferredLightingPreparer(this), EnumRenderStage.Opaque,
                "vsmod-deferred-lighting-prepare");
            this._mod.CApi.Event.RegisterRenderer(new DeferredLightingRenderer(this), EnumRenderStage.Opaque,
                "vsmod-deferred-lighting");
            this._mod.ShaderInjector.RegisterBoolProperty("VSMOD_DEFERREDLIGHTING", () => this._enabled);
            this._mod.CApi.Event.ReloadShader += this.OnReloadShaders;
            this._mod.Events.RebuildFramebuffers += this.SetupFramebuffers;
            this.SetupFramebuffers(this._platform.FrameBuffers);
        }

        // Token: 0x060001AE RID: 430 RVA: 0x000035CB File Offset: 0x000017CB
        private void OnDeferredLightingChanged(bool enabled)
        {
            this._enabled = enabled;
            if (enabled && ClientSettings.SSAOQuality == 0)
            {
                ClientSettings.SSAOQuality = 1;
            }
        }

        // Token: 0x060001AF RID: 431 RVA: 0x000035E4 File Offset: 0x000017E4
        private void OnSSAOQualityChanged(int quality)
        {
            if (quality == 0 && this._enabled)
            {
                ModSettings.DeferredLightingEnabled = false;
                this._platform.RebuildFrameBuffers();
                this._mod.CApi.Shader.ReloadShaders();
            }
        }

        // Token: 0x060001B0 RID: 432 RVA: 0x00007330 File Offset: 0x00005530
        private bool OnReloadShaders()
        {
            bool success = true;
            ShaderProgram shader = this._shader;
            if (shader != null)
            {
                shader.Dispose();
            }

            this._shader = (ShaderProgram)this._mod.RegisterShader("deferredlighting", ref success);
            return success;
        }

        // Token: 0x060001B1 RID: 433 RVA: 0x00007370 File Offset: 0x00005570
        private void SetupFramebuffers(List<FrameBufferRef> mainBuffers)
        {
            if (this._frameBuffer != null)
            {
                this._platform.DisposeFrameBuffer(this._frameBuffer, true);
                this._frameBuffer = null;
            }

            if (ClientSettings.SSAOQuality <= 0 || !this._enabled)
            {
                return;
            }

            FrameBufferRef fbPrimary = mainBuffers[0];
            int fbWidth = (int)((float)this._platform.window.Bounds.Size.X * ClientSettings.SSAA);
            int fbHeight = (int)((float)this._platform.window.Bounds.Size.Y * ClientSettings.SSAA);
            if (fbWidth == 0 || fbHeight == 0)
            {
                return;
            }

            FrameBufferRef frameBufferRef = new FrameBufferRef();
            frameBufferRef.FboId = GL.GenFramebuffer();
            frameBufferRef.Width = fbWidth;
            frameBufferRef.Height = fbHeight;
            frameBufferRef.ColorTextureIds = ArrayUtil.CreateFilled<int>(2, (int _) => GL.GenTexture());
            FrameBufferRef fb = frameBufferRef;
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, fb.FboId);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
                TextureTarget.Texture2D, fbPrimary.DepthTextureId, 0);
            fb.SetupColorTexture(0);
            fb.SetupColorTexture(1);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2,
                TextureTarget.Texture2D, fbPrimary.ColorTextureIds[2], 0);
            GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment3,
                TextureTarget.Texture2D, fbPrimary.ColorTextureIds[3], 0);
            GL.DrawBuffers(4, new DrawBuffersEnum[]
            {
                DrawBuffersEnum.ColorAttachment0,
                DrawBuffersEnum.ColorAttachment1,
                DrawBuffersEnum.ColorAttachment2,
                DrawBuffersEnum.ColorAttachment3
            });
            Framebuffers.CheckStatus();
            this._frameBuffer = fb;
            this._screenQuad = this._platform.GetScreenQuad();
        }

        // Token: 0x060001B2 RID: 434 RVA: 0x00003618 File Offset: 0x00001818
        public void OnBeginRender()
        {
            if (this._frameBuffer == null)
            {
                return;
            }

            this._platform.LoadFrameBuffer(this._frameBuffer);
            GL.Clear(ClearBufferMask.DepthBufferBit | ClearBufferMask.ColorBufferBit);
        }

        // Token: 0x060001B3 RID: 435 RVA: 0x00007500 File Offset: 0x00005700
        public void OnEndRender()
        {
            if (this._frameBuffer == null)
            {
                return;
            }

            this._platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
            GL.ClearBuffer(ClearBuffer.Color, 0, new float[] { 0f, 0f, 0f, 1f });
            GL.ClearBuffer(ClearBuffer.Color, 1, new float[] { 0f, 0f, 0f, 1f });
            IRenderAPI render = this._mod.CApi.Render;
            DefaultShaderUniforms uniforms = render.ShaderUniforms;
            Uniforms myUniforms = this._mod.Uniforms;
            FrameBufferRef fb = this._frameBuffer;
            FrameBufferRef fbPrimary = this._platform.FrameBuffers[0];
            this._platform.GlDisableDepthTest();
            this._platform.GlToggleBlend(false, EnumBlendMode.Standard);
            GL.DrawBuffers(2, new DrawBuffersEnum[]
            {
                DrawBuffersEnum.ColorAttachment0,
                DrawBuffersEnum.ColorAttachment1
            });
            ShaderProgram s = this._shader;
            s.Use();
            s.BindTexture2D("gDepth", fbPrimary.DepthTextureId);
            s.BindTexture2D("gNormal", fbPrimary.ColorTextureIds[2]);
            s.BindTexture2D("inColor", fb.ColorTextureIds[0]);
            s.BindTexture2D("inGlow", fb.ColorTextureIds[1]);
            s.UniformMatrix("invProjectionMatrix", myUniforms.InvProjectionMatrix);
            s.UniformMatrix("invModelViewMatrix", myUniforms.InvModelViewMatrix);
            s.Uniform("dayLight", myUniforms.DayLight);
            s.Uniform("sunPosition", uniforms.SunPosition3D);
            if (ShaderProgramBase.shadowmapQuality > 0)
            {
                s.Uniform("shadowRangeFar", uniforms.ShadowRangeFar);
                s.Uniform("shadowRangeNear", uniforms.ShadowRangeNear);
                s.UniformMatrix("toShadowMapSpaceMatrixFar", uniforms.ToShadowMapSpaceMatrixFar);
                s.UniformMatrix("toShadowMapSpaceMatrixNear", uniforms.ToShadowMapSpaceMatrixNear);
            }

            s.Uniform("fogDensityIn", render.FogDensity);
            s.Uniform("fogMinIn", render.FogMin);
            s.Uniform("rgbaFog", render.FogColor);
            s.Uniform("flatFogDensity", uniforms.FlagFogDensity);
            s.Uniform("flatFogStart", uniforms.FlatFogStartYPos - uniforms.PlayerPos.Y);
            s.Uniform("viewDistance", ClientSettings.ViewDistance);
            s.Uniform("viewDistanceLod0", (float)ClientSettings.ViewDistance * ClientSettings.LodBias);
            this._platform.RenderFullscreenTriangle(this._screenQuad);
            s.Stop();
            this._platform.CheckGlError("Error while calculating deferred lighting");
            GL.DrawBuffers(4, new DrawBuffersEnum[]
            {
                DrawBuffersEnum.ColorAttachment0,
                DrawBuffersEnum.ColorAttachment1,
                DrawBuffersEnum.ColorAttachment2,
                DrawBuffersEnum.ColorAttachment3
            });
            this._platform.GlEnableDepthTest();
        }

        // Token: 0x060001B4 RID: 436 RVA: 0x0000363E File Offset: 0x0000183E
        public void Dispose()
        {
            ShaderProgram shader = this._shader;
            if (shader != null)
            {
                shader.Dispose();
            }

            this._shader = null;
            if (this._frameBuffer != null)
            {
                this._platform.DisposeFrameBuffer(this._frameBuffer, true);
                this._frameBuffer = null;
            }
        }

        // Token: 0x040000B4 RID: 180
        private readonly VolumetricShadingMod _mod;

        // Token: 0x040000B5 RID: 181
        private readonly ClientPlatformWindows _platform;

        // Token: 0x040000B6 RID: 182
        private ShaderProgram _shader;

        // Token: 0x040000B7 RID: 183
        private FrameBufferRef _frameBuffer;

        // Token: 0x040000B8 RID: 184
        private MeshRef _screenQuad;

        // Token: 0x040000B9 RID: 185
        private bool _enabled;
    }
}