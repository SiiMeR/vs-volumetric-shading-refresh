# VolumetricShadingRefreshed

A modernized shader mod for Vintage Story that adds volumetric lighting, screen space reflections, deferred lighting, and other visual enhancements.

## Major Features

- **Volumetric Lighting** - Realistic light shafts and god rays
- **Screen Space Reflections (SSR)** - Accurate reflections on water and other surfaces
- **Soft Shadows** - Percentage Closer Soft Shadows (PCSS) implementation
- **Overexposure/Bloom** - HDR-like lighting with sun bloom effects
- **Deferred Lighting** - PBR-based deferred rendering pipeline

## Technical Improvements

This fork includes significant technical improvements to make the mod more robust and maintainable:

1. **Reduced YAML Patch Dependencies** - Most shader patches have been replaced with dedicated shader implementations using official APIs.
2. **Improved Error Handling** - Non-critical errors no longer crash the game.
3. **Modern Shader Management** - Using a proper shader management system instead of string manipulation.
4. **Hardware Compatibility** - Better support for AMD, Intel, and NVIDIA GPUs.
5. **Performance Optimization** - Automatic quality adjustments based on hardware capabilities.

## Installation

1. Download the mod from [Vintage Story Mod DB](https://mods.vintagestory.at/) or the Releases page
2. Place the ZIP file in your `Mods` folder
3. Launch the game and configure settings to your liking

## Configuration

The mod adds a configuration screen accessible by pressing `Ctrl+C` in-game. This allows you to adjust:

- Volumetric Lighting quality and intensity
- Screen Space Reflection settings
- Shadow quality and softness
- Overexposure/bloom intensity
- Performance options

## Troubleshooting

If you experience crashes or performance issues:

1. **Performance Issues**
   - Lower your settings in the configuration menu
   - Disable features one-by-one to identify the problematic effect

2. **Graphic Glitches**
   - Update your GPU drivers
   - Try disabling specific effects that show artifacts

3. **Crashes**
   - Check `client-crash.log` for details
   - Try disabling the mod and re-enabling only specific features

## Known Issues

- **Some YAML patches may fail** - This is expected with newer game versions and shouldn't cause crashes
- **Shader compilation errors** - Some advanced features may not work on older hardware
- **Performance impact** - The mod automatically adjusts quality based on performance

## Credits

- Original VolumetricShading mod by Xepos
- Modernized by daviaaze
- Thanks to the Vintage Story community for testing and feedback

## License

This mod is provided under the MIT license. See LICENSE file for details.
