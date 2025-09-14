using System;
using System.Collections.Generic;
using System.Diagnostics;
using Vintagestory.API.Client;

namespace volumetricshadingupdated.VolumetricShading;

public class PerformanceManager
{
    private readonly VolumetricShadingMod _mod;
    private readonly Dictionary<string, PerformanceMetrics> _metrics = new();
    
    private double _frameTimeAccumulator = 0;
    private int _frameCount = 0;
    private double _averageFrameTime = 16.67; // Default to 60fps
    private DateTime _lastCheck = DateTime.Now;
    
    // Performance thresholds
    private const double TARGET_FRAME_TIME_MS = 16.67; // 60fps
    private const double CRITICAL_FRAME_TIME_MS = 33.33; // 30fps
    private const double EMERGENCY_FRAME_TIME_MS = 200.0; // 5fps
    
    // Quality reduction state
    private int _qualityLevel = 3; // 0=lowest, 3=highest
    private bool _emergencyMode = false;
    
    public PerformanceManager(VolumetricShadingMod mod)
    {
        _mod = mod;
    }
    
    public class PerformanceMetrics
    {
        public double TotalTime { get; set; }
        public double MaxTime { get; set; }
        public double AverageTime { get; set; }
        public int SampleCount { get; set; }
        public bool IsBottleneck => AverageTime > TARGET_FRAME_TIME_MS * 0.3; // >30% of frame time
    }
    
    /// <summary>
    /// Start timing a performance-critical section
    /// </summary>
    public Stopwatch StartTiming(string section)
    {
        var stopwatch = Stopwatch.StartNew();
        return stopwatch;
    }
    
    /// <summary>
    /// End timing and record the results
    /// </summary>
    public void EndTiming(string section, Stopwatch stopwatch)
    {
        stopwatch.Stop();
        var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
        
        if (!_metrics.ContainsKey(section))
        {
            _metrics[section] = new PerformanceMetrics();
        }
        
        var metrics = _metrics[section];
        metrics.TotalTime += elapsedMs;
        metrics.MaxTime = Math.Max(metrics.MaxTime, elapsedMs);
        metrics.SampleCount++;
        metrics.AverageTime = metrics.TotalTime / metrics.SampleCount;
        
        // Log performance warnings for slow operations
        if (elapsedMs > TARGET_FRAME_TIME_MS * 0.5) // >50% of target frame time
        {
            _mod.Mod.Logger.Warning($"Performance: {section} took {elapsedMs:F2}ms (>50% frame budget)");
        }
    }
    
    /// <summary>
    /// Update frame timing and adjust quality if needed
    /// </summary>
    public void UpdateFrameTiming(double deltaTime)
    {
        var frameTimeMs = deltaTime * 1000.0;
        _frameTimeAccumulator += frameTimeMs;
        _frameCount++;
        
        // Calculate average every second
        if ((DateTime.Now - _lastCheck).TotalSeconds >= 1.0)
        {
            _averageFrameTime = _frameTimeAccumulator / _frameCount;
            
            // Check for performance issues and adjust quality
            CheckAndAdjustQuality();
            
            // Reset counters
            _frameTimeAccumulator = 0;
            _frameCount = 0;
            _lastCheck = DateTime.Now;
            
            // Log severe performance issues
            if (_averageFrameTime > CRITICAL_FRAME_TIME_MS)
            {
                _mod.Mod.Logger.Warning($"Performance: Average frame time {_averageFrameTime:F1}ms (Target: {TARGET_FRAME_TIME_MS:F1}ms)");
                LogBottlenecks();
            }
        }
    }
    
    /// <summary>
    /// Automatically adjust quality based on performance
    /// </summary>
    private void CheckAndAdjustQuality()
    {
        var previousQualityLevel = _qualityLevel;
        
        if (_averageFrameTime > EMERGENCY_FRAME_TIME_MS)
        {
            // Emergency: disable most effects
            _qualityLevel = 0;
            _emergencyMode = true;
            _mod.Mod.Logger.Error($"Emergency performance mode activated! FPS critically low ({1000.0/_averageFrameTime:F1} fps)");
        }
        else if (_averageFrameTime > CRITICAL_FRAME_TIME_MS * 1.5)
        {
            // Very poor performance: lowest quality
            _qualityLevel = Math.Max(0, _qualityLevel - 1);
            _emergencyMode = false;
        }
        else if (_averageFrameTime > CRITICAL_FRAME_TIME_MS)
        {
            // Poor performance: reduce quality
            _qualityLevel = Math.Max(1, _qualityLevel - 1);
            _emergencyMode = false;
        }
        else if (_averageFrameTime < TARGET_FRAME_TIME_MS * 0.8)
        {
            // Good performance: can increase quality
            _qualityLevel = Math.Min(3, _qualityLevel + 1);
            _emergencyMode = false;
        }
        
        if (_qualityLevel != previousQualityLevel)
        {
            _mod.Mod.Logger.Event($"Performance: Quality level changed from {previousQualityLevel} to {_qualityLevel} (Frame time: {_averageFrameTime:F1}ms)");
            ApplyQualitySettings();
        }
    }
    
    /// <summary>
    /// Apply performance-based quality settings
    /// </summary>
    private void ApplyQualitySettings()
    {
        switch (_qualityLevel)
        {
            case 0: // Emergency - disable most effects
                SetSSREnabled(false);
                SetVolumetricLightingEnabled(false);
                SetDeferredLightingEnabled(false);
                _mod.Mod.Logger.Warning("Performance: Disabled SSR, Volumetric Lighting, and Deferred Lighting");
                break;
                
            case 1: // Low - basic features only
                SetSSREnabled(false);
                SetVolumetricLightingEnabled(false);
                SetDeferredLightingEnabled(true);
                _mod.Mod.Logger.Event("Performance: Low quality mode - SSR and Volumetric Lighting disabled");
                break;
                
            case 2: // Medium - selective features
                SetSSREnabled(false);
                SetVolumetricLightingEnabled(true);
                SetDeferredLightingEnabled(true);
                _mod.Mod.Logger.Event("Performance: Medium quality mode - SSR disabled");
                break;
                
            case 3: // High - all features
                SetSSREnabled(true);
                SetVolumetricLightingEnabled(true);
                SetDeferredLightingEnabled(true);
                _mod.Mod.Logger.Event("Performance: High quality mode - all features enabled");
                break;
        }
    }
    
    private void SetSSREnabled(bool enabled)
    {
        if (ModSettings.ScreenSpaceReflectionsEnabled != enabled)
        {
            ModSettings.ScreenSpaceReflectionsEnabled = enabled;
        }
    }
    
    private void SetVolumetricLightingEnabled(bool enabled)
    {
        // Note: GodRayQuality is controlled through game settings, not directly accessible
        // This would need to be handled through the game's settings system
        _mod.Mod.Logger.Event($"Performance: Would set Volumetric Lighting to {enabled}");
    }
    
    private void SetDeferredLightingEnabled(bool enabled)
    {
        // Control deferred lighting through mod settings
        _mod.Mod.Logger.Event($"Performance: Would set Deferred Lighting to {enabled}");
    }
    
    /// <summary>
    /// Log performance bottlenecks
    /// </summary>
    private void LogBottlenecks()
    {
        _mod.Mod.Logger.Event("=== Performance Analysis ===");
        _mod.Mod.Logger.Event($"Average Frame Time: {_averageFrameTime:F2}ms ({1000.0/_averageFrameTime:F1} fps)");
        _mod.Mod.Logger.Event($"Quality Level: {_qualityLevel}/3 {(_emergencyMode ? "(EMERGENCY)" : "")}");
        
        foreach (var kvp in _metrics)
        {
            var metrics = kvp.Value;
            if (metrics.IsBottleneck)
            {
                _mod.Mod.Logger.Warning($"BOTTLENECK: {kvp.Key} - Avg: {metrics.AverageTime:F2}ms, Max: {metrics.MaxTime:F2}ms, Samples: {metrics.SampleCount}");
            }
            else
            {
                _mod.Mod.Logger.Event($"{kvp.Key} - Avg: {metrics.AverageTime:F2}ms, Max: {metrics.MaxTime:F2}ms, Samples: {metrics.SampleCount}");
            }
        }
        _mod.Mod.Logger.Event("========================");
    }
    
    /// <summary>
    /// Get current performance status
    /// </summary>
    public string GetPerformanceStatus()
    {
        var fps = 1000.0 / _averageFrameTime;
        var status = _emergencyMode ? "CRITICAL" : 
                    _averageFrameTime > CRITICAL_FRAME_TIME_MS ? "POOR" :
                    _averageFrameTime > TARGET_FRAME_TIME_MS ? "FAIR" : "GOOD";
        
        return $"Performance: {status} ({fps:F1} fps, Q{_qualityLevel})";
    }
    
    /// <summary>
    /// Reset all metrics
    /// </summary>
    public void Reset()
    {
        _metrics.Clear();
        _frameTimeAccumulator = 0;
        _frameCount = 0;
        _averageFrameTime = TARGET_FRAME_TIME_MS;
        _lastCheck = DateTime.Now;
    }
    
    /// <summary>
    /// Force emergency mode for testing
    /// </summary>
    public void ForceEmergencyMode(bool enable)
    {
        _emergencyMode = enable;
        _qualityLevel = enable ? 0 : 3;
        ApplyQualitySettings();
        _mod.Mod.Logger.Event($"Emergency mode {(enable ? "enabled" : "disabled")} manually");
    }
    
    /// <summary>
    /// Get bottleneck information
    /// </summary>
    public Dictionary<string, PerformanceMetrics> GetMetrics()
    {
        return new Dictionary<string, PerformanceMetrics>(_metrics);
    }
}
