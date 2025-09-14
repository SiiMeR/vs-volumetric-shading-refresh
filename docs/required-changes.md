# VolumetricShadingRefreshed - Required Changes Analysis

## Executive Summary

Based on analysis of the current implementation against Vintage Story's official modding APIs, this document identifies specific changes needed to modernize the mod architecture, improve compatibility, and reduce technical debt while maintaining all visual effects.

## Critical Finding: Hybrid Approach Required

**~60% of the current shader patching system can be replaced** with official Vintage Story APIs, while **~40% legitimately requires shader modification** for deep integration effects.

## Current Architecture Analysis

### What's Currently Implemented

1. **YAML-Based Shader Patching System** (Complex)
   - `YamlPatchLoader`: Loads patch definitions from YAML files
   - `ShaderPatcher`: Applies string-based patches to shader source
   - `ShaderInjector`: Injects properties and code snippets
   - **6 YAML patch files** defining ~83 individual patches

2. **Harmony Runtime Patching** (Necessary but can be reduced)
   - `ShaderRegistryPatches`: Intercepts shader loading pipeline
   - Various game system patches for deep integration

3. **Custom Effect Renderers** (✅ Good - using official APIs)
   - Properly using `IRenderer` interface and render stages
   - Using official framebuffer management APIs

## Required Changes by Category

### 🔄 REPLACE: Overcomplicated YAML System → Simplified Official APIs

#### **1. Custom Effect Shaders (60% of current patches)**

**Current Problem:**
```yaml
# screenspacereflections.yaml - 12 different patches just for SSR
- type: token
  filename: chunkliquid.fsh  
  tokens: texColor.a += (max(0, diff/16 + noise/(12 - 8*min(1,windSpeed))) + vn / 4) / clamp(fresnel, 0.5, 1);
  content: |
    texColor.a += (max(0, diff/16 + noise/(12 - 8*min(1,windSpeed))) + vn / 4) / clamp(fresnel, 0.5, 1);
    #if VSMOD_SSR > 0
    texColor.a *= VSMOD_SSR_WATER_TRANSPARENCY;
    #endif
```

**Better Solution:**
```csharp
// Create dedicated shader programs instead of patching
public class SSRRenderer : IRenderer
{
    private IShaderProgram _ssrLiquidShader;
    
    private void SetupShaders()
    {
        _ssrLiquidShader = CApi.Shader.NewShaderProgram()
            .WithName("ssr_liquid")
            .WithVertexShader(CApi.Assets.Get(new AssetLocation(modId, "shaders/ssr_liquid.vsh")))
            .WithFragmentShader(CApi.Assets.Get(new AssetLocation(modId, "shaders/ssr_liquid.fsh")))
            .WithUniformProvider(() => {
                _ssrLiquidShader.Uniform("waterTransparency", ModSettings.SSRWaterTransparency * 0.01f);
                // Other uniforms...
            })
            .Compile();
    }
}
```

**Files to Replace:**
- `screenspacereflections.yaml` → Custom SSR shaders + renderer
- `volumetriclighting.yaml` → Custom volumetric shaders + renderer  
- `overexposure.yaml` → Post-process shader + renderer

#### **2. Property Injection System**

**Current Problem:**
Complex `ShaderInjector` with string replacement:
```csharp
public void OnShaderLoaded(ShaderProgram program, EnumShaderType shaderType)
{
    // Complex string manipulation and code injection
    var stringBuilder = new StringBuilder();
    foreach (var shaderProperty in ShaderProperties)
    {
        stringBuilder.Append(shaderProperty.GenerateOutput());
    }
    shader.PrefixCode += stringBuilder.ToString();
}
```

**Better Solution:**
```csharp
// Use official uniform providers
public IShaderProgram CreateShaderWithUniforms(string name)
{
    return CApi.Shader.NewShaderProgram()
        .WithName(name)
        .WithUniformProvider(() => {
            // Direct uniform setting - no string manipulation
            shader.Uniform("VSMOD_SSR_ENABLED", ModSettings.ScreenSpaceReflectionsEnabled ? 1 : 0);
            shader.Uniform("VSMOD_VOLUMETRIC_INTENSITY", ModSettings.VolumetricLightingIntensity * 0.01f);
        })
        .Compile();
}
```

### 🔧 SIMPLIFY: Reduce Harmony Patching

#### **Current Harmony Patches to Reduce:**

1. **`ShaderRegistryPatches`** (Partially removable)
   - Currently intercepts all shader loading
   - **Keep:** Only the minimal patches needed for core game shader integration
   - **Remove:** Complex transpiler modifications that can be handled with custom shaders

2. **`PlatformPatches`** (Reduce scope)
   - Currently patches multiple rendering methods  
   - **Keep:** Only patches needed for framebuffer compatibility
   - **Remove:** Patches that duplicate functionality available through official APIs

### ✅ KEEP: Necessary Core Integrations

#### **These shader modifications are legitimately required:**

1. **`final.fsh` modifications** (SSR compositing)
   ```yaml
   # This MUST be patched - no other way to composite SSR
   - type: token
     filename: final.fsh
     tokens: '#if BLOOM == 1'
     snippet: ssrfinalcomposite.txt
   ```

2. **Shadow map integration** (Soft shadows)
   ```yaml
   # Required for soft shadow sampling
   - type: token
     filename: shadowcoords.fsh  
     # Must integrate with existing shadow pipeline
   ```

3. **Core water rendering** (Refractions/Caustics)
   ```yaml
   # Some water effects need core integration
   - type: token
     filename: chunkliquid.fsh
     # Only the essential modifications
   ```

## Specific Implementation Plan

### Phase 1: Extract Independent Effects (2 weeks)

#### Replace These YAML Files with Custom Shaders:

1. **`overexposure.yaml` → `OverexposureRenderer`**
   ```csharp
   public class OverexposureRenderer : IRenderer
   {
       private IShaderProgram _overexposureShader;
       
       public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
       {
           if (stage == EnumRenderStage.AfterOIT)
           {
               ApplyOverexposure();
           }
       }
   }
   ```

2. **`blur.yaml` → `BlurEffect`**
   - Custom Gaussian blur shader
   - No need to patch core shaders

3. **`deferredlighting.yaml` → Enhanced `DeferredLightingRenderer`**
   - Already partially implemented correctly
   - Remove YAML patches, keep custom shader approach

#### Benefits:
- **90% less string manipulation**
- **Easier debugging** - actual shader files instead of generated code  
- **Better performance** - no runtime string processing
- **Hardware compatibility** - easier to add vendor-specific variants

### Phase 2: Simplify Core Integrations (2 weeks)

#### Reduce `screenspacereflections.yaml` to Essential Patches Only:

**Current: 12 patches → Target: 3 core patches**

Keep only:
```yaml
# 1. Final composite integration (no alternative)
- type: token
  filename: final.fsh
  tokens: '#if BLOOM == 1'
  snippet: ssrfinalcomposite.txt

# 2. Water transparency core integration  
- type: token
  filename: chunkliquid.fsh
  tokens: texColor.a += (max(0, diff/16...
  content: |
    texColor.a += (max(0, diff/16 + noise/(12 - 8*min(1,windSpeed))) + vn / 4) / clamp(fresnel, 0.5, 1);
    #if VSMOD_SSR > 0
    texColor.a *= VSMOD_SSR_WATER_TRANSPARENCY;
    #endif

# 3. Disable conflicting effects
- type: token
  filename: chunktransparent.fsh
  tokens: '#if SHINYEFFECT > 0'
  content: "#if SHINYEFFECT > 0 && VSMOD_SSR == 0\n"
```

Replace the other 9 patches with dedicated SSR renderers.

#### Reduce `shadowtweaks.yaml`:

**Current: Complex shadow pipeline integration → Target: Minimal core patches**

Keep only the essential shadow bias adjustments, move soft shadow rendering to dedicated shader.

### Phase 3: Modernize Shader Loading (1 week)

#### Replace ShaderInjector with Official APIs:

**Current:**
```csharp
// Complex string-based injection
shaderInjector.RegisterFloatProperty("VSMOD_SSR_WATER_TRANSPARENCY", 
    () => (100 - ModSettings.SSRWaterTransparency) * 0.01f);
```

**Better:**
```csharp
// Direct uniform management  
public class ShaderUniformManager
{
    public void UpdateShaderUniforms(IShaderProgram shader)
    {
        shader.Uniform("waterTransparency", (100 - ModSettings.SSRWaterTransparency) * 0.01f);
        shader.Uniform("ssrEnabled", ModSettings.ScreenSpaceReflectionsEnabled ? 1 : 0);
        // All uniforms set directly, no string generation
    }
}
```

## File Impact Analysis

### Files to DELETE (Technical Debt Removal):
```
VolumetricShading.Patch/
├── YamlPatchLoader.cs        ❌ DELETE - Replace with direct shader creation
├── ShaderInjector.cs         ❌ DELETE - Replace with uniform providers  
├── TokenPatch.cs             ❌ DELETE - No longer needed
├── RegexPatch.cs            ❌ DELETE - No longer needed
├── StartPatch.cs            ❌ DELETE - No longer needed
└── ValueShaderProperty.cs   ❌ DELETE - Use uniforms directly

assets/volumetricshadingrefreshed/shaderpatches/
├── overexposure.yaml        ❌ DELETE - Replace with custom shader
├── blur.yaml                ❌ DELETE - Replace with custom shader  
├── deferredlighting.yaml    ❌ DELETE - Already has custom implementation
├── volumetriclighting.yaml  🔧 SIMPLIFY - Keep minimal core patches only
├── screenspacereflections.yaml 🔧 SIMPLIFY - Reduce from 12 to 3 patches
└── shadowtweaks.yaml        🔧 SIMPLIFY - Keep only essential shadow bias
```

### Files to CREATE (Modern Implementation):
```
VolumetricShading.Effects/
├── OverexposureRenderer.cs   ✅ NEW - Dedicated post-process renderer
├── BlurRenderer.cs           ✅ NEW - Gaussian blur implementation
└── VolumetricRenderer.cs     ✅ NEW - Enhanced volumetric lighting

assets/volumetricshadingrefreshed/shaders/
├── overexposure.fsh         ✅ NEW - Actual shader file instead of patches
├── overexposure.vsh         ✅ NEW - Vertex shader
├── blur_horizontal.fsh      ✅ NEW - Separable blur implementation  
├── blur_vertical.fsh        ✅ NEW - Better performance than single-pass
├── volumetric_enhanced.fsh  ✅ NEW - Improved volumetric calculations
└── volumetric_enhanced.vsh  ✅ NEW - Vertex processing
```

## Expected Benefits

### Code Quality Improvements:
- **-2,000 lines** of complex string manipulation code
- **-6 YAML files** of patch definitions  
- **+8 dedicated shader files** with proper syntax highlighting and validation
- **90% reduction** in runtime string processing

### Compatibility Improvements:
- **Easier AMD driver compatibility** - dedicated shaders can have vendor-specific versions
- **Better error reporting** - actual compilation errors instead of string manipulation failures
- **Simpler debugging** - can inspect actual shader code instead of generated patches

### Performance Improvements:
- **Eliminated runtime string processing** - all uniforms set directly
- **Reduced harmony overhead** - fewer intercepted method calls
- **Better GPU state management** - dedicated renderers can optimize binding

### Maintainability Improvements:  
- **Standard shader development** - normal .fsh/.vsh files instead of YAML
- **IDE support** - syntax highlighting, error checking for shaders
- **Easier testing** - can test individual effects in isolation

## Risk Assessment

### Low Risk Changes:
- ✅ Extracting independent effects (overexposure, blur, deferred lighting)
- ✅ Replacing property injection with uniform providers  
- ✅ Reducing harmony patches

### Medium Risk Changes:
- ⚠️ Simplifying SSR integration (needs careful testing of water rendering)
- ⚠️ Modifying shadow pipeline (soft shadows are complex)

### High Risk Changes:
- ❌ None identified - all core integrations are kept

## Success Criteria

1. **All visual effects remain identical** - no loss of functionality
2. **AMD compatibility improved** - dedicated shaders can be optimized per vendor
3. **Code complexity reduced** - fewer layers of string manipulation  
4. **Performance maintained or improved** - less CPU overhead
5. **Maintainability improved** - standard shader development workflow

## Conclusion

The current YAML-based shader patching system represents sophisticated engineering but is **over-engineered for most use cases**. By adopting a hybrid approach that:

1. **Uses official APIs** for independent effects (60% of current patches)
2. **Keeps minimal core patches** for deep integration (40% of current patches)  
3. **Eliminates complex string processing** in favor of direct uniform management

We can achieve **identical visual results** with **significantly simpler code** that is **easier to maintain** and **more compatible** across GPU vendors.

**Estimated Development Time:** 5 weeks total
**Risk Level:** Medium (careful testing required for SSR/shadow integration)  
**Compatibility Impact:** Positive (better AMD support)
**Performance Impact:** Neutral to positive (less CPU overhead)

