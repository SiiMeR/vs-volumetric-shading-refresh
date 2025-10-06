using System;
using System.Collections.Generic;
using System.Reflection;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using VolumetricShadingRefreshed.VolumetricShading.Patch;

namespace VolumetricShadingRefreshed.VolumetricShading.Effects;

public class ScreenSpaceReflections : IRenderer, IDisposable
{
    private readonly FrameBufferRef[] _framebuffers = new FrameBufferRef[3];

    private readonly ClientMain _game;

    private readonly VolumetricShadingMod _mod;

    private readonly ClientPlatformWindows _platform;

    private readonly IShaderProgram[] _shaders = new IShaderProgram[6];

    private readonly FieldInfo _textureIdsField;

    private bool _causticsEnabled;

    private ChunkRenderer _chunkRenderer;

    private float _currentRain;

    private bool _enabled;

    private int _fbHeight;

    private int _fbWidth;

    private float _rainAccumulator;

    private bool _rainEnabled;

    private bool _refractionsEnabled;

    private MeshRef _screenQuad;

    private float _targetRain;

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

        //mod.CApi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "ssrWorldSpace");
        //mod.CApi.Event.RegisterRenderer(this, EnumRenderStage.AfterOIT, "ssrOut");

        // Doing this so Both ssr passes are done afterOIT (the main scene)(it includes mobs/entities, should fix the water in boat bug)
        mod.CApi.Event.RegisterRenderer(this, EnumRenderStage.AfterOIT, "ssr");

        _textureIdsField = typeof(ChunkRenderer).GetField("textureIds", BindingFlags.Instance | BindingFlags.Public);
        mod.Events.RebuildFramebuffers += SetupFramebuffers;
        SetupFramebuffers(_platform.FrameBuffers);
    }

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

        /* Keeping this here temporarily (also the ones above) just so if any unseen concequences do occurr it'll be easy to fix (hopefully) 
        if (stage == EnumRenderStage.Opaque)
        {
            OnPreRender(deltaTime);
            OnRenderSsrChunks();
            return;
        }

        if (stage == EnumRenderStage.AfterOIT)
        {
            OnRenderSsrOut();
        }*/

        // Both pass called in one render stage.
        if (stage == EnumRenderStage.AfterOIT)
        {
            OnPreRender(deltaTime);
            OnRenderSsrChunks(); 
            OnRenderSsrOut();
        } 
    }

    public void Dispose()
    {
        var windowsPlatform = _mod.CApi.GetClientPlatformWindows();
        for (var i = 0; i < _framebuffers.Length; i++)
        {
            if (_framebuffers[i] != null)
            {
                windowsPlatform.DisposeFrameBuffer(_framebuffers[i]);
                _framebuffers[i] = null;
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

    public double RenderOrder => 1.0;


    public int RenderRange => int.MaxValue;

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

    private void OnEnabledChanged(bool enabled)
    {
        _enabled = enabled;
    }

    private void OnRainReflectionsChanged(bool enabled)
    {
        _rainEnabled = enabled;
    }

    private void OnRefractionsChanged(bool enabled)
    {
        _refractionsEnabled = enabled;
    }

    private void OnCausticsChanged(bool enabled)
    {
        _causticsEnabled = enabled;
    }

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

    public void SetupFramebuffers(List<FrameBufferRef> mainBuffers)
    {
        _mod.Mod.Logger.Event("Recreating framebuffers");
        for (var i = 0; i < _framebuffers.Length; i++)
        {
            if (_framebuffers[i] != null)
            {
                _platform.DisposeFrameBuffer(_framebuffers[i]);
                _framebuffers[i] = null;
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

        Framebuffers.CheckStatus();
        _framebuffers[0] = framebuffer;
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
        Framebuffers.CheckStatus();
        _framebuffers[1] = framebuffer;
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
            Framebuffers.CheckStatus();
            _framebuffers[2] = framebuffer;
        }

        _screenQuad = _platform.GetScreenQuad();
    }

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

    private void OnRenderSsrOut()
    {
        var ssrOutFB = _framebuffers[1];
        var ssrCausticsFB = _framebuffers[2];
        var ssrFB = _framebuffers[0];
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

    private void OnRenderSsrChunks()
    {
        var ssrFB = _framebuffers[0];
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

    public void OnSetFinalUniforms(ShaderProgramFinal final)
    {
        var ssrOutFB = _framebuffers[1];
        var ssrFB = _framebuffers[0];
        var causticsFB = _framebuffers[2];
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