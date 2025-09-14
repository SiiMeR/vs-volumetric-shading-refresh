using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace volumetricshadingupdated.VolumetricShading;

/// <summary>
/// Manages debug commands for performance analysis and mod testing
/// </summary>
public class DebugCommandManager
{
    private readonly VolumetricShadingMod _mod;
    private readonly ICoreClientAPI _capi;
    
    public DebugCommandManager(VolumetricShadingMod mod)
    {
        _mod = mod;
        _capi = mod.CApi;
    }
    
    /// <summary>
    /// Register all debug commands
    /// </summary>
    public void RegisterCommands()
    {
        try
        {
            // Performance monitoring commands
            _capi.ChatCommands.Create("vsperf")
                .WithDescription("Volumetric Shading Performance Tools")
                .WithArgs(_capi.ChatCommands.Parsers.Word("action"))
                .HandleWith(OnPerformanceCommand);
                
            // Effect control commands  
            _capi.ChatCommands.Create("vseffects")
                .WithDescription("Control Volumetric Shading Effects")
                .WithArgs(_capi.ChatCommands.Parsers.Word("action"), _capi.ChatCommands.Parsers.OptionalWord("effect"))
                .HandleWith(OnEffectsCommand);
                
            // Debug output commands
            _capi.ChatCommands.Create("vsdebug")
                .WithDescription("Volumetric Shading Debug Tools")
                .WithArgs(_capi.ChatCommands.Parsers.Word("action"))
                .HandleWith(OnDebugCommand);
        }
        catch (Exception ex)
        {
            _mod.Mod.Logger.Error($"Failed to register debug commands: {ex.Message}");
        }
    }
    
    private TextCommandResult OnPerformanceCommand(TextCommandCallingArgs args)
    {
        var subCmd = args.Parsers[0].GetValue() as string;
        
        switch (subCmd?.ToLower())
        {
            case "status":
                ShowPerformanceStatus();
                break;
                
            case "reset":
                _mod.PerformanceManager?.Reset();
                _capi.ShowChatMessage("Performance metrics reset.");
                break;
                
            case "emergency":
                var enable = true; // Default to enable for emergency
                _mod.PerformanceManager?.ForceEmergencyMode(enable);
                _capi.ShowChatMessage($"Emergency mode {(enable ? "enabled" : "disabled")}");
                break;
                
            case "profile":
                StartPerformanceProfiling();
                break;
                
            default:
                ShowPerformanceHelp();
                break;
        }
        return TextCommandResult.Success();
    }
    
    private TextCommandResult OnEffectsCommand(TextCommandCallingArgs args)
    {
        var subCmd = args.Parsers[0].GetValue() as string;
        
        switch (subCmd?.ToLower())
        {
            case "list":
                ListAvailableEffects();
                break;
                
            case "enable":
            case "disable":
                var effectName = args.Parsers.Count > 1 ? args.Parsers[1].GetValue() as string : "";
                var enable = subCmd.ToLower() == "enable";
                ToggleEffect(effectName, enable);
                break;
                
            default:
                ShowEffectsHelp();
                break;
        }
        return TextCommandResult.Success();
    }
    
    private TextCommandResult OnDebugCommand(TextCommandCallingArgs args)
    {
        var subCmd = args.Parsers[0].GetValue() as string;
        
        switch (subCmd?.ToLower())
        {
            case "metrics":
                ShowDetailedMetrics();
                break;
                
            case "shaders":
                ShowShaderInfo();
                break;
                
            case "framebuffers":
                ShowFramebufferInfo();
                break;
                
            default:
                ShowDebugHelp();
                break;
        }
        return TextCommandResult.Success();
    }
    
    private void ShowPerformanceStatus()
    {
        var status = _mod.PerformanceManager?.GetPerformanceStatus() ?? "Performance manager not initialized";
        _capi.ShowChatMessage($"🎯 {status}");
        
        // Show key metrics
        var metrics = _mod.PerformanceManager?.GetMetrics();
        if (metrics != null && metrics.Count > 0)
        {
            _capi.ShowChatMessage("📊 Top Performance Consumers:");
            var sortedMetrics = new List<KeyValuePair<string, PerformanceManager.PerformanceMetrics>>(metrics);
            sortedMetrics.Sort((a, b) => b.Value.AverageTime.CompareTo(a.Value.AverageTime));
            
            for (int i = 0; i < Math.Min(5, sortedMetrics.Count); i++)
            {
                var kvp = sortedMetrics[i];
                var bottleneck = kvp.Value.IsBottleneck ? " ⚠️ BOTTLENECK" : "";
                _capi.ShowChatMessage($"  {i + 1}. {kvp.Key}: {kvp.Value.AverageTime:F2}ms{bottleneck}");
            }
        }
    }
    
    private void StartPerformanceProfiling()
    {
        _capi.ShowChatMessage("🔍 Starting comprehensive performance analysis...");
        _capi.ShowChatMessage("Use these Vintage Story debug commands:");
        _capi.ShowChatMessage("  • Press Ctrl+F3 for FPS display");
        _capi.ShowChatMessage("  • Press Ctrl+F10 for frame profiler");
        _capi.ShowChatMessage("  • Type .edi for extended debug info");
        _capi.ShowChatMessage("  • Type .debug logticks 40 for tick logging (3 FPS = use 40)");
        _capi.ShowChatMessage("📝 Check logs in %appdata%/VintagestoryData/Logs/client-main.txt");
        
        // Start our internal profiling
        _mod.PerformanceManager?.Reset();
    }
    
    private void ListAvailableEffects()
    {
        _capi.ShowChatMessage("🎨 Available Volumetric Shading Effects:");
        _capi.ShowChatMessage($"  • SSR (Screen Space Reflections): {(ModSettings.ScreenSpaceReflectionsEnabled ? "✅ ON" : "❌ OFF")}");
        _capi.ShowChatMessage($"  • Deferred Lighting: {(ModSettings.DeferredLightingEnabled ? "✅ ON" : "❌ OFF")}");
        _capi.ShowChatMessage($"  • Volumetric Lighting: {(_mod.VolumetricLighting != null ? "✅ Available" : "❌ Disabled")}");
        _capi.ShowChatMessage($"  • SSAO: {(_mod.ScreenSpaceDirectionalOcclusion != null ? "✅ Available" : "❌ Disabled")}");
        _capi.ShowChatMessage($"  • Shadow Tweaks: {(_mod.ShadowTweaks != null ? "✅ Available" : "❌ Disabled")}");
    }
    
    private void ToggleEffect(string effectName, bool enable)
    {
        switch (effectName?.ToLower())
        {
            case "ssr":
            case "reflections":
                ModSettings.ScreenSpaceReflectionsEnabled = enable;
                _capi.ShowChatMessage($"Screen Space Reflections {(enable ? "enabled" : "disabled")}");
                break;
                
            case "deferred":
            case "lighting":
                ModSettings.DeferredLightingEnabled = enable;
                _capi.ShowChatMessage($"Deferred Lighting {(enable ? "enabled" : "disabled")}");
                break;
                
            case "ssao":
            case "occlusion":
                _capi.ShowChatMessage($"SSAO control not yet implemented - restart game to change");
                break;
                
            case "shadows":
            case "shadowtweaks":
                _capi.ShowChatMessage($"Shadow Tweaks control not yet implemented - restart game to change");
                break;
                
            default:
                _capi.ShowChatMessage($"❌ Unknown effect: {effectName}");
                _capi.ShowChatMessage("Available: ssr, deferred, ssao, shadows");
                break;
        }
    }
    
    private void ShowDetailedMetrics()
    {
        var metrics = _mod.PerformanceManager?.GetMetrics();
        if (metrics == null || metrics.Count == 0)
        {
            _capi.ShowChatMessage("No performance metrics available yet.");
            return;
        }
        
        _capi.ShowChatMessage("📈 Detailed Performance Metrics:");
        foreach (var kvp in metrics)
        {
            var m = kvp.Value;
            var status = m.IsBottleneck ? "⚠️ BOTTLENECK" : "✅ OK";
            _capi.ShowChatMessage($"  {kvp.Key} {status}:");
            _capi.ShowChatMessage($"    Avg: {m.AverageTime:F2}ms | Max: {m.MaxTime:F2}ms | Samples: {m.SampleCount}");
        }
    }
    
    private void ShowShaderInfo()
    {
        _capi.ShowChatMessage("🔧 Shader Information:");
        _capi.ShowChatMessage($"  • Graphics info available in client log");
        _capi.ShowChatMessage($"  • Use .debug info or check client-main.txt for GL details");
        
        // Check shader compilation status
        _capi.ShowChatMessage("  Shader Status:");
        if (_mod.VolumetricLighting != null) _capi.ShowChatMessage("    ✅ Volumetric Lighting loaded");
        if (_mod.ScreenSpaceReflections != null) _capi.ShowChatMessage("    ✅ SSR loaded");  
        if (_mod.DeferredLighting != null) _capi.ShowChatMessage("    ✅ Deferred Lighting loaded");
    }
    
    private void ShowFramebufferInfo()
    {
        _capi.ShowChatMessage("🖼️ Framebuffer Information:");
        _capi.ShowChatMessage($"  • Window Size: {_capi.Render.FrameWidth}x{_capi.Render.FrameHeight}");
        _capi.ShowChatMessage("  • Use .debug renderers to see active renderers");
        _capi.ShowChatMessage("  • Use .debug meshsummary for mesh statistics");
    }
    
    private void ShowPerformanceHelp()
    {
        _capi.ShowChatMessage("🎯 Volumetric Shading Performance Commands:");
        _capi.ShowChatMessage("  /vsperf status    - Show current performance metrics");
        _capi.ShowChatMessage("  /vsperf reset     - Reset performance counters");
        _capi.ShowChatMessage("  /vsperf emergency - Toggle emergency performance mode");
        _capi.ShowChatMessage("  /vsperf profile   - Start comprehensive profiling");
    }
    
    private void ShowEffectsHelp()
    {
        _capi.ShowChatMessage("🎨 Volumetric Shading Effects Commands:");
        _capi.ShowChatMessage("  /vseffects list              - List all effects and status");
        _capi.ShowChatMessage("  /vseffects enable [effect]   - Enable specific effect");
        _capi.ShowChatMessage("  /vseffects disable [effect]  - Disable specific effect");
        _capi.ShowChatMessage("  Effects: ssr, deferred, ssao, shadows");
    }
    
    private void ShowDebugHelp()
    {
        _capi.ShowChatMessage("🔍 Volumetric Shading Debug Commands:");
        _capi.ShowChatMessage("  /vsdebug metrics      - Show detailed performance metrics");
        _capi.ShowChatMessage("  /vsdebug shaders      - Show shader and OpenGL information");
        _capi.ShowChatMessage("  /vsdebug framebuffers - Show framebuffer information");
    }
}
