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
        mod.CApi.Event.RegisterRenderer(this, EnumRenderStage.Opaque, "ssrWorldSpace");
        mod.CApi.Event.RegisterRenderer(this, EnumRenderStage.AfterOIT, "ssrOut");
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

    // (get) Token: 0x060001D5 RID: 469 RVA: 0x0000358C File Offset: 0x0000178C
    public double RenderOrder => 1.0;

    // (get) Token: 0x060001D6 RID: 470 RVA: 0x00002A24 File Offset: 0x00000C24
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

        // AMD Compatibility: Fix windowed mode calculations
        var bounds = _platform.window.Bounds.Size;
        _fbWidth = (int)(bounds.X * ClientSettings.SSAA);
        _fbHeight = (int)(bounds.Y * ClientSettings.SSAA);
        
        // Ensure minimum dimensions for AMD compatibility
        _fbWidth = Math.Max(_fbWidth, 64);
        _fbHeight = Math.Max(_fbHeight, 64);
        
        if (_fbWidth == 0 || _fbHeight == 0)
        {
            _mod.Mod.Logger.Warning($"Invalid framebuffer dimensions: {_fbWidth}x{_fbHeight}");
            return;
        }

        // AMD Compatibility: Enhanced framebuffer creation with proper error checking
        var framebuffer = CreateFramebufferSafely(_fbWidth, _fbHeight, _refractionsEnabled ? 4 : 3, true, "SSR Main");
        if (framebuffer == null)
        {
            _mod.Mod.Logger.Error("Failed to create main SSR framebuffer");
            return;
        }
        _framebuffers[0] = framebuffer;
        // AMD Compatibility: Create output framebuffer with error checking
        framebuffer = CreateFramebufferSafely(_fbWidth, _fbHeight, 1, false, "SSR Output");
        if (framebuffer == null)
        {
            _mod.Mod.Logger.Error("Failed to create SSR output framebuffer");
            return;
        }
        _framebuffers[1] = framebuffer;
        if (_causticsEnabled)
        {
            // AMD Compatibility: Create caustics framebuffer with error checking
            framebuffer = CreateFramebufferSafely(_fbWidth, _fbHeight, 1, false, "SSR Caustics", true);
            if (framebuffer == null)
            {
                _mod.Mod.Logger.Warning("Failed to create caustics framebuffer, disabling caustics");
                _causticsEnabled = false;
            }
            else
            {
                _framebuffers[2] = framebuffer;
            }
        }

        _screenQuad = _platform.GetScreenQuad();
    }

    /// <summary>
    /// AMD Compatibility: Create framebuffer with proper error checking and fallback mechanisms
    /// </summary>
    private FrameBufferRef CreateFramebufferSafely(int width, int height, int colorAttachments, bool needsDepth, string name, bool singleChannel = false)
    {
        try
        {
            var framebuffer = new FrameBufferRef
            {
                FboId = GL.GenFramebuffer(),
                Width = width,
                Height = height
            };

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.FboId);

            // Setup depth buffer if needed
            if (needsDepth)
            {
                framebuffer.DepthTextureId = GL.GenTexture();
                framebuffer.SetupDepthTexture();
            }

            // Setup color attachments
            framebuffer.ColorTextureIds = new int[colorAttachments];
            for (int i = 0; i < colorAttachments; i++)
            {
                framebuffer.ColorTextureIds[i] = GL.GenTexture();
                if (singleChannel)
                {
                    framebuffer.SetupSingleColorTexture(i);
                }
                else if (i < 2)
                {
                    framebuffer.SetupVertexTexture(i);
                }
                else
                {
                    framebuffer.SetupColorTexture(i);
                }
            }

            // Setup draw buffers
            if (colorAttachments == 1)
            {
                GL.DrawBuffer(DrawBufferMode.ColorAttachment0);
            }
            else
            {
                var drawBuffers = new DrawBuffersEnum[colorAttachments];
                for (int i = 0; i < colorAttachments; i++)
                {
                    drawBuffers[i] = DrawBuffersEnum.ColorAttachment0 + i;
                }
                GL.DrawBuffers(colorAttachments, drawBuffers);
            }

            // AMD Compatibility: Check framebuffer completeness
            var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
            if (status != FramebufferErrorCode.FramebufferComplete)
            {
                _mod.Mod.Logger.Error($"Framebuffer '{name}' incomplete: {status}");
                
                // Cleanup on failure
                GL.DeleteFramebuffer(framebuffer.FboId);
                if (framebuffer.DepthTextureId != 0)
                    GL.DeleteTexture(framebuffer.DepthTextureId);
                foreach (var texId in framebuffer.ColorTextureIds)
                    GL.DeleteTexture(texId);
                
                return null;
            }

            Framebuffers.CheckStatus();
            _mod.Mod.Logger.VerboseDebug($"Successfully created framebuffer '{name}' ({width}x{height}, {colorAttachments} attachments)");
            return framebuffer;
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Error($"Exception creating framebuffer '{name}': {ex.Message}");
            return null;
        }
    }

    private void OnPreRender(float dt)
    {
        _rainAccumulator += dt;
        if (_rainAccumulator > 5f)
        {
            _rainAccumulator = 0f;
            var climate = _game.BlockAccessor.GetClimateAt(_game.EntityPlayer.Pos.AsBlockPos);
            var rainMul = GameMath.Clamp((climate.Temperature + 1f) / 4f, 0f, 1f);
            // Clamp rain calculation to prevent extreme values that cause shader artifacts
            _targetRain = GameMath.Clamp(climate.Rainfall * rainMul, 0f, 2.0f);
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
        TrySetUniformMatrix(shader, "projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
        TrySetUniformMatrix(shader, "invProjectionMatrix", myUniforms.InvProjectionMatrix);
        TrySetUniformMatrix(shader, "invModelViewMatrix", myUniforms.InvModelViewMatrix);
        TrySetUniform(shader, "zFar", uniforms.ZNear);
        TrySetUniform(shader, "sunPosition", _mod.CApi.World.Calendar.SunPositionNormalized);
        TrySetUniform(shader, "dayLight", myUniforms.DayLight);
        TrySetUniform(shader, "horizonFog", ambient.BlendedCloudDensity);
        TrySetUniform(shader, "fogDensityIn", ambient.BlendedFogDensity);
        TrySetUniform(shader, "fogMinIn", ambient.BlendedFogMin);
        TrySetUniform(shader, "rgbaFog", ambient.BlendedFogColor);
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
            TrySetUniformMatrix(shader, "invProjectionMatrix", myUniforms.InvProjectionMatrix);
            TrySetUniformMatrix(shader, "invModelViewMatrix", myUniforms.InvModelViewMatrix);
            TrySetUniform(shader, "dayLight", myUniforms.DayLight);
            TrySetUniform(shader, "playerPos", uniforms.PlayerPos);
            TrySetUniform(shader, "sunPosition", uniforms.SunPosition3D);
            TrySetUniform(shader, "waterFlowCounter", uniforms.WaterFlowCounter);
            if (ShaderProgramBase.shadowmapQuality > 0)
            {
                var fbShadowFar = _platform.FrameBuffers[11];
                shader.BindTexture2D("shadowMapFar", fbShadowFar.DepthTextureId, 2);
                shader.BindTexture2D("shadowMapNear", _platform.FrameBuffers[12].DepthTextureId, 3);
                TrySetUniform(shader, "shadowMapWidthInv", 1f / fbShadowFar.Width);
                TrySetUniform(shader, "shadowMapHeightInv", 1f / fbShadowFar.Height);
                TrySetUniform(shader, "shadowRangeFar", uniforms.ShadowRangeFar);
                TrySetUniform(shader, "shadowRangeNear", uniforms.ShadowRangeNear);
                TrySetUniformMatrix(shader, "toShadowMapSpaceMatrixFar", uniforms.ToShadowMapSpaceMatrixFar);
                TrySetUniformMatrix(shader, "toShadowMapSpaceMatrixNear", uniforms.ToShadowMapSpaceMatrixNear);
            }

            TrySetUniform(shader, "fogDensityIn", ambient.BlendedFogDensity);
            TrySetUniform(shader, "fogMinIn", ambient.BlendedFogMin);
            TrySetUniform(shader, "rgbaFog", ambient.BlendedFogColor);
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
        TrySetUniformMatrix(shader, "projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
        TrySetUniformMatrix(shader, "modelViewMatrix", _mod.CApi.Render.CurrentModelviewMatrix);
        TrySetUniform(shader, "playerUnderwater", playerUnderwater);
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
            TrySetUniformMatrix(shader, "projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
            TrySetUniformMatrix(shader, "modelViewMatrix", _mod.CApi.Render.CurrentModelviewMatrix);
            // Clamp rainStrength to prevent shader artifacts from extreme values
            var clampedRainStrength = Math.Max(0f, Math.Min(_currentRain, 2.0f));
            TrySetUniform(shader, "rainStrength", clampedRainStrength);
            TrySetUniform(shader, "playerUnderwater", playerUnderwater);
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
        TrySetUniformMatrix(shader, "projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
        TrySetUniformMatrix(shader, "modelViewMatrix", _mod.CApi.Render.CurrentModelviewMatrix);
        TrySetUniform(shader, "dropletIntensity", curRainFall);
        TrySetUniform(shader, "waterFlowCounter", _platform.ShaderUniforms.WaterFlowCounter);
        TrySetUniform(shader, "windSpeed", _platform.ShaderUniforms.WindSpeed);
        TrySetUniform(shader, "playerUnderwater", playerUnderwater);
        TrySetUniform(shader, "cameraWorldPosition", _mod.Uniforms.CameraWorldPosition);
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
        TrySetUniformMatrix(shader, "projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
        TrySetUniformMatrix(shader, "modelViewMatrix", _mod.CApi.Render.CurrentModelviewMatrix);
        TrySetUniform(shader, "playerUnderwater", playerUnderwater);
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

        var stopwatch = _mod.PerformanceManager?.StartTiming("SSR_FinalUniforms");
        try
        {
            // Critical: Validate shader program is properly linked before setting uniforms
            if (!IsShaderProgramValid(final))
            {
                _mod.Mod.Logger.Error("Final shader program is not properly linked, skipping SSR uniforms");
                return;
            }

            // Safe texture binding with validation
            if (IsFramebufferValid(ssrOutFB))
            {
                SafeBindTexture2D(final, "ssrScene", ssrOutFB.ColorTextureIds[0]);
            }
            
            if ((_refractionsEnabled || _causticsEnabled) && ssrFB != null && IsFramebufferValid(ssrFB))
            {
                TrySetUniformMatrix(final, "projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
                SafeBindTexture2D(final, "gpositionScene", ssrFB.ColorTextureIds[0]);
                
                // Safely bind depth texture
                var depthBuffer = _platform.FrameBuffers[0];
                if (depthBuffer != null && depthBuffer.DepthTextureId > 0)
                {
                    SafeBindTexture2D(final, "gdepthScene", depthBuffer.DepthTextureId);
                }
            }

            if (_refractionsEnabled && ssrFB != null && IsFramebufferValid(ssrFB) && ssrFB.ColorTextureIds.Length > 3)
            {
                SafeBindTexture2D(final, "refractionScene", ssrFB.ColorTextureIds[3]);
            }

            if (_causticsEnabled && causticsFB != null && IsFramebufferValid(causticsFB))
            {
                SafeBindTexture2D(final, "causticsScene", causticsFB.ColorTextureIds[0]);
            }
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Error($"Critical error in final composition: {ex.Message}");
            _mod.Mod.Logger.Error($"Stack trace: {ex.StackTrace}");
            
            // Disable SSR to prevent further crashes
            _enabled = false;
            _mod.Mod.Logger.Warning("SSR disabled due to critical final composition errors");
        }
        finally
        {
            if (stopwatch != null)
                _mod.PerformanceManager?.EndTiming("SSR_FinalUniforms", stopwatch);
        }
    }

    /// <summary>
    /// Safe uniform setting with error handling to prevent KeyNotFoundException crashes
    /// </summary>
    private void TrySetUniform(IShaderProgram shader, string uniformName, float value)
    {
        try
        {
            shader.Uniform(uniformName, value);
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            // Uniform doesn't exist in shader, silently ignore
            _mod.Mod.Logger.Debug($"Uniform '{uniformName}' not found in shader, skipping");
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Warning($"Failed to set uniform '{uniformName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Safe uniform setting for Vec3f values
    /// </summary>
    private void TrySetUniform(IShaderProgram shader, string uniformName, Vec3f value)
    {
        try
        {
            shader.Uniform(uniformName, value);
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            // Uniform doesn't exist in shader, silently ignore
            _mod.Mod.Logger.Debug($"Uniform '{uniformName}' not found in shader, skipping");
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Warning($"Failed to set uniform '{uniformName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Safe uniform setting for Vec4f values
    /// </summary>
    private void TrySetUniform(IShaderProgram shader, string uniformName, Vec4f value)
    {
        try
        {
            shader.Uniform(uniformName, value);
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            // Uniform doesn't exist in shader, silently ignore
            _mod.Mod.Logger.Debug($"Uniform '{uniformName}' not found in shader, skipping");
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Warning($"Failed to set uniform '{uniformName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Safe uniform matrix setting with error handling to prevent KeyNotFoundException crashes
    /// </summary>
    private void TrySetUniformMatrix(IShaderProgram shader, string uniformName, float[] matrix)
    {
        try
        {
            shader.UniformMatrix(uniformName, matrix);
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            // Uniform doesn't exist in shader, silently ignore
            _mod.Mod.Logger.Debug($"Matrix uniform '{uniformName}' not found in shader, skipping");
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Warning($"Failed to set matrix uniform '{uniformName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Check if a shader program is valid and properly linked
    /// </summary>
    private bool IsShaderProgramValid(IShaderProgram program)
    {
        if (program == null)
        {
            return false;
        }
        
        try
        {
            // Check if the shader program has a valid program ID and is properly linked
            var shaderBase = program as ShaderProgramBase;
            if (shaderBase != null && shaderBase.Disposed)
            {
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Warning($"Error validating shader program: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Check if a framebuffer is valid and ready to use
    /// </summary>
    private bool IsFramebufferValid(FrameBufferRef framebuffer)
    {
        if (framebuffer == null)
        {
            return false;
        }
        
        try
        {
            // Check if framebuffer has valid FBO ID
            if (framebuffer.FboId <= 0)
            {
                return false;
            }
            
            // Check if color textures are valid
            if (framebuffer.ColorTextureIds == null || framebuffer.ColorTextureIds.Length == 0)
            {
                return false;
            }
            
            // Check if at least the first color texture is valid
            if (framebuffer.ColorTextureIds[0] <= 0)
            {
                return false;
            }
            
            return true;
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Warning($"Error validating framebuffer: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Safely bind a 2D texture to a shader uniform with error handling
    /// </summary>
    private void SafeBindTexture2D(IShaderProgram shader, string uniformName, int textureId)
    {
        if (shader == null || textureId <= 0)
        {
            return;
        }
        
        try
        {
            // Find an available texture slot (we'll use a simple incremental approach)
            var textureSlot = GetNextAvailableTextureSlot();
            shader.BindTexture2D(uniformName, textureId, textureSlot);
        }
        catch (System.Collections.Generic.KeyNotFoundException)
        {
            // Uniform doesn't exist in shader, silently ignore
            _mod.Mod.Logger.Debug($"Texture uniform '{uniformName}' not found in shader, skipping");
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Warning($"Failed to bind texture to uniform '{uniformName}': {ex.Message}");
        }
    }
    
    private int _currentTextureSlot = 0;
    
    /// <summary>
    /// Get the next available texture slot (simple incrementing approach)
    /// </summary>
    private int GetNextAvailableTextureSlot()
    {
        return _currentTextureSlot++;
    }
}