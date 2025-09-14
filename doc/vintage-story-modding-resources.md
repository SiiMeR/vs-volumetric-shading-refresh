# Vintage Story Modding Resources

## Official Documentation

### Core Modding APIs
- **[Modding API Documentation](https://wiki.vintagestory.at/Category:Modding)** - Complete API reference
- **[Render Stages](https://wiki.vintagestory.at/Modding:Render_Stages)** - Understanding the rendering pipeline
- **[Matrix Operations](https://wiki.vintagestory.at/Modding:Matrix_Operations)** - 3D math for shaders
- **[Client API](https://apidocs.vintagestory.at/)** - Auto-generated API documentation

### Shader Development
- **[Rendering API](https://wiki.vintagestory.at/Rendering_API)** - Low-level rendering functions
- **[Modding Efficiently](https://wiki.vintagestory.at/Modding_Efficiently)** - Best practices and debugging
- **[Shader Integration](https://wiki.vintagestory.at/Modding:Shaders)** - How to add custom shaders

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

### Shader Program Registration

```csharp
// Register custom shaders through the mod
public IShaderProgram RegisterShader(string name, ref bool success)
{
    return CApi.Shader.NewShaderProgram()
        .WithName(name)
        .WithVertexShader(CApi.Assets.Get(new AssetLocation(domain, $"shaders/{name}.vsh")))
        .WithFragmentShader(CApi.Assets.Get(new AssetLocation(domain, $"shaders/{name}.fsh")))
        .WithUniformProvider(() => {
            // Provide uniforms here
        })
        .Compile();
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

## Community Resources

### Forums & Communities
- **[Vintage Story Forums](https://www.vintagestory.at/forums/)** - Official community
- **[Modding Discord](https://discord.gg/vintagestory)** - Real-time help
- **[GitHub Discussions](https://github.com/anegostudios/vsessentialsmod)** - Technical discussions

### Example Mods
- **[Essentials Mod](https://github.com/anegostudios/vsessentialsmod)** - Official examples
- **[Player Physics](https://mods.vintagestory.at/show/mod/1672)** - Physics integration
- **[Better Drifters](https://mods.vintagestory.at/show/mod/2066)** - Entity modification

### Tools & Utilities
- **[Asset Viewer](https://wiki.vintagestory.at/Modding:Asset_System)** - Debug asset loading
- **[ShaderToy](https://www.shadertoy.com/)** - Prototype fragment shaders
- **[RenderDoc](https://renderdoc.org/)** - GPU debugging tool

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

### Enable Debug Output
```csharp
// Enable OpenGL debug output
if (Debug)
{
    GL.Enable(EnableCap.DebugOutput);
    GL.DebugMessageCallback((source, type, id, severity, length, message, userParam) =>
    {
        if (severity == DebugSeverity.DebugSeverityHigh)
        {
            Mod.Logger.Error($"OpenGL Error: {message}");
        }
    }, IntPtr.Zero);
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

This resource document provides the foundation for developing high-quality shader mods for Vintage Story that work reliably across all hardware configurations.
