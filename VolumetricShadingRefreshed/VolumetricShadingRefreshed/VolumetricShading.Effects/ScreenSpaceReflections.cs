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

namespace volumetricshadingupdated.VolumetricShading.Effects;

// Token: 0x02000045 RID: 69
public class ScreenSpaceReflections : IRenderer, IDisposable
{
    // Token: 0x040000D6 RID: 214
    private readonly ClientMain _game;

    // Token: 0x040000CF RID: 207
    private readonly VolumetricShadingMod _mod;

    // Token: 0x040000D7 RID: 215
    private readonly ClientPlatformWindows _platform;

    // Token: 0x040000D5 RID: 213
    private readonly IShaderProgram[] _shaders = new IShaderProgram[6];

    // Token: 0x040000DA RID: 218
    private readonly FieldInfo _textureIdsField;

    // Token: 0x040000D4 RID: 212
    public readonly FrameBufferRef[] Framebuffers = new FrameBufferRef[3];

    // Token: 0x040000D3 RID: 211
    private bool _causticsEnabled;

    // Token: 0x040000D8 RID: 216
    private ChunkRenderer _chunkRenderer;

    // Token: 0x040000DD RID: 221
    private float _currentRain;

    // Token: 0x040000D0 RID: 208
    private bool _enabled;

    // Token: 0x040000DC RID: 220
    private int _fbHeight;

    // Token: 0x040000DB RID: 219
    private int _fbWidth;

    // Token: 0x040000DF RID: 223
    private float _rainAccumulator;

    // Token: 0x040000D1 RID: 209
    private bool _rainEnabled;

    // Token: 0x040000D2 RID: 210
    private bool _refractionsEnabled;

    // Token: 0x040000D9 RID: 217
    private MeshRef _screenQuad;

    // Token: 0x040000DE RID: 222
    private float _targetRain;

    // Token: 0x060001C6 RID: 454 RVA: 0x00007860 File Offset: 0x00005A60
    public ScreenSpaceReflections(VolumetricShadingMod mod)
    {
        _mod = mod;
        _game = mod.CApi.GetClient();
        _platform = _game.GetClientPlatformWindows();
        RegisterInjectorProperties();
        mod.CApi.Event.ReloadShader += ReloadShaders;
        mod.Events.PreFinalRender += OnSetFinalUniforms;
        mod.ShaderPatcher.OnReload += RegeneratePatches;
        _enabled = ModSettings.ScreenSpaceReflectionsEnabled;
        _rainEnabled = ModSettings.SSRRainReflectionsEnabled;
        _refractionsEnabled = ModSettings.SSRRefractionsEnabled;
        _causticsEnabled = ModSettings.SSRCausticsEnabled;
        mod.CApi.Settings.AddWatcher("volumetricshading_screenSpaceReflections",
            new OnSettingsChanged<bool>(OnEnabledChanged));
        mod.CApi.Settings.AddWatcher("volumetricshading_SSRRainReflections",
            new OnSettingsChanged<bool>(OnRainReflectionsChanged));
        mod.CApi.Settings.AddWatcher("volumetricshading_SSRRefractions",
            new OnSettingsChanged<bool>(OnRefractionsChanged));
        mod.CApi.Settings.AddWatcher("volumetricshading_SSRCaustics", new OnSettingsChanged<bool>(OnCausticsChanged));
        mod.CApi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "ssrWorldSpace");
        mod.CApi.Event.RegisterRenderer(this, EnumRenderStage.AfterOIT, "ssrOut");
        _textureIdsField = typeof(ChunkRenderer).GetField("textureIds", BindingFlags.Instance | BindingFlags.Public);
        mod.Events.RebuildFramebuffers += SetupFramebuffers;
        SetupFramebuffers(_platform.FrameBuffers);
    }

    // Token: 0x060001CF RID: 463 RVA: 0x0000376B File Offset: 0x0000196B
    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        if (!_enabled)
        {
            return;
        }

        if (_chunkRenderer == null)
        {
            _chunkRenderer = _game.GetChunkRenderer();
        }

        if (stage == EnumRenderStage.Opaque)
        {
            OnPreRender(deltaTime);
            OnRenderSsrChunks();
            return;
        }

        if (stage == EnumRenderStage.AfterOIT)
        {
            OnRenderSsrOut();
        }
    }

    // Token: 0x060001D4 RID: 468 RVA: 0x00008C28 File Offset: 0x00006E28
    public void Dispose()
    {
        var windowsPlatform = _mod.CApi.GetClientPlatformWindows();
        for (var i = 0; i < Framebuffers.Length; i++)
        {
            if (Framebuffers[i] != null)
            {
                windowsPlatform.DisposeFrameBuffer(Framebuffers[i]);
                Framebuffers[i] = null;
            }
        }

        for (var j = 0; j < _shaders.Length; j++)
        {
            var shaderProgram = _shaders[j];
            if (shaderProgram != null)
            {
                shaderProgram.Dispose();
            }

            _shaders[j] = null;
        }

        _chunkRenderer = null;
        _screenQuad = null;
    }

    // Token: 0x1700005C RID: 92
    // (get) Token: 0x060001D5 RID: 469 RVA: 0x0000358C File Offset: 0x0000178C
    public double RenderOrder => 1.0;

    // Token: 0x1700005D RID: 93
    // (get) Token: 0x060001D6 RID: 470 RVA: 0x00002A24 File Offset: 0x00000C24
    public int RenderRange => int.MaxValue;

    // Token: 0x060001C7 RID: 455 RVA: 0x00007A28 File Offset: 0x00005C28
    private void RegeneratePatches()
    {
        var code = _mod.CApi.Assets.Get(new AssetLocation("game", "shaders/chunkliquid.fsh")).ToText();
        var flag = true;
        var extractor = new FunctionExtractor();
        if (!(flag & extractor.Extract(code, "droplethash3") & extractor.Extract(code, "dropletnoise")))
        {
            throw new InvalidOperationException("Could not extract dropletnoise/droplethash3");
        }

        var content = extractor.ExtractedContent;
        content = content.Replace("waterWaveCounter", "waveCounter");
        content = new TokenPatch("float dropletnoise(in vec2 x)")
        {
            ReplacementString = "float dropletnoise(in vec2 x, in float waveCounter)"
        }.Patch("dropletnoise", content);
        content = new TokenPatch("a = smoothstep(0.99, 0.999, a);")
        {
            ReplacementString = "a = smoothstep(0.97, 0.999, a);"
        }.Patch("dropletnoise", content);
        _mod.ShaderInjector["dropletnoise"] = content;
    }

    // Token: 0x060001C8 RID: 456 RVA: 0x00007B00 File Offset: 0x00005D00
    private void RegisterInjectorProperties()
    {
        var shaderInjector = _mod.ShaderInjector;
        shaderInjector.RegisterBoolProperty("VSMOD_SSR", () => ModSettings.ScreenSpaceReflectionsEnabled);
        shaderInjector.RegisterFloatProperty("VSMOD_SSR_WATER_TRANSPARENCY",
            () => (100 - ModSettings.SSRWaterTransparency) * 0.01f);
        shaderInjector.RegisterFloatProperty("VSMOD_SSR_SPLASH_TRANSPARENCY",
            () => (100 - ModSettings.SSRSplashTransparency) * 0.01f);
        shaderInjector.RegisterFloatProperty("VSMOD_SSR_REFLECTION_DIMMING",
            () => ModSettings.SSRReflectionDimming * 0.01f);
        shaderInjector.RegisterFloatProperty("VSMOD_SSR_TINT_INFLUENCE", () => ModSettings.SSRTintInfluence * 0.01f);
        shaderInjector.RegisterFloatProperty("VSMOD_SSR_SKY_MIXIN", () => ModSettings.SSRSkyMixin * 0.01f);
        shaderInjector.RegisterBoolProperty("VSMOD_REFRACT", () => ModSettings.SSRRefractionsEnabled);
        shaderInjector.RegisterBoolProperty("VSMOD_CAUSTICS", () => ModSettings.SSRCausticsEnabled);
    }

    // Token: 0x060001C9 RID: 457 RVA: 0x00003747 File Offset: 0x00001947
    private void OnEnabledChanged(bool enabled)
    {
        _enabled = enabled;
    }

    // Token: 0x060001CA RID: 458 RVA: 0x00003750 File Offset: 0x00001950
    private void OnRainReflectionsChanged(bool enabled)
    {
        _rainEnabled = enabled;
    }

    // Token: 0x060001CB RID: 459 RVA: 0x00003759 File Offset: 0x00001959
    private void OnRefractionsChanged(bool enabled)
    {
        _refractionsEnabled = enabled;
    }

    // Token: 0x060001CC RID: 460 RVA: 0x00003762 File Offset: 0x00001962
    private void OnCausticsChanged(bool enabled)
    {
        _causticsEnabled = enabled;
    }

    // Token: 0x060001CD RID: 461 RVA: 0x00007C68 File Offset: 0x00005E68
    private bool ReloadShaders()
    {
        var success = true;
        for (var i = 0; i < _shaders.Length; i++)
        {
            var shaderProgram = _shaders[i];
            if (shaderProgram != null)
            {
                shaderProgram.Dispose();
            }

            _shaders[i] = null;
        }

        _shaders[0] = _mod.RegisterShader("ssrliquid", ref success);
        _shaders[1] = _mod.RegisterShader("ssropaque", ref success);
        ((ShaderProgram)_shaders[1]).SetCustomSampler("terrainTexLinear", true);
        _shaders[2] = _mod.RegisterShader("ssrtransparent", ref success);
        _shaders[3] = _mod.RegisterShader("ssrtopsoil", ref success);
        _shaders[4] = _mod.RegisterShader("ssrout", ref success);
        _shaders[5] = _mod.RegisterShader("ssrcausticsout", ref success);
        return success;
    }

    // Token: 0x060001CE RID: 462 RVA: 0x00007D5C File Offset: 0x00005F5C
    public void SetupFramebuffers(List<FrameBufferRef> mainBuffers)
    {
        _mod.Mod.Logger.Event("Recreating framebuffers");
        for (var i = 0; i < Framebuffers.Length; i++)
        {
            if (Framebuffers[i] != null)
            {
                _platform.DisposeFrameBuffer(Framebuffers[i]);
                Framebuffers[i] = null;
            }
        }

        _fbWidth = (int)(_platform.window.Bounds.Size.X * ClientSettings.SSAA);
        _fbHeight = (int)(_platform.window.Bounds.Size.Y * ClientSettings.SSAA);
        if (_fbWidth == 0 || _fbHeight == 0)
        {
            return;
        }

        var framebuffer = new FrameBufferRef
        {
            FboId = GL.GenFramebuffer(),
            Width = _fbWidth,
            Height = _fbHeight,
            DepthTextureId = GL.GenTexture()
        };
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.FboId);
        framebuffer.SetupDepthTexture();
        framebuffer.ColorTextureIds = ArrayUtil.CreateFilled(_refractionsEnabled ? 4 : 3, _ => GL.GenTexture());
        framebuffer.SetupVertexTexture(0);
        framebuffer.SetupVertexTexture(1);
        framebuffer.SetupColorTexture(2);
        if (_refractionsEnabled)
        {
            framebuffer.SetupVertexTexture(3);
        }

        if (_refractionsEnabled)
        {
            GL.DrawBuffers(4, new[]
            {
                DrawBuffersEnum.ColorAttachment0,
                DrawBuffersEnum.ColorAttachment1,
                DrawBuffersEnum.ColorAttachment2,
                DrawBuffersEnum.ColorAttachment3
            });
        }
        else
        {
            GL.DrawBuffers(3, new[]
            {
                DrawBuffersEnum.ColorAttachment0,
                DrawBuffersEnum.ColorAttachment1,
                DrawBuffersEnum.ColorAttachment2
            });
        }

        VolumetricShading.Framebuffers.CheckStatus();
        Framebuffers[0] = framebuffer;
        framebuffer = new FrameBufferRef
        {
            FboId = GL.GenFramebuffer(),
            Width = _fbWidth,
            Height = _fbHeight
        };
        GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.FboId);
        framebuffer.ColorTextureIds = new[] { GL.GenTexture() };
        framebuffer.SetupColorTexture(0);
        GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
        VolumetricShading.Framebuffers.CheckStatus();
        Framebuffers[1] = framebuffer;
        if (_causticsEnabled)
        {
            framebuffer = new FrameBufferRef
            {
                FboId = GL.GenFramebuffer(),
                Width = _fbWidth,
                Height = _fbHeight
            };
            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.FboId);
            framebuffer.ColorTextureIds = new[] { GL.GenTexture() };
            framebuffer.SetupSingleColorTexture(0);
            GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            VolumetricShading.Framebuffers.CheckStatus();
            Framebuffers[2] = framebuffer;
        }

        _screenQuad = _platform.GetScreenQuad();
    }

    // Token: 0x060001D0 RID: 464 RVA: 0x00008000 File Offset: 0x00006200
    private void OnPreRender(float dt)
    {
        _rainAccumulator += dt;
        if (_rainAccumulator > 5f)
        {
            _rainAccumulator = 0f;
            var climate = _game.BlockAccessor.GetClimateAt(_game.EntityPlayer.Pos.AsBlockPos);
            var rainMul = GameMath.Clamp((climate.Temperature + 1f) / 4f, 0f, 1f);
            _targetRain = climate.Rainfall * rainMul;
        }

        if (_targetRain > _currentRain)
        {
            _currentRain = Math.Min(_currentRain + dt * 0.15f, _targetRain);
            return;
        }

        if (_targetRain < _currentRain)
        {
            _currentRain = Math.Max(_currentRain - dt * 0.01f, _targetRain);
        }
    }

    // Token: 0x060001D1 RID: 465
    private void OnRenderSsrOut()
    {
        var ssrOutFB = Framebuffers[1];
        var ssrCausticsFB = Framebuffers[2];
        var ssrFB = Framebuffers[0];
        var ssrOutShader = _shaders[4];
        var ssrCausticsShader = _shaders[5];
        if (ssrOutFB == null)
        {
            return;
        }

        if (ssrOutShader == null)
        {
            return;
        }

        GL.Disable(EnableCap.Blend);
        _platform.LoadFrameBuffer(ssrOutFB);
        GL.ClearBuffer(ClearBuffer.Color, 0, new[] { 0f, 0f, 0f, 1f });
        var myUniforms = _mod.Uniforms;
        var uniforms = _mod.CApi.Render.ShaderUniforms;
        var ambient = _mod.CApi.Ambient;
        var shader = ssrOutShader;
        shader.Use();
        shader.BindTexture2D("primaryScene", _platform.FrameBuffers[0].ColorTextureIds[0], 0);
        shader.BindTexture2D("gPosition", ssrFB.ColorTextureIds[0], 1);
        shader.BindTexture2D("gNormal", ssrFB.ColorTextureIds[1], 2);
        shader.BindTexture2D("gDepth", _platform.FrameBuffers[0].DepthTextureId, 3);
        shader.BindTexture2D("gTint", ssrFB.ColorTextureIds[2], 4);
        shader.UniformMatrix("projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
        shader.UniformMatrix("invProjectionMatrix", myUniforms.InvProjectionMatrix);
        shader.UniformMatrix("invModelViewMatrix", myUniforms.InvModelViewMatrix);
        shader.Uniform("zFar", uniforms.ZNear);
        shader.Uniform("sunPosition", _mod.CApi.World.Calendar.SunPositionNormalized);
        shader.Uniform("dayLight", myUniforms.DayLight);
        shader.Uniform("horizonFog", ambient.BlendedCloudDensity);
        shader.Uniform("fogDensityIn", ambient.BlendedFogDensity);
        shader.Uniform("fogMinIn", ambient.BlendedFogMin);
        shader.Uniform("rgbaFog", ambient.BlendedFogColor);
        _platform.RenderFullscreenTriangle(_screenQuad);
        shader.Stop();
        _platform.CheckGlError("Error while calculating SSR");
        if (_causticsEnabled && ssrCausticsFB != null && ssrCausticsShader != null)
        {
            _platform.LoadFrameBuffer(ssrCausticsFB);
            GL.ClearBuffer(ClearBuffer.Color, 0, new[] { 0.5f });
            shader = ssrCausticsShader;
            shader.Use();
            shader.BindTexture2D("gDepth", _platform.FrameBuffers[0].DepthTextureId, 0);
            shader.BindTexture2D("gNormal", ssrFB.ColorTextureIds[1], 1);
            shader.UniformMatrix("invProjectionMatrix", myUniforms.InvProjectionMatrix);
            shader.UniformMatrix("invModelViewMatrix", myUniforms.InvModelViewMatrix);
            shader.Uniform("dayLight", myUniforms.DayLight);
            shader.Uniform("playerPos", uniforms.PlayerPos);
            shader.Uniform("sunPosition", uniforms.SunPosition3D);
            shader.Uniform("waterFlowCounter", uniforms.WaterFlowCounter);
            if (ShaderProgramBase.shadowmapQuality > 0)
            {
                var fbShadowFar = _platform.FrameBuffers[11];
                shader.BindTexture2D("shadowMapFar", fbShadowFar.DepthTextureId, 2);
                shader.BindTexture2D("shadowMapNear", _platform.FrameBuffers[12].DepthTextureId, 3);
                shader.Uniform("shadowMapWidthInv", 1f / fbShadowFar.Width);
                shader.Uniform("shadowMapHeightInv", 1f / fbShadowFar.Height);
                shader.Uniform("shadowRangeFar", uniforms.ShadowRangeFar);
                shader.Uniform("shadowRangeNear", uniforms.ShadowRangeNear);
                shader.UniformMatrix("toShadowMapSpaceMatrixFar", uniforms.ToShadowMapSpaceMatrixFar);
                shader.UniformMatrix("toShadowMapSpaceMatrixNear", uniforms.ToShadowMapSpaceMatrixNear);
            }

            shader.Uniform("fogDensityIn", ambient.BlendedFogDensity);
            shader.Uniform("fogMinIn", ambient.BlendedFogMin);
            shader.Uniform("rgbaFog", ambient.BlendedFogColor);
            _platform.RenderFullscreenTriangle(_screenQuad);
            shader.Stop();
            _platform.CheckGlError("Error while calculating caustics");
        }

        _platform.LoadFrameBuffer(EnumFrameBuffer.Primary);
        GL.Enable(EnableCap.Blend);
    }

    // Token: 0x060001D2 RID: 466 RVA: 0x00008584 File Offset: 0x00006784
    private void OnRenderSsrChunks()
    {
        var ssrFB = Framebuffers[0];
        if (ssrFB == null)
        {
            return;
        }

        if (_shaders[0] == null)
        {
            return;
        }

        var textureIds = _textureIdsField.GetValue(_chunkRenderer) as int[];
        if (textureIds == null)
        {
            return;
        }

        var playerUnderwater = _game.playerProperties.EyesInWaterDepth >= 0.1f ? 0f : 1f;
        var primaryBuffer = _platform.FrameBuffers[0];
        GL.BindFramebuffer(FramebufferTarget.ReadFramebuffer, primaryBuffer.FboId);
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, ssrFB.FboId);
        GL.Clear(ClearBufferMask.DepthBufferBit);
        GL.BlitFramebuffer(0, 0, primaryBuffer.Width, primaryBuffer.Height, 0, 0, _fbWidth, _fbHeight,
            ClearBufferMask.DepthBufferBit, BlitFramebufferFilter.Nearest);
        _platform.LoadFrameBuffer(ssrFB);
        GL.ClearBuffer(ClearBuffer.Color, 0, new[] { 0f, 0f, 0f, 1f });
        GL.ClearBuffer(ClearBuffer.Color, 1, new[] { 0f, 0f, 0f, playerUnderwater });
        GL.ClearBuffer(ClearBuffer.Color, 2, new[] { 0f, 0f, 0f, 1f });
        if (_refractionsEnabled)
        {
            GL.ClearBuffer(ClearBuffer.Color, 3, new[] { 0f, 0f, 0f, 1f });
        }

        _platform.GlEnableCullFace();
        _platform.GlDepthMask(true);
        _platform.GlEnableDepthTest();
        _platform.GlToggleBlend(false);
        var climateAt = _game.BlockAccessor.GetClimateAt(_game.EntityPlayer.Pos.AsBlockPos);
        var num = GameMath.Clamp((float)((climateAt.Temperature + 1.0) / 4.0), 0f, 1f);
        var curRainFall = climateAt.Rainfall * num;
        var cameraPos = _game.EntityPlayer.CameraPos;
        _game.GlPushMatrix();
        _game.GlLoadMatrix(_mod.CApi.Render.CameraMatrixOrigin);
        var shader = _shaders[1];
        shader.Use();
        shader.UniformMatrix("projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
        shader.UniformMatrix("modelViewMatrix", _mod.CApi.Render.CurrentModelviewMatrix);
        shader.Uniform("playerUnderwater", playerUnderwater);
        var pools = _chunkRenderer.poolsByRenderPass[0];
        for (var i = 0; i < textureIds.Length; i++)
        {
            shader.BindTexture2D("terrainTex", textureIds[i], 0);
            shader.BindTexture2D("terrainTexLinear", textureIds[i], 1);
            pools[i].Render(cameraPos, "origin");
        }

        shader.Stop();
        GL.BindSampler(0, 0);
        GL.BindSampler(1, 0);
        if (_rainEnabled)
        {
            shader = _shaders[3];
            shader.Use();
            shader.UniformMatrix("projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
            shader.UniformMatrix("modelViewMatrix", _mod.CApi.Render.CurrentModelviewMatrix);
            shader.Uniform("rainStrength", _currentRain);
            shader.Uniform("playerUnderwater", playerUnderwater);
            pools = _chunkRenderer.poolsByRenderPass[5];
            for (var j = 0; j < textureIds.Length; j++)
            {
                shader.BindTexture2D("terrainTex", textureIds[j], 0);
                pools[j].Render(cameraPos, "origin");
            }

            shader.Stop();
        }

        _platform.GlDisableCullFace();
        shader = _shaders[0];
        shader.Use();
        shader.UniformMatrix("projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
        shader.UniformMatrix("modelViewMatrix", _mod.CApi.Render.CurrentModelviewMatrix);
        shader.Uniform("dropletIntensity", curRainFall);
        shader.Uniform("waterFlowCounter", _platform.ShaderUniforms.WaterFlowCounter);
        shader.Uniform("windSpeed", _platform.ShaderUniforms.WindSpeed);
        shader.Uniform("playerUnderwater", playerUnderwater);
        shader.Uniform("cameraWorldPosition", _mod.Uniforms.CameraWorldPosition);
        pools = _chunkRenderer.poolsByRenderPass[4];
        for (var k = 0; k < textureIds.Length; k++)
        {
            shader.BindTexture2D("terrainTex", textureIds[k], 0);
            pools[k].Render(cameraPos, "origin");
        }

        shader.Stop();
        _platform.GlEnableCullFace();
        shader = _shaders[2];
        shader.Use();
        shader.UniformMatrix("projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
        shader.UniformMatrix("modelViewMatrix", _mod.CApi.Render.CurrentModelviewMatrix);
        shader.Uniform("playerUnderwater", playerUnderwater);
        pools = _chunkRenderer.poolsByRenderPass[3];
        for (var l = 0; l < textureIds.Length; l++)
        {
            shader.BindTexture2D("terrainTex", textureIds[l], 0);
            pools[l].Render(cameraPos, "origin");
        }

        shader.Stop();
        _game.GlPopMatrix();
        _platform.UnloadFrameBuffer(ssrFB);
        _platform.GlDepthMask(false);
        _platform.GlToggleBlend(true);
        _platform.CheckGlError("Error while rendering solid liquids");
    }

    // Token: 0x060001D3 RID: 467 RVA: 0x00008B3C File Offset: 0x00006D3C
    public void OnSetFinalUniforms(ShaderProgramFinal final)
    {
        var ssrOutFB = Framebuffers[1];
        var ssrFB = Framebuffers[0];
        var causticsFB = Framebuffers[2];
        if (!_enabled)
        {
            return;
        }

        if (ssrOutFB == null)
        {
            return;
        }

        final.BindTexture2D("ssrScene", ssrOutFB.ColorTextureIds[0]);
        if ((_refractionsEnabled || _causticsEnabled) && ssrFB != null)
        {
            final.UniformMatrix("projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
            final.BindTexture2D("gpositionScene", ssrFB.ColorTextureIds[0]);
            final.BindTexture2D("gdepthScene", _platform.FrameBuffers[0].DepthTextureId);
        }

        if (_refractionsEnabled && ssrFB != null)
        {
            final.BindTexture2D("refractionScene", ssrFB.ColorTextureIds[3]);
        }

        if (_causticsEnabled && causticsFB != null)
        {
            final.BindTexture2D("causticsScene", causticsFB.ColorTextureIds[0]);
        }
    }
}