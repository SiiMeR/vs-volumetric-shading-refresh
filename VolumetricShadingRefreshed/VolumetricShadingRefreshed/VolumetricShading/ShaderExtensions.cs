using System;
using System.IO;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading
{
    /// <summary>
    /// Extensions for modern shader management using official Vintage Story APIs
    /// instead of string-based patching. This reduces technical debt and improves maintainability.
    /// </summary>
    public static class VSModShaderExtensions
    {
        /// <summary>
        /// Register a shader with error handling and diagnostics
        /// </summary>
        public static IShaderProgram RegisterVSModShader(this VolumetricShadingMod mod, string name, ref bool success)
        {
            try
            {
                var shader = (ShaderProgram)mod.CApi.Shader.NewShaderProgram();
                shader.AssetDomain = mod.Mod.Info.ModID; // Available since latest API updates
                mod.CApi.Shader.RegisterFileShaderProgram(name, shader);
                
                if (!shader.Compile())
                {
                    success = false;
                    mod.Mod.Logger.Error($"Failed to compile shader: {name}");
                    return null;
                }
                
                // Add custom shader unload hook - Vintage Story specific approach
                try 
                {
                    // Instead of using Disposers property which may not exist in all VS versions
                    // Use reflection as a fallback
                    FieldInfo disposersField = typeof(ShaderProgram).GetField("disposers", 
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    
                    if (disposersField != null)
                    {
                        var disposers = disposersField.GetValue(shader) as System.Collections.Generic.List<Action>;
                        if (disposers != null)
                        {
                            disposers.Add(() => 
                            {
                                mod.Mod.Logger.Notification($"Shader {name} disposed");
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    mod.Mod.Logger.Warning($"Could not add shader dispose hook (non-critical): {ex.Message}");
                }
                
                return shader;
            }
            catch (Exception ex)
            {
                success = false;
                mod.Mod.Logger.Error($"Exception registering shader '{name}': {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Register a shader with automatic uniform binding
        /// </summary>
        public static IShaderProgram RegisterShaderWithUniforms(this VolumetricShadingMod mod, string name)
        {
            bool success = true;
            var shader = mod.RegisterVSModShader(name, ref success);
            
            if (shader != null && success)
            {
                // Use a safe approach to add uniform updates
                // Store the shader in a dictionary for updates in a central location
                mod.ShaderUniformManager.RegisterManagedShader(name, shader);
            }
            
            return success ? shader : null;
        }
        
        /// <summary>
        /// Check if a shader is compatible with the current GPU
        /// </summary>
        public static bool IsShaderCompatible(this VolumetricShadingMod mod, string shaderName)
        {
            // Check for known compatibility issues
            var (isAmd, isIntel, isNvidia, _) = mod.ShaderUniformManager.GetVendorInfo();
            
            if (isAmd)
            {
                // AMD-specific checks
                if (shaderName.Contains("ssr_"))
                {
                    // Instead of using a missing setting
                    bool enableExperimentalFeatures = mod.CApi.Settings.Bool.Get("volumetricshading_experimentalAmdFeatures", false);
                    if (!enableExperimentalFeatures)
                    {
                        mod.Mod.Logger.Warning($"Shader {shaderName} disabled on AMD GPUs (enable in experimental settings)");
                        return false;
                    }
                }
            }
            
            if (isIntel)
            {
                // Intel-specific checks
                if (shaderName.Contains("deferred_"))
                {
                    mod.Mod.Logger.Warning($"Shader {shaderName} disabled on Intel GPUs (known compatibility issues)");
                    return false;
                }
            }
            
            return true;
        }
        
        /// <summary>
        /// Add shader debugging support
        /// </summary>
        public static void WriteDebugShader(this VolumetricShadingMod mod, ShaderProgram shader, string shaderName)
        {
            if (!mod.Debug) return;
            
            try
            {
                var debugDir = Path.Combine(GamePaths.DataPath, "ShaderDebug");
                Directory.CreateDirectory(debugDir);
                
                if (shader.VertexShader != null)
                {
                    string vertPath = Path.Combine(debugDir, $"{shaderName}.vsh");
                    File.WriteAllText(vertPath, shader.VertexShader.Code);
                }
                
                if (shader.FragmentShader != null)
                {
                    string fragPath = Path.Combine(debugDir, $"{shaderName}.fsh");
                    File.WriteAllText(fragPath, shader.FragmentShader.Code);
                }
                
                mod.Mod.Logger.Event($"Wrote debug shader: {shaderName}");
            }
            catch (Exception ex)
            {
                mod.Mod.Logger.Warning($"Failed to write debug shader: {ex.Message}");
            }
        }
    }
}
