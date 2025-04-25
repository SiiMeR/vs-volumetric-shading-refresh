using System;
using System.Collections.Generic;
using System.Reflection;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.Client.NoObf;
using volumetricshadingupdated.VolumetricShading.Patch;

namespace volumetricshadingupdated.VolumetricShading.Effects;

public class ScreenSpaceReflections : IRenderer, IDisposable
{
    private readonly VolumetricShadingMod _mod;

    private bool _enabled;

    private bool _rainEnabled;

    private bool _refractionsEnabled;

    private bool _causticsEnabled;

    private readonly FrameBufferRef[] _framebuffers = (FrameBufferRef[])(object)new FrameBufferRef[3];

    private readonly IShaderProgram[] _shaders = (IShaderProgram[])(object)new IShaderProgram[6];

    private readonly ClientMain _game;

    private readonly ClientPlatformWindows _platform;

    private ChunkRenderer _chunkRenderer;

    private MeshRef _screenQuad;

    private readonly FieldInfo _textureIdsField;

    private int _fbWidth;

    private int _fbHeight;

    private float _currentRain;

    private float _targetRain;

    private float _rainAccumulator;

    public double RenderOrder => 1.0;

    public int RenderRange => int.MaxValue;

    public ScreenSpaceReflections(VolumetricShadingMod mod)
    {
        //IL_005f: Unknown result type (might be due to invalid IL or missing references)
        //IL_0069: Expected O, but got Unknown
        _mod = mod;
        _game = mod.CApi.GetClient();
        _platform = _game.GetClientPlatformWindows();
        RegisterInjectorProperties();
        mod.CApi.Event.ReloadShader += new ActionBoolReturn(ReloadShaders);
        mod.Events.PreFinalRender += OnSetFinalUniforms;
        mod.ShaderPatcher.OnReload += RegeneratePatches;
        _enabled = ModSettings.ScreenSpaceReflectionsEnabled;
        _rainEnabled = ModSettings.SSRRainReflectionsEnabled;
        _refractionsEnabled = ModSettings.SSRRefractionsEnabled;
        _causticsEnabled = ModSettings.SSRCausticsEnabled;
        mod.CApi.Settings.AddWatcher<bool>("volumetricshading_screenSpaceReflections",
            (OnSettingsChanged<bool>)OnEnabledChanged);
        mod.CApi.Settings.AddWatcher<bool>("volumetricshading_SSRRainReflections",
            (OnSettingsChanged<bool>)OnRainReflectionsChanged);
        mod.CApi.Settings.AddWatcher<bool>("volumetricshading_SSRRefractions",
            (OnSettingsChanged<bool>)OnRefractionsChanged);
        mod.CApi.Settings.AddWatcher<bool>("volumetricshading_SSRCaustics", (OnSettingsChanged<bool>)OnCausticsChanged);
        mod.CApi.Event.RegisterRenderer((IRenderer)(object)this, (EnumRenderStage)1, "ssrWorldSpace");
        mod.CApi.Event.RegisterRenderer((IRenderer)(object)this, (EnumRenderStage)3, "ssrOut");
        _textureIdsField = typeof(ChunkRenderer).GetField("textureIds", BindingFlags.Instance | BindingFlags.NonPublic);
        mod.Events.RebuildFramebuffers += SetupFramebuffers;
        SetupFramebuffers(((ClientPlatformAbstract)_platform).FrameBuffers);
    }

    private void RegeneratePatches()
    {
        //IL_001a: Unknown result type (might be due to invalid IL or missing references)
        //IL_0024: Expected O, but got Unknown
        string code = ((ICoreAPI)_mod.CApi).Assets.Get(new AssetLocation("game", "shaders/chunkliquid.fsh")).ToText();
        FunctionExtractor functionExtractor = new FunctionExtractor();
        if ((1u & (functionExtractor.Extract(code, "droplethash3") ? 1u : 0u) &
             (functionExtractor.Extract(code, "dropletnoise") ? 1u : 0u)) == 0)
        {
            throw new InvalidOperationException("Could not extract dropletnoise/droplethash3");
        }

        string extractedContent = functionExtractor.ExtractedContent;
        extractedContent = extractedContent.Replace("waterWaveCounter", "waveCounter");
        extractedContent = new TokenPatch("float dropletnoise(in vec2 x)")
        {
            ReplacementString = "float dropletnoise(in vec2 x, in float waveCounter)"
        }.Patch("dropletnoise", extractedContent);
        extractedContent = new TokenPatch("a = smoothstep(0.99, 0.999, a);")
        {
            ReplacementString = "a = smoothstep(0.97, 0.999, a);"
        }.Patch("dropletnoise", extractedContent);
        _mod.ShaderInjector["dropletnoise"] = extractedContent;
    }

    private void RegisterInjectorProperties()
    {
        ShaderInjector shaderInjector = _mod.ShaderInjector;
        shaderInjector.RegisterBoolProperty("VSMOD_SSR", () => ModSettings.ScreenSpaceReflectionsEnabled);
        shaderInjector.RegisterFloatProperty("VSMOD_SSR_WATER_TRANSPARENCY",
            () => (float)(100 - ModSettings.SSRWaterTransparency) * 0.01f);
        shaderInjector.RegisterFloatProperty("VSMOD_SSR_SPLASH_TRANSPARENCY",
            () => (float)(100 - ModSettings.SSRSplashTransparency) * 0.01f);
        shaderInjector.RegisterFloatProperty("VSMOD_SSR_REFLECTION_DIMMING",
            () => (float)ModSettings.SSRReflectionDimming * 0.01f);
        shaderInjector.RegisterFloatProperty("VSMOD_SSR_TINT_INFLUENCE",
            () => (float)ModSettings.SSRTintInfluence * 0.01f);
        shaderInjector.RegisterFloatProperty("VSMOD_SSR_SKY_MIXIN", () => (float)ModSettings.SSRSkyMixin * 0.01f);
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
        //IL_006d: Unknown result type (might be due to invalid IL or missing references)
        bool success = true;
        for (int i = 0; i < _shaders.Length; i++)
        {
            ((IDisposable)_shaders[i])?.Dispose();
            _shaders[i] = null;
        }

        _shaders[0] = _mod.RegisterShader("ssrliquid", ref success);
        _shaders[1] = _mod.RegisterShader("ssropaque", ref success);
        ((ShaderProgramBase)(ShaderProgram)_shaders[1]).SetCustomSampler("terrainTexLinear", true);
        _shaders[2] = _mod.RegisterShader("ssrtransparent", ref success);
        _shaders[3] = _mod.RegisterShader("ssrtopsoil", ref success);
        _shaders[4] = _mod.RegisterShader("ssrout", ref success);
        _shaders[5] = _mod.RegisterShader("ssrcausticsout", ref success);
        return success;
    }

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
        framebuffer.ColorTextureIds =
            ArrayUtil.CreateFilled<int>(this._refractionsEnabled ? 4 : 3, (int _) => GL.GenTexture());
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

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        //IL_0022: Unknown result type (might be due to invalid IL or missing references)
        //IL_0024: Invalid comparison between Unknown and I4
        //IL_0034: Unknown result type (might be due to invalid IL or missing references)
        //IL_0036: Invalid comparison between Unknown and I4
        if (_enabled)
        {
            if (_chunkRenderer == null)
            {
                _chunkRenderer = _game.GetChunkRenderer();
            }

            if ((int)stage == 1)
            {
                OnPreRender(deltaTime);
                OnRenderSsrChunks();
            }
            else if ((int)stage == 3)
            {
                OnRenderSsrOut();
            }
        }
    }

    private void OnPreRender(float dt)
    {
        _rainAccumulator += dt;
        if (_rainAccumulator > 5f)
        {
            _rainAccumulator = 0f;
            ClimateCondition climateAt = _game.BlockAccessor.GetClimateAt(((Entity)_game.EntityPlayer).Pos.AsBlockPos,
                (EnumGetClimateMode)1, 0.0);
            float num = GameMath.Clamp((climateAt.Temperature + 1f) / 4f, 0f, 1f);
            _targetRain = climateAt.Rainfall * num;
        }

        if (_targetRain > _currentRain)
        {
            _currentRain = Math.Min(_currentRain + dt * 0.15f, _targetRain);
        }
        else if (_targetRain < _currentRain)
        {
            _currentRain = Math.Max(_currentRain - dt * 0.01f, _targetRain);
        }
    }

    private void OnRenderSsrOut()
    {
        FrameBufferRef val = _framebuffers[1];
        FrameBufferRef val2 = _framebuffers[2];
        FrameBufferRef val3 = _framebuffers[0];
        IShaderProgram val4 = _shaders[4];
        IShaderProgram val5 = _shaders[5];
        if (val == null || val4 == null)
        {
            return;
        }

        GL.Disable((EnableCap)3042);
        ((ClientPlatformAbstract)_platform).LoadFrameBuffer(val);
        GL.ClearBuffer((ClearBuffer)6144, 0, new float[4] { 0f, 0f, 0f, 1f });
        Uniforms uniforms = _mod.Uniforms;
        DefaultShaderUniforms shaderUniforms = _mod.CApi.Render.ShaderUniforms;
        IAmbientManager ambient = _mod.CApi.Ambient;
        IShaderProgram val6 = val4;
        val6.Use();
        val6.BindTexture2D("primaryScene", ((ClientPlatformAbstract)_platform).FrameBuffers[0].ColorTextureIds[0], 0);
        val6.BindTexture2D("gPosition", val3.ColorTextureIds[0], 1);
        val6.BindTexture2D("gNormal", val3.ColorTextureIds[1], 2);
        val6.BindTexture2D("gDepth", ((ClientPlatformAbstract)_platform).FrameBuffers[0].DepthTextureId, 3);
        val6.BindTexture2D("gTint", val3.ColorTextureIds[2], 4);
        val6.UniformMatrix("projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
        val6.UniformMatrix("invProjectionMatrix", uniforms.InvProjectionMatrix);
        val6.UniformMatrix("invModelViewMatrix", uniforms.InvModelViewMatrix);
        if (val6.HasUniform("zNear"))
        {
            val6.Uniform("zNear", shaderUniforms.ZNear);
        }

        val6.Uniform("zFar", shaderUniforms.ZNear);
        val6.Uniform("sunPosition", _mod.CApi.World.Calendar.SunPositionNormalized);
        val6.Uniform("dayLight", uniforms.DayLight);
        val6.Uniform("horizonFog", ambient.BlendedCloudDensity);
        val6.Uniform("fogDensityIn", ambient.BlendedFogDensity);
        val6.Uniform("fogMinIn", ambient.BlendedFogMin);
        val6.Uniform("rgbaFog", ambient.BlendedFogColor);
        _platform.RenderFullscreenTriangle(_screenQuad);
        val6.Stop();
        ((ClientPlatformAbstract)_platform).CheckGlError("Error while calculating SSR");
        if (_causticsEnabled && val2 != null && val5 != null)
        {
            ((ClientPlatformAbstract)_platform).LoadFrameBuffer(val2);
            GL.ClearBuffer((ClearBuffer)6144, 0, new float[1] { 0.5f });
            val6 = val5;
            val6.Use();
            val6.BindTexture2D("gDepth", ((ClientPlatformAbstract)_platform).FrameBuffers[0].DepthTextureId, 0);
            val6.BindTexture2D("gNormal", val3.ColorTextureIds[1], 1);
            val6.UniformMatrix("invProjectionMatrix", uniforms.InvProjectionMatrix);
            val6.UniformMatrix("invModelViewMatrix", uniforms.InvModelViewMatrix);
            val6.Uniform("dayLight", uniforms.DayLight);
            val6.Uniform("playerPos", shaderUniforms.PlayerPos);
            val6.Uniform("sunPosition", shaderUniforms.SunPosition3D);
            val6.Uniform("waterFlowCounter", shaderUniforms.WaterFlowCounter);
            if (ShaderProgramBase.shadowmapQuality > 0)
            {
                FrameBufferRef val7 = ((ClientPlatformAbstract)_platform).FrameBuffers[11];
                val6.BindTexture2D("shadowMapFar", val7.DepthTextureId, 2);
                val6.BindTexture2D("shadowMapNear", ((ClientPlatformAbstract)_platform).FrameBuffers[12].DepthTextureId,
                    3);
                val6.Uniform("shadowMapWidthInv", 1f / (float)val7.Width);
                val6.Uniform("shadowMapHeightInv", 1f / (float)val7.Height);
                val6.Uniform("shadowRangeFar", shaderUniforms.ShadowRangeFar);
                val6.Uniform("shadowRangeNear", shaderUniforms.ShadowRangeNear);
                val6.UniformMatrix("toShadowMapSpaceMatrixFar", shaderUniforms.ToShadowMapSpaceMatrixFar);
                val6.UniformMatrix("toShadowMapSpaceMatrixNear", shaderUniforms.ToShadowMapSpaceMatrixNear);
            }

            val6.Uniform("fogDensityIn", ambient.BlendedFogDensity);
            val6.Uniform("fogMinIn", ambient.BlendedFogMin);
            val6.Uniform("rgbaFog", ambient.BlendedFogColor);
            _platform.RenderFullscreenTriangle(_screenQuad);
            val6.Stop();
            ((ClientPlatformAbstract)_platform).CheckGlError("Error while calculating caustics");
        }

        ((ClientPlatformAbstract)_platform).LoadFrameBuffer((EnumFrameBuffer)0);
        GL.Enable((EnableCap)3042);
    }

    private void OnRenderSsrChunks()
    {
        FrameBufferRef val = _framebuffers[0];
        if (val == null || _shaders[0] == null || !(_textureIdsField.GetValue(_chunkRenderer) is int[] array))
        {
            return;
        }

        float num = ((_game.playerProperties.EyesInWaterDepth >= 0.1f) ? 0f : 1f);
        FrameBufferRef val2 = ((ClientPlatformAbstract)_platform).FrameBuffers[0];
        GL.BindFramebuffer((FramebufferTarget)36008, val2.FboId);
        GL.BindFramebuffer((FramebufferTarget)36009, val.FboId);
        GL.Clear((ClearBufferMask)256);
        GL.BlitFramebuffer(0, 0, val2.Width, val2.Height, 0, 0, _fbWidth, _fbHeight, (ClearBufferMask)256,
            (BlitFramebufferFilter)9728);
        ((ClientPlatformAbstract)_platform).LoadFrameBuffer(val);
        GL.ClearBuffer((ClearBuffer)6144, 0, new float[4] { 0f, 0f, 0f, 1f });
        GL.ClearBuffer((ClearBuffer)6144, 1, new float[4] { 0f, 0f, 0f, num });
        GL.ClearBuffer((ClearBuffer)6144, 2, new float[4] { 0f, 0f, 0f, 1f });
        if (_refractionsEnabled)
        {
            GL.ClearBuffer((ClearBuffer)6144, 3, new float[4] { 0f, 0f, 0f, 1f });
        }

        ((ClientPlatformAbstract)_platform).GlEnableCullFace();
        ((ClientPlatformAbstract)_platform).GlDepthMask(true);
        ((ClientPlatformAbstract)_platform).GlEnableDepthTest();
        ((ClientPlatformAbstract)_platform).GlToggleBlend(false, (EnumBlendMode)0);
        ClimateCondition climateAt =
            _game.BlockAccessor.GetClimateAt(((Entity)_game.EntityPlayer).Pos.AsBlockPos, (EnumGetClimateMode)1, 0.0);
        float num2 = GameMath.Clamp((float)(((double)climateAt.Temperature + 1.0) / 4.0), 0f, 1f);
        float num3 = climateAt.Rainfall * num2;
        Vec3d cameraPos = _game.EntityPlayer.CameraPos;
        _game.GlPushMatrix();
        _game.GlLoadMatrix(_mod.CApi.Render.CameraMatrixOrigin);
        IShaderProgram val3 = _shaders[1];
        val3.Use();
        val3.UniformMatrix("projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
        val3.UniformMatrix("modelViewMatrix", _mod.CApi.Render.CurrentModelviewMatrix);
        val3.Uniform("playerUnderwater", num);
        MeshDataPoolManager[] array2 = _chunkRenderer.poolsByRenderPass[0];
        for (int i = 0; i < array.Length; i++)
        {
            val3.BindTexture2D("terrainTex", array[i], 0);
            val3.BindTexture2D("terrainTexLinear", array[i], 1);
            array2[i].Render(cameraPos, "origin", (EnumFrustumCullMode)1);
        }

        val3.Stop();
        GL.BindSampler(0, 0);
        GL.BindSampler(1, 0);
        if (_rainEnabled)
        {
            val3 = _shaders[3];
            val3.Use();
            val3.UniformMatrix("projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
            val3.UniformMatrix("modelViewMatrix", _mod.CApi.Render.CurrentModelviewMatrix);
            val3.Uniform("rainStrength", _currentRain);
            val3.Uniform("playerUnderwater", num);
            array2 = _chunkRenderer.poolsByRenderPass[5];
            for (int j = 0; j < array.Length; j++)
            {
                val3.BindTexture2D("terrainTex", array[j], 0);
                array2[j].Render(cameraPos, "origin", (EnumFrustumCullMode)1);
            }

            val3.Stop();
        }

        ((ClientPlatformAbstract)_platform).GlDisableCullFace();
        val3 = _shaders[0];
        val3.Use();
        val3.UniformMatrix("projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
        val3.UniformMatrix("modelViewMatrix", _mod.CApi.Render.CurrentModelviewMatrix);
        val3.Uniform("dropletIntensity", num3);
        val3.Uniform("waterFlowCounter", ((ClientPlatformAbstract)_platform).ShaderUniforms.WaterFlowCounter);
        val3.Uniform("windSpeed", ((ClientPlatformAbstract)_platform).ShaderUniforms.WindSpeed);
        val3.Uniform("playerUnderwater", num);
        val3.Uniform("cameraWorldPosition", _mod.Uniforms.CameraWorldPosition);
        array2 = _chunkRenderer.poolsByRenderPass[4];
        for (int k = 0; k < array.Length; k++)
        {
            val3.BindTexture2D("terrainTex", array[k], 0);
            array2[k].Render(cameraPos, "origin", (EnumFrustumCullMode)1);
        }

        val3.Stop();
        ((ClientPlatformAbstract)_platform).GlEnableCullFace();
        val3 = _shaders[2];
        val3.Use();
        val3.UniformMatrix("projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
        val3.UniformMatrix("modelViewMatrix", _mod.CApi.Render.CurrentModelviewMatrix);
        val3.Uniform("playerUnderwater", num);
        array2 = _chunkRenderer.poolsByRenderPass[3];
        for (int l = 0; l < array.Length; l++)
        {
            val3.BindTexture2D("terrainTex", array[l], 0);
            array2[l].Render(cameraPos, "origin", (EnumFrustumCullMode)1);
        }

        val3.Stop();
        _game.GlPopMatrix();
        ((ClientPlatformAbstract)_platform).UnloadFrameBuffer(val);
        ((ClientPlatformAbstract)_platform).GlDepthMask(false);
        ((ClientPlatformAbstract)_platform).GlToggleBlend(true, (EnumBlendMode)0);
        ((ClientPlatformAbstract)_platform).CheckGlError("Error while rendering solid liquids");
    }

    public void OnSetFinalUniforms(ShaderProgramFinal final)
    {
        FrameBufferRef val = _framebuffers[1];
        FrameBufferRef val2 = _framebuffers[0];
        FrameBufferRef val3 = _framebuffers[2];
        if (_enabled && val != null)
        {
            ((ShaderProgramBase)final).BindTexture2D("ssrScene", val.ColorTextureIds[0]);
            if ((_refractionsEnabled || _causticsEnabled) && val2 != null)
            {
                ((ShaderProgramBase)final).UniformMatrix("projectionMatrix", _mod.CApi.Render.CurrentProjectionMatrix);
                ((ShaderProgramBase)final).BindTexture2D("gpositionScene", val2.ColorTextureIds[0]);
                ((ShaderProgramBase)final).BindTexture2D("gdepthScene",
                    ((ClientPlatformAbstract)_platform).FrameBuffers[0].DepthTextureId);
            }

            if (_refractionsEnabled && val2 != null)
            {
                ((ShaderProgramBase)final).BindTexture2D("refractionScene", val2.ColorTextureIds[3]);
            }

            if (_causticsEnabled && val3 != null)
            {
                ((ShaderProgramBase)final).BindTexture2D("causticsScene", val3.ColorTextureIds[0]);
            }
        }
    }

    public void Dispose()
    {
        ClientPlatformWindows clientPlatformWindows = _mod.CApi.GetClientPlatformWindows();
        for (int i = 0; i < _framebuffers.Length; i++)
        {
            if (_framebuffers[i] != null)
            {
                ((ClientPlatformAbstract)clientPlatformWindows).DisposeFrameBuffer(_framebuffers[i], true);
                _framebuffers[i] = null;
            }
        }

        for (int j = 0; j < _shaders.Length; j++)
        {
            ((IDisposable)_shaders[j])?.Dispose();
            _shaders[j] = null;
        }

        _chunkRenderer = null;
        _screenQuad = null;
    }
}