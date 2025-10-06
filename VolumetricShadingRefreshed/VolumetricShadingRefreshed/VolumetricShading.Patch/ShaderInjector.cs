using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace VolumetricShadingRefreshed.VolumetricShading.Patch;

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
        this.RegisterStaticProperty("#define VOLUMETRICSHADINGMOD\r\n");
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

        var stringBuilder = new StringBuilder();
        foreach (var shaderProperty in ShaderProperties)
        {
            stringBuilder.Append(shaderProperty.GenerateOutput());
        }

        var obj = shader;
        obj.PrefixCode += stringBuilder.ToString();
        shader.Code = HandleGenerated(shader.Code);
        shader.Code = HandleSnippets(shader.Code);
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
}