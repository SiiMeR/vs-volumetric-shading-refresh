# VolumetricShadingRefreshed Integration Testing Guide

This guide outlines the steps to test the modernized VolumetricShadingRefreshed mod to ensure all effects work correctly with the new architecture.

## Testing Setup

### Hardware Testing Matrix
Test on these GPU configurations if possible:
1. **NVIDIA**: GTX 1060 or newer
2. **AMD**: RX 580 or newer
3. **Intel**: Iris Xe integrated or Arc discrete GPU

### Software Requirements
- Vintage Story 1.21.0 or newer
- .NET 8.0 runtime
- Development environment with Visual Studio or VS Code

## Build Instructions

1. **Compile the mod**
   ```bash
   # Windows
   ./build.ps1
   
   # Linux
   ./build.sh
   ```

2. **Install the mod**
   - Copy `VolumetricShadingRefreshed.dll` and all assets to your VS mods folder
   - Or use the `Releases/volumetricshadingrefreshed` folder directly

## Testing Methodology

### 1. Base Functionality

Test each effect individually to ensure the modernized implementation matches the original:

#### Overexposure
1. Enable overexposure and set intensity to 50%
2. Look at the sun through trees
3. Verify bloom effect around bright areas
4. Confirm performance is stable

#### Blur
1. Enable bloom quality settings (in-game settings)
2. Test with different quality levels
3. Verify blur effect on bright areas
4. Check performance on low-end hardware

#### Soft Shadows
1. Enable soft shadows
2. Compare shadow edges to vanilla
3. Test different sample counts
4. Verify AMD compatibility

#### Deferred Lighting
1. Enable deferred lighting
2. Verify correct appearance of lit surfaces
3. Test with different light sources
4. Check for compatibility issues on AMD/Intel

### 2. Core Integration Tests

Test areas where shader patches integrate with core game systems:

#### Water Integration
1. Test SSR water transparency
2. Check underwater view with effects enabled
3. Verify caustics and reflections
4. Look for graphical artifacts

#### Shadow Integration
1. Test shadow bias adjustments
2. Verify no shadow acne or peter panning
3. Check far cascade transitions
4. Test in different biomes and times of day

### 3. Performance Testing

Track performance metrics to ensure the new implementation is efficient:

#### Frame Time Analysis
1. Enable debug mode (`VOLUMETRICSHADING_DEBUG=true`)
2. Monitor frame times in different scenarios
3. Compare with original implementation
4. Check CPU usage for reduced string processing overhead

#### GPU Vendor Testing
1. Test on AMD hardware for shader compatibility
2. Test on Intel hardware for feature support
3. Verify vendor-specific optimizations activate correctly

## Common Issues & Fixes

### Shader Compilation Errors
- Check debug output for compilation failures
- Verify precision qualifiers are explicitly specified
- Review shader compatibility with older OpenGL versions

### AMD-Specific Issues
- Ensure explicit highp/mediump qualifiers on all variables
- Check for loop constructs with dynamic bounds
- Verify framebuffer formats are widely supported

### Intel-Specific Issues
- Test with deferred lighting disabled
- Check for uniform buffer size limits
- Monitor for shader precision issues

## Reporting Results

When reporting testing results, include:
1. Hardware configuration (GPU, RAM, CPU)
2. Driver version
3. Operating system
4. Vintage Story version
5. Specific test scenarios and results
6. Screenshots of any issues encountered

Submit testing feedback as GitHub issues with the "testing" label.

## Debug Commands

The mod provides several debug commands to help with testing:

- `/vs_debug fps` - Show detailed performance metrics
- `/vs_debug shaders` - Dump shader info to logs
- `/vs_debug vendor` - Show detected GPU info
- `/vs_debug reset` - Reset all settings to defaults

## Known Issues

1. **Volumetric lighting patches** - Still requires some core shader patching
2. **Shadow integration** - Simplified but not fully modernized yet
3. **Intel GPUs** - Deferred lighting may have compatibility issues

Thank you for helping test this modernized implementation!