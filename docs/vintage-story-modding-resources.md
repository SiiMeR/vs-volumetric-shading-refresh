# Vintage Story Modding Resources

## Official Documentation

### Core Modding APIs
- **[Modding API Documentation](https://wiki.vintagestory.at/Category:Modding)** - Complete API reference
- **[Getting Started Guide](https://wiki.vintagestory.at/Modding:Getting_Started)** - Overview of modding capabilities and initial steps
- **[Asset System](https://wiki.vintagestory.at/Basic_Modding)** - How Vintage Story loads game content from asset JSONs
- **[Client API](https://apidocs.vintagestory.at/)** - Auto-generated API documentation
- **[Modding API Updates](https://wiki.vintagestory.at/Modding:Modding_API_Updates)** - Latest API changes and improvements

### Shader Development
- **[Modding Efficiently](https://wiki.vintagestory.at/Modding_Efficiently)** - Best practices and debugging
- **[JSON Patching](https://wiki.vintagestory.at/Modding:JSON_Patching)** - Comprehensive guide to patch testing and debugging

## Community Resources & Tools

### Essential Community Libraries
- **[Config Lib by Maltiez](https://mods.vintagestory.at/configlib)** - Add configuration files editable via GUI for user customization
- **[Achievements API by Nat](https://wiki.vintagestory.at/Modding:Community_Resources)** - Add achievements using simple JSON format
- **[Recipe Patcher by DanaCraluminum](https://wiki.vintagestory.at/Modding:Community_Resources)** - Patch existing recipes before resolving them
- **[PatchDebug by goxmeor](https://wiki.vintagestory.at/Modding:Community_Resources)** - Debug patches by showing file contents before/after applying patches

### Development Tools
- **[Modding Tools by Maltiez](https://mods.vintagestory.at/moddingtools)** - Collection of modding tools including:
  - Particle effects editor for editing and exporting particle effects to JSON
  - Various utilities for streamlining the modding process

## Key Concepts for Shader Mods

### Rendering Pipeline Overview

Vintage Story uses a forward rendering pipeline with these stages:

1. **EnumRenderStage.Opaque** - Solid objects and terrain
2. **EnumRenderStage.OIT** - Order-Independent Transparency 
3. **EnumRenderStage.AfterOIT** - Post-transparency effects
4. **EnumRenderStage.Ortho** - UI and overlays

### Framebuffer Management

```csharp
// Typical framebuffer setup
var framebuffer = new FrameBufferRef
{
    FboId = GL.GenFramebuffer(),
    Width = width,
    Height = height,
    ColorTextureIds = new[] { GL.GenTexture() },
    DepthTextureId = GL.GenTexture()
};

// Always check completeness
GL.BindFramebuffer(FramebufferTarget.Framebuffer, framebuffer.FboId);
framebuffer.SetupColorTexture(0);
framebuffer.SetupDepthTexture();
Framebuffers.CheckStatus(); // Critical for AMD compatibility!
```

### Modern Shader Program Registration

```csharp
// Modern approach using the established extension pattern
public static class ShaderExtensions
{
    public static IShaderProgram RegisterShader(this VolumetricShadingMod mod, string name, ref bool success)
    {
        var shader = (ShaderProgram)mod.CApi.Shader.NewShaderProgram();
        shader.AssetDomain = mod.Mod.Info.ModID; // Available since latest API updates
        mod.CApi.Shader.RegisterFileShaderProgram(name, shader);
        
        if (!shader.Compile())
        {
            success = false;
            mod.Mod.Logger.Error($"Failed to compile shader: {name}");
        }
        return shader;
    }
}

// Usage in mod
public IShaderProgram RegisterModernShader(string shaderName)
{
    bool success = true;
    var shader = this.RegisterShader(shaderName, ref success);
    return success ? shader : null;
}
```

### Alternative: Asset-Based Approach
```csharp
// Load shaders as assets instead of patches - more maintainable
public void RegisterShaderAssets()
{
    var shaderAssets = new[]
    {
        "overexposure", "blur_horizontal", "blur_vertical", 
        "deferred_geometry", "deferred_lighting", "softshadow"
    };
    
    foreach (var name in shaderAssets)
    {
        // Shaders loaded from assets/[domain]/shaders/
        bool success = true;
        RegisterShader(name, ref success);
        if (success)
        {
            Mod.Logger.Event($"Successfully registered asset-based shader: {name}");
        }
    }
}
```

## OpenGL Compatibility Guidelines

### Version Requirements
- **Minimum**: OpenGL 3.3 Core Profile
- **Recommended**: OpenGL 4.0+ for advanced features
- **Maximum**: 4.6 (for maximum compatibility)

### AMD-Specific Considerations

#### Precision Qualifiers
Always use explicit precision in fragment shaders:
```glsl
// Bad (implicit precision)
uniform float time;
varying vec2 texCoord;

// Good (explicit precision)  
uniform highp float time;
in highp vec2 texCoord;
```

#### Texture Formats
AMD drivers are strict about texture format compatibility:
```glsl
// Prefer widely supported formats
GL_RGBA8       // Instead of GL_RGBA32F when possible
GL_DEPTH24     // Instead of GL_DEPTH32F_STENCIL8
GL_R8          // For single-channel data
```

#### Loop Constructs
AMD drivers handle loops differently:
```glsl
// Bad (variable loop bounds)
for(int i = 0; i < dynamicCount; i++) {
    // operations
}

// Good (constant bounds with early exit)
const int MAX_SAMPLES = 16;
for(int i = 0; i < MAX_SAMPLES; i++) {
    if(i >= actualCount) break;
    // operations
}
```

## Performance Best Practices

### Shader Optimization

1. **Minimize Texture Samples**:
   ```glsl
   // Cache texture lookups
   vec4 albedo = texture(diffuseMap, uv);
   vec3 normal = texture(normalMap, uv).xyz;
   ```

2. **Use Appropriate Precision**:
   ```glsl
   // Use mediump for color calculations
   mediump vec3 lightColor = light.color * attenuation;
   
   // Use highp for positions and normals
   highp vec3 worldPos = modelMatrix * position;
   ```

3. **Avoid Complex Branching**:
   ```glsl
   // Instead of complex if/else
   vec3 result = mix(option1, option2, condition);
   ```

### Framebuffer Management

1. **Minimize State Changes**:
   - Batch operations by framebuffer
   - Avoid unnecessary binding/unbinding
   - Cache framebuffer references

2. **Proper Resource Cleanup**:
   ```csharp
   public override void Dispose()
   {
       // Always dispose in reverse order
       foreach(var fb in framebuffers.Reverse())
       {
           platform.DisposeFrameBuffer(fb);
       }
       
       foreach(var shader in shaders)
       {
           shader?.Dispose();
       }
   }
   ```

## Common Pitfalls & Solutions

### Issue: Shader Compilation Failures on AMD
**Solution**: Add explicit precision qualifiers and test compilation:
```csharp
private bool ValidateShader(int shaderId)
{
    GL.GetShader(shaderId, ShaderParameter.CompileStatus, out int status);
    if (status == 0)
    {
        string log = GL.GetShaderInfoLog(shaderId);
        Mod.Logger.Error($"Shader compilation failed: {log}");
        return false;
    }
    return true;
}
```

### Issue: Framebuffer Incomplete Errors
**Solution**: Always validate framebuffer completeness:
```csharp
public static void CheckFramebufferStatus()
{
    var status = GL.CheckFramebufferStatus(FramebufferTarget.Framebuffer);
    if (status != FramebufferErrorCode.FramebufferComplete)
    {
        throw new InvalidOperationException($"Framebuffer incomplete: {status}");
    }
}
```

### Issue: Performance Degradation
**Solution**: Profile GPU usage and implement fallbacks:
```csharp
// Performance monitoring
var startTime = GL.GetInteger64(GetPName.Timestamp);
// Render operations
var endTime = GL.GetInteger64(GetPName.Timestamp);
var gpuTime = (endTime - startTime) / 1000000.0; // Convert to ms

if (gpuTime > maxAllowedTime)
{
    // Reduce quality or disable features
}
```

## Testing Strategies

### Hardware Testing Matrix
1. **NVIDIA**: GTX 1060, RTX 3070, RTX 4080
2. **AMD**: RX 580, RX 6700 XT, RX 7800 XT  
3. **Intel**: Arc A750, Iris Xe integrated

### Driver Testing
- Test latest stable drivers
- Test one version back for each vendor
- Test beta drivers when available

### Resolution Testing
- 1080p, 1440p, 4K resolutions
- Windowed and fullscreen modes
- Different aspect ratios (16:9, 21:9, 16:10)

## Community Resources & Learning

### Forums & Communities
- **[Vintage Story Forums](https://www.vintagestory.at/forums/)** - Official community
- **[Modding Discord](https://discord.gg/vintagestory)** - Real-time help
- **[GitHub Discussions](https://github.com/anegostudios/vsessentialsmod)** - Technical discussions

### Code Examples & Repositories
- **[Nat's Mod Examples](https://github.com/anegostudios/vsmodexamples)** - GitHub repository of mod examples by Nat, maintained and kept up-to-date with current game version
- **[Essentials Mod](https://github.com/anegostudios/vsessentialsmod)** - Official examples and patterns
- **[VintagestoryAPI](https://github.com/anegostudios)** - Official GitHub repository for API examples

### Development & Debugging Tools
- **[Asset Viewer](https://wiki.vintagestory.at/Modding:Asset_System)** - Debug asset loading
- **[PatchDebug](https://wiki.vintagestory.at/Modding:Community_Resources)** - Display contents of affected files before and after applying patches
- **[ShaderToy](https://www.shadertoy.com/)** - Prototype fragment shaders
- **[RenderDoc](https://renderdoc.org/)** - GPU debugging tool

## Reducing Patch Dependency

### Modern API-First Approaches

#### 1. Use Official Renderer System
```csharp
// Instead of patching shaders, implement IRenderer
public class ModernEffectRenderer : IRenderer
{
    public double RenderOrder => 0.5;
    public int RenderRange => 1;
    
    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        // Custom rendering logic using official APIs
        // No string manipulation or shader patching required
    }
}

// Register with official API
capi.Event.RegisterRenderer(new ModernEffectRenderer(), EnumRenderStage.AfterOIT, "myeffect");
```

#### 2. Asset-Based Configuration
```csharp
// Instead of hardcoded patches, use JSON assets
public class ShaderConfig
{
    public Dictionary<string, ShaderSettings> Effects { get; set; }
}

// Load from assets/[domain]/config/shaders.json
var config = capi.Assets.Get<ShaderConfig>(new AssetLocation(domain, "config/shaders.json"));
```

#### 3. ModSystem Integration Points
```csharp
public class ModernVolumetricMod : ModSystem
{
    // New API method for conditional loading
    public override bool ShouldLoad(ICoreAPI api)
    {
        // Check compatibility, GPU capabilities, etc.
        return base.ShouldLoad(api);
    }
    
    public override void StartClientSide(ICoreClientAPI api)
    {
        // Use official registration methods
        RegisterModernRenderers(api);
        RegisterShaderAssets(api);
        SetupUniformProviders(api);
    }
}
```

### Debugging Modern Mods

#### Enhanced Error Handling
```csharp
// Modern error handling with detailed logging
private bool TryRegisterShader(string name, out IShaderProgram shader)
{
    shader = null;
    try
    {
        bool success = true;
        shader = this.RegisterShader(name, ref success);
        
        if (!success)
        {
            Mod.Logger.Error($"Shader compilation failed for '{name}'. Check shader syntax and GPU compatibility.");
            return false;
        }
        
        Mod.Logger.Event($"Successfully registered shader: {name}");
        return true;
    }
    catch (Exception ex)
    {
        Mod.Logger.Error($"Exception registering shader '{name}': {ex}");
        return false;
    }
}
```

#### Graceful Degradation
```csharp
// Implement fallbacks instead of crashes
private void SetupEffects()
{
    var effects = new List<IRenderer>();
    
    // Try modern approach first
    if (TryRegisterShader("advanced_lighting", out var modernShader))
    {
        effects.Add(new AdvancedLightingRenderer(modernShader));
    }
    else
    {
        // Fallback to simpler implementation
        Mod.Logger.Warning("Using fallback lighting - advanced features disabled");
        effects.Add(new BasicLightingRenderer());
    }
}
```

## Advanced Techniques

### Temporal Accumulation
```glsl
// Temporal anti-aliasing pattern
vec4 currentColor = texture(currentFrame, uv);
vec4 historyColor = texture(historyFrame, reprojectUV);

// Velocity-based rejection
vec2 velocity = texture(velocityBuffer, uv).xy;
float velocityLength = length(velocity);

// Blend based on motion
float blend = mix(0.95, 0.1, saturate(velocityLength * 10.0));
vec4 result = mix(historyColor, currentColor, blend);
```

### Multi-Scale Rendering
```csharp
// Render at different scales for performance
var halfRes = new Vector2i(width / 2, height / 2);
var quarterRes = new Vector2i(width / 4, height / 4);

// Render expensive effects at lower resolution
RenderVolumetricLighting(quarterRes);
RenderScreenSpaceReflections(halfRes);

// Upscale with proper filtering
UpscaleToFullResolution(effect, fullRes);
```

## Debugging Tips

### Modern Debugging Approaches

#### Enable Debug Output
```csharp
// Enable OpenGL debug output with enhanced logging
if (Debug)
{
    GL.Enable(EnableCap.DebugOutput);
    GL.DebugMessageCallback((source, type, id, severity, length, message, userParam) =>
    {
        string severityStr = severity switch
        {
            DebugSeverity.DebugSeverityHigh => "ERROR",
            DebugSeverity.DebugSeverityMedium => "WARNING", 
            DebugSeverity.DebugSeverityLow => "INFO",
            _ => "DEBUG"
        };
        
        Mod.Logger.Debug($"OpenGL {severityStr}: {message}");
        
        if (severity == DebugSeverity.DebugSeverityHigh)
        {
            // Log to file for post-mortem analysis
            File.AppendAllText("gpu_errors.log", $"{DateTime.Now}: {message}\n");
        }
    }, IntPtr.Zero);
}
```

#### Patch Debugging with PatchDebug Tool
```csharp
// Instead of guessing why patches fail, use the community PatchDebug tool
// It shows file contents before and after patches are applied
// Available from: https://wiki.vintagestory.at/Modding:Community_Resources

private void ValidatePatches()
{
    // Enable detailed patch logging
    Mod.Logger.Event("Validating shader patches...");
    
    // Use optional patches to prevent crashes on version mismatches
    var criticalPatches = new[] { "essential_lighting", "core_shadows" };
    var optionalPatches = new[] { "enhanced_reflections", "advanced_fog" };
    
    foreach (var patch in optionalPatches)
    {
        // These won't crash if patterns change
        Mod.Logger.Debug($"Attempting optional patch: {patch}");
    }
}
```

### Shader Debugging
```glsl
// Debug output in shaders
#ifdef DEBUG
    if (gl_FragCoord.x < 100.0 && gl_FragCoord.y < 100.0)
    {
        outColor = vec4(debugValue, 0, 0, 1); // Red for debug values
        return;
    }
#endif
```

### Performance Profiling
```csharp
// GPU timer queries
private uint[] timerQueries = new uint[2];

private void BeginGPUTimer()
{
    GL.BeginQuery(QueryTarget.TimeElapsed, timerQueries[0]);
}

private void EndGPUTimer()
{
    GL.EndQuery(QueryTarget.TimeElapsed);
    GL.GetQueryObject(timerQueries[0], GetQueryObjectParam.QueryResult, out long elapsed);
    var milliseconds = elapsed / 1000000.0; // Convert to ms
}
```

## Future-Proofing Strategies

### API Evolution Preparation
```csharp
// Design for API changes using abstraction
public interface IShaderManager 
{
    IShaderProgram RegisterShader(string name);
    void UpdateUniforms(IShaderProgram shader, object uniformData);
    void DisposeShader(IShaderProgram shader);
}

// Implement current API, easily swappable when APIs change
public class VintageStoryShaderManager : IShaderManager
{
    public IShaderProgram RegisterShader(string name)
    {
        bool success = true;
        return _mod.RegisterShader(name, ref success);
    }
}
```

### Version Compatibility
```csharp
public override bool ShouldLoad(ICoreAPI api)
{
    // Check game version compatibility
    var gameVersion = api.Version;
    var supportedVersions = new[] { "1.21.0", "1.21.1", "1.22.0" };
    
    if (!supportedVersions.Any(v => gameVersion.StartsWith(v)))
    {
        Logger.Warning($"Untested game version: {gameVersion}. Mod may not work correctly.");
    }
    
    return base.ShouldLoad(api);
}
```

### Asset-Driven Configuration
```json
// assets/[domain]/config/effects.json
{
    "volumetricLighting": {
        "enabled": true,
        "quality": "high",
        "fallbackShader": "basic_volumetric",
        "gpuRequirements": {
            "minimumVram": 2048,
            "requiredExtensions": ["GL_ARB_shader_storage_buffer_object"]
        }
    },
    "screenSpaceReflections": {
        "enabled": true,
        "adaptiveQuality": true,
        "fallbacks": ["planar", "cubemap", "disabled"]
    }
}
```

## Latest API Features to Leverage

### Recently Added Capabilities
- **IShaderProgram.AssetDomain** - Now available for proper shader organization
- **UDP Channels** - Use `api.Network.RegisterUdpChannel()` for real-time data
- **Enhanced Error Handling** - Server-side events are more robust against mod crashes
- **Animation Improvements** - Better support for custom animations and effects
- **ModSystem.ShouldLoad()** - Conditional mod loading based on environment

### Recommended Migration Path
1. **Phase 1**: Replace critical patches with IRenderer implementations ✅
2. **Phase 2**: Convert remaining patches to optional with fallbacks ✅
3. **Phase 3**: Implement asset-based configuration system
4. **Phase 4**: Add community tool integrations (Config Lib, etc.)
5. **Phase 5**: Develop automated testing with different VS versions

This resource document provides the foundation for developing high-quality, future-proof shader mods for Vintage Story that minimize patching and work reliably across all hardware configurations and game versions.
