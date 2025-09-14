using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace volumetricshadingupdated.VolumetricShading.Patch;

public class ShaderInjector
{
    private static readonly Regex GeneratedRegex =
        new("^\\s*#\\s*generated\\s+(.*)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private static readonly Regex SnippetRegex =
        new("^\\s*#\\s*snippet\\s+(.*)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

    private readonly ICoreClientAPI _capi;

    private readonly string _domain;
    public bool Debug;

    public ShaderInjector(ICoreClientAPI capi, string domain)
    {
        _capi = capi;
        _domain = domain;
        ShaderProperties = new List<IShaderProperty>();
        GeneratedValues = new Dictionary<string, string>();
        // TEMPORARILY DISABLED: All shader property registration disabled
        // this.RegisterStaticProperty("#define VOLUMETRICSHADINGMOD\r\n");
        
        // MINIMAL RE-ENABLE: Register essential properties for vanilla shaders
        RegisterEssentialProperties();
    }

    public IList<IShaderProperty> ShaderProperties { get; }

    public IDictionary<string, string> GeneratedValues { get; }

    public string this[string key]
    {
        get => GeneratedValues[key];
        set => GeneratedValues[key] = value;
    }

    public void RegisterShaderProperty(IShaderProperty property)
    {
        ShaderProperties.Add(property);
    }

    public void OnShaderLoaded(ShaderProgram program, EnumShaderType shaderType)
    {
        // MINIMAL RE-ENABLE: Only apply essential shader properties, no snippet/generated processing
        var ext = ".unknown";
        Shader shader = null;
        switch (shaderType)
        {
            case EnumShaderType.FragmentShader:
                shader = program.FragmentShader;
                ext = ".frag";
                break;
            case EnumShaderType.VertexShader:
                shader = program.VertexShader;
                ext = ".vert";
                break;
            case EnumShaderType.GeometryShader:
                shader = program.GeometryShader;
                ext = ".geom";
                break;
        }

        if (shader == null)
        {
            return;
        }

        // Apply only essential shader properties (not snippet/generated processing)
        var stringBuilder = new StringBuilder();
        foreach (var shaderProperty in ShaderProperties)
        {
            stringBuilder.Append(shaderProperty.GenerateOutput());
        }

        if (stringBuilder.Length > 0)
        {
            var obj = shader;
            obj.PrefixCode += stringBuilder.ToString();
        }

        if (Debug)
        {
            var text2 = Path.Combine(GamePaths.DataPath, "ShaderDebug");
            Directory.CreateDirectory(text2);
            var path = Path.Combine(text2, program.PassName + ext);
            var code = shader.Code;
            var startIndex = code.IndexOf("\n", Math.Max(0, code.IndexOf("#version", StringComparison.Ordinal)),
                StringComparison.Ordinal) + 1;
            code = code.Insert(startIndex, shader.PrefixCode);
            File.WriteAllText(path, code);
        }
    }

    private string HandleGenerated(string code)
    {
        return GeneratedRegex.Replace(code, InsertGenerated);
    }

    private string HandleSnippets(string code)
    {
        return SnippetRegex.Replace(code, InsertSnippet);
    }

    private string InsertGenerated(Match match)
    {
        var key = match.Groups[1].Value.Trim();
        return GeneratedValues[key];
    }

    private string InsertSnippet(Match match)
    {
        var text = match.Groups[1].Value.Trim();
        return _capi.Assets.Get(new AssetLocation(_domain, "shadersnippets/" + text)).ToText();
    }
    
    /// <summary>
    /// Register essential shader properties needed for vanilla game shaders
    /// This is a minimal implementation to fix godrays.fsh without enabling full shader modification
    /// </summary>
    private void RegisterEssentialProperties()
    {
        // Register properties needed by godrays.fsh
        this.RegisterFloatProperty("VOLUMETRIC_INTENSITY", () => {
            // Use game setting for volumetric lighting intensity
            // If godrays are disabled, return 0, otherwise return a reasonable default
            return _capi.Settings.Int["godRays"] > 0 ? 0.3f : 0.0f;
        });
        
        this.RegisterFloatProperty("VOLUMETRIC_FLATNESS", () => {
            // Return a reasonable default value for volumetric flatness
            return _capi.Settings.Int["godRays"] > 0 ? 0.8f : 0.0f;
        });
    }
    
    /// <summary>
    /// Register a float property for shaders (simplified version)
    /// </summary>
    private void RegisterFloatProperty(string name, Func<float> valueProvider)
    {
        this.RegisterShaderProperty(new FloatValueShaderProperty(name, valueProvider));
    }
}