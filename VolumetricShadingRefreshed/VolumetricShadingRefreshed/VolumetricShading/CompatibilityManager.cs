using System;
using System.Text.RegularExpressions;
using OpenTK.Graphics.OpenGL;
using Vintagestory.API.Client;

namespace volumetricshadingupdated.VolumetricShading;

/// <summary>
/// AMD Compatibility: Hardware compatibility management and automatic fallback settings
/// </summary>
public static class CompatibilityManager
{
    public enum GPUVendor
    {
        NVIDIA,
        AMD,
        Intel, 
        Unknown
    }

    public enum FeatureSupport
    {
        Full,      // Feature fully supported
        Limited,   // Feature supported with limitations
        Disabled   // Feature should be disabled
    }

    private static GPUVendor? _detectedVendor;
    private static string _rendererString;
    private static string _versionString;

    /// <summary>
    /// Detect the GPU vendor based on OpenGL renderer string
    /// </summary>
    public static GPUVendor DetectGPU()
    {
        if (_detectedVendor.HasValue)
            return _detectedVendor.Value;

        try
        {
            _rendererString = GL.GetString(StringName.Renderer);
            _versionString = GL.GetString(StringName.Version);
            var vendorString = GL.GetString(StringName.Vendor);

            // Log detection info for debugging
            VolumetricShadingMod.Instance?.Mod.Logger.Event(
                $"GPU Detection - Vendor: {vendorString}, Renderer: {_rendererString}, Version: {_versionString}");

            // Check for AMD
            if (IsAMDGPU(_rendererString, vendorString))
            {
                _detectedVendor = GPUVendor.AMD;
                VolumetricShadingMod.Instance?.Mod.Logger.Event("Detected AMD GPU - applying compatibility settings");
            }
            // Check for NVIDIA 
            else if (IsNVIDIAGPU(_rendererString, vendorString))
            {
                _detectedVendor = GPUVendor.NVIDIA;
                VolumetricShadingMod.Instance?.Mod.Logger.Event("Detected NVIDIA GPU - full features available");
            }
            // Check for Intel
            else if (IsIntelGPU(_rendererString, vendorString))
            {
                _detectedVendor = GPUVendor.Intel;
                VolumetricShadingMod.Instance?.Mod.Logger.Event("Detected Intel GPU - applying conservative settings");
            }
            else
            {
                _detectedVendor = GPUVendor.Unknown;
                VolumetricShadingMod.Instance?.Mod.Logger.Warning($"Unknown GPU vendor: {vendorString}");
            }
        }
        catch (Exception ex)
        {
            VolumetricShadingMod.Instance?.Mod.Logger.Error($"Failed to detect GPU: {ex.Message}");
            _detectedVendor = GPUVendor.Unknown;
        }

        return _detectedVendor.Value;
    }

    private static bool IsAMDGPU(string renderer, string vendor)
    {
        if (string.IsNullOrEmpty(renderer) || string.IsNullOrEmpty(vendor))
            return false;

        var amdPatterns = new[]
        {
            "AMD", "Radeon", "RX ", "Vega", "RDNA", "GCN", "ATI", 
            "Advanced Micro Devices", "Polaris", "Navi", "RDNA2", "RDNA3"
        };

        var combined = $"{vendor} {renderer}".ToUpperInvariant();
        foreach (var pattern in amdPatterns)
        {
            if (combined.Contains(pattern.ToUpperInvariant()))
                return true;
        }

        return false;
    }

    private static bool IsNVIDIAGPU(string renderer, string vendor)
    {
        if (string.IsNullOrEmpty(renderer) || string.IsNullOrEmpty(vendor))
            return false;

        var nvidiaPatterns = new[] { "NVIDIA", "GeForce", "GTX", "RTX", "Quadro", "Tesla" };
        var combined = $"{vendor} {renderer}".ToUpperInvariant();
        
        foreach (var pattern in nvidiaPatterns)
        {
            if (combined.Contains(pattern.ToUpperInvariant()))
                return true;
        }

        return false;
    }

    private static bool IsIntelGPU(string renderer, string vendor)
    {
        if (string.IsNullOrEmpty(renderer) || string.IsNullOrEmpty(vendor))
            return false;

        var intelPatterns = new[] { "Intel", "Iris", "UHD", "HD Graphics", "Arc" };
        var combined = $"{vendor} {renderer}".ToUpperInvariant();
        
        foreach (var pattern in intelPatterns)
        {
            if (combined.Contains(pattern.ToUpperInvariant()))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Check support level for a specific feature based on detected hardware
    /// </summary>
    public static FeatureSupport CheckFeature(string feature)
    {
        var vendor = DetectGPU();

        switch (feature.ToLowerInvariant())
        {
            case "screenspacereflections":
                return vendor switch
                {
                    GPUVendor.NVIDIA => FeatureSupport.Full,
                    GPUVendor.AMD => FeatureSupport.Limited,  // Precision fixes applied
                    GPUVendor.Intel => FeatureSupport.Limited,
                    _ => FeatureSupport.Limited
                };

            case "volumetriclighting":
                return vendor switch
                {
                    GPUVendor.NVIDIA => FeatureSupport.Full,
                    GPUVendor.AMD => FeatureSupport.Full,     // Should work with precision fixes
                    GPUVendor.Intel => FeatureSupport.Limited,
                    _ => FeatureSupport.Limited
                };

            case "softshadows":
                return vendor switch
                {
                    GPUVendor.NVIDIA => FeatureSupport.Full,
                    GPUVendor.AMD => FeatureSupport.Limited,  // May need reduced sample counts
                    GPUVendor.Intel => FeatureSupport.Disabled,
                    _ => FeatureSupport.Limited
                };

            case "caustics":
                return vendor switch
                {
                    GPUVendor.NVIDIA => FeatureSupport.Full,
                    GPUVendor.AMD => FeatureSupport.Limited,  // Complex math operations
                    GPUVendor.Intel => FeatureSupport.Disabled,
                    _ => FeatureSupport.Limited
                };

            case "deferredlighting":
                return vendor switch
                {
                    GPUVendor.NVIDIA => FeatureSupport.Full,
                    GPUVendor.AMD => FeatureSupport.Full,     // Better performance on AMD
                    GPUVendor.Intel => FeatureSupport.Limited,
                    _ => FeatureSupport.Full
                };

            default:
                return FeatureSupport.Limited;
        }
    }

    /// <summary>
    /// Apply hardware-specific compatibility settings
    /// </summary>
    public static void ApplyCompatibilitySettings()
    {
        var vendor = DetectGPU();
        var logger = VolumetricShadingMod.Instance?.Mod.Logger;

        logger?.Event($"Applying compatibility settings for {vendor} GPU");

        switch (vendor)
        {
            case GPUVendor.AMD:
                ApplyAMDCompatibilitySettings();
                break;
            case GPUVendor.Intel:
                ApplyIntelCompatibilitySettings();
                break;
            case GPUVendor.NVIDIA:
                // NVIDIA generally works well with default settings
                logger?.Event("NVIDIA GPU detected - using full feature set");
                break;
            case GPUVendor.Unknown:
                ApplyConservativeSettings();
                break;
        }
    }

    private static void ApplyAMDCompatibilitySettings()
    {
        var logger = VolumetricShadingMod.Instance?.Mod.Logger;

        // Reduce soft shadow sample count for better performance
        if (ModSettings.SoftShadowSamples > 16)
        {
            ModSettings.SoftShadowSamples = 16;
            logger?.Event("AMD Compatibility: Reduced soft shadow samples to 16");
        }

        // Conservative approach: Let users enable deferred lighting manually for now
        // TODO: Enable automatically once shader compilation issues are resolved
        logger?.Event("AMD Compatibility: Deferred lighting available but not auto-enabled");

        // Conservative caustics settings
        if (CheckFeature("caustics") == FeatureSupport.Limited)
        {
            logger?.Event("AMD Compatibility: Caustics may have reduced quality");
        }

        logger?.Event("Applied AMD-specific compatibility settings");
    }

    private static void ApplyIntelCompatibilitySettings()
    {
        var logger = VolumetricShadingMod.Instance?.Mod.Logger;

        // Disable demanding features on Intel integrated graphics
        if (ModSettings.SSRCausticsEnabled)
        {
            ModSettings.SSRCausticsEnabled = false;
            logger?.Event("Intel Compatibility: Disabled SSR caustics");
        }

        if (ModSettings.SoftShadowsEnabled)
        {
            ModSettings.SoftShadowsEnabled = false;
            logger?.Event("Intel Compatibility: Disabled soft shadows");
        }

        // Reduce sample counts
        if (ModSettings.SoftShadowSamples > 8)
        {
            ModSettings.SoftShadowSamples = 8;
            logger?.Event("Intel Compatibility: Reduced shadow samples");
        }

        logger?.Event("Applied Intel-specific compatibility settings");
    }

    private static void ApplyConservativeSettings()
    {
        var logger = VolumetricShadingMod.Instance?.Mod.Logger;

        // Apply conservative settings for unknown hardware
        if (ModSettings.SoftShadowSamples > 12)
        {
            ModSettings.SoftShadowSamples = 12;
            logger?.Event("Conservative: Reduced shadow samples");
        }

        logger?.Event("Applied conservative compatibility settings for unknown GPU");
    }

    /// <summary>
    /// Get hardware info string for diagnostic purposes
    /// </summary>
    public static string GetHardwareInfo()
    {
        var vendor = DetectGPU();
        return $"GPU: {vendor}, Renderer: {_rendererString ?? "Unknown"}, Version: {_versionString ?? "Unknown"}";
    }

    /// <summary>
    /// Check if current hardware supports advanced features
    /// </summary>
    public static bool SupportsAdvancedFeatures()
    {
        var vendor = DetectGPU();
        return vendor == GPUVendor.NVIDIA || vendor == GPUVendor.AMD;
    }
}
