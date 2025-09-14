# Volumetric Shading Refreshed - Analysis & Issues

## Overview

The **Volumetric Shading Refreshed** mod for Vintage Story is an advanced shader modification that adds sophisticated visual effects including:

- **Screen Space Reflections (SSR)** - Real-time water reflections, refractions, and caustics
- **Volumetric Lighting** - Enhanced god rays and atmospheric scattering
- **Shadow Improvements** - Soft shadows with PCSS (Percentage Closer Soft Shadows)
- **Deferred Lighting** - Alternative lighting pipeline for better performance
- **Overexposure Effects** - HDR-like bloom and exposure adjustments
- **Underwater Enhancements** - Improved underwater visuals

## Current Issues

### 1. AMD Graphics Card Compatibility Problems

**Root Causes Identified:**

- **OpenGL Driver Differences**: AMD's OpenGL drivers handle certain shader operations differently than NVIDIA drivers, particularly:
  - Complex fragment shader operations with multiple render targets
  - Advanced texture sampling (shadow maps, depth textures)
  - Float precision handling in calculations
  - Memory layout for framebuffer objects

- **Shader Compilation Issues**: AMD drivers are more strict with GLSL standards:
  - Implicit type conversions may fail
  - Precision qualifiers may be required
  - Loop unrolling behavior differs
  - Texture format compatibility varies

- **Performance Bottlenecks**: 
  - Multiple render passes causing GPU stalls
  - Excessive shader switching
  - Framebuffer binding/unbinding overhead

### 2. Non-Functional Configuration Options

**Issues Found:**

- **Deferred Lighting**: Currently disabled by default (`ModSettings.DeferredLightingEnabled = false`)
- **Some Shadow Tweaks**: Peter Panning adjustments may not apply correctly
- **SSDO (Screen Space Directional Occlusion)**: Implementation appears incomplete
- **Overexposure Settings**: Some intensity sliders don't provide visual feedback

**Root Causes:**

- Settings not properly connected to shader uniforms
- Missing shader recompilation after settings change
- Conditional compilation branches that bypass setting effects
- Default values overriding user settings

### 3. Fullscreen Mode Dependency

**Issues:**

- Mod only works correctly in fullscreen mode
- Initial blurriness requiring F11 toggle
- Framebuffer sizing problems in windowed modes

**Root Causes:**

- Hardcoded framebuffer dimensions based on fullscreen resolution
- SSAA (Super Sampling Anti-Aliasing) calculations incorrect for windowed modes
- Window resize events not properly handled

### 4. Error Logs and Stability Issues

**Common Problems:**

- Shader compilation failures on certain hardware
- Framebuffer incomplete errors
- Texture binding issues
- Memory leaks from undisposed resources

## Technical Analysis

### Shader Architecture

The mod uses a sophisticated patching system:

1. **YAML Patch Loader**: Loads shader patches from YAML configuration files
2. **Shader Injector**: Dynamically injects code and uniforms into existing shaders
3. **Shader Patcher**: Applies text-based patches to shader source code

### Problematic Patterns Found

1. **Complex Fragment Shader Operations**:
   - Multiple render targets (MRT) with 3-4 color attachments
   - Heavy texture sampling in loops
   - Complex mathematical operations without precision qualifiers

2. **Framebuffer Management**:
   - Dynamic framebuffer creation without proper validation
   - Mixed texture formats that may not be compatible on all GPUs
   - No fallback mechanisms for unsupported features

3. **Resource Management**:
   - Some resources not properly disposed
   - Shader programs recompiled unnecessarily
   - Memory allocations in render loops

## Compatibility Matrix

| Feature | NVIDIA | AMD | Intel |
|---------|--------|-----|-------|
| SSR | ✅ Works | ❌ Issues | ❓ Untested |
| Volumetric Lighting | ✅ Works | ⚠️ Performance | ❓ Untested |
| Soft Shadows | ✅ Works | ❌ Compilation | ❓ Untested |
| Deferred Lighting | ⚠️ Disabled | ❌ Broken | ❓ Untested |
| Caustics | ✅ Works | ❌ Artifacts | ❓ Untested |

## Current Workarounds

1. **For AMD Users**:
   - Use fullscreen mode only
   - Disable problematic features (SSR, Caustics)
   - Toggle F11 when experiencing blurriness
   - Update to latest AMD drivers

2. **For All Users**:
   - Avoid adjusting non-functional settings
   - Monitor performance impact
   - Report specific errors with hardware information

## Architecture Strengths

Despite the issues, the mod demonstrates good architectural patterns:

- **Modular Design**: Separate effects that can be enabled/disabled independently
- **Configuration System**: Comprehensive settings management
- **Shader Patching**: Flexible system for modifying existing shaders
- **Event-Driven**: Proper integration with Vintage Story's rendering pipeline

## Hardware Requirements

**Minimum (for basic functionality)**:
- DirectX 11 compatible graphics card
- OpenGL 3.3 support
- 2GB VRAM

**Recommended (for full features)**:
- NVIDIA GTX 1060 / AMD RX 580 or better
- OpenGL 4.3 support  
- 4GB VRAM
- Current graphics drivers

**Note**: AMD cards may require specific driver versions or settings adjustments.
