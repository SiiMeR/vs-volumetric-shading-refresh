using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.Client.NoObf;

namespace VolumetricShading.Patch;

public class ShaderInjector
{
	public bool Debug;

	private readonly ICoreClientAPI _capi;

	private readonly string _domain;

	private static readonly Regex GeneratedRegex = new Regex("^\\s*#\\s*generated\\s+(.*)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

	private static readonly Regex SnippetRegex = new Regex("^\\s*#\\s*snippet\\s+(.*)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

	public IList<IShaderProperty> ShaderProperties { get; }

	public IDictionary<string, string> GeneratedValues { get; }

	public string this[string key]
	{
		get
		{
			return GeneratedValues[key];
		}
		set
		{
			GeneratedValues[key] = value;
		}
	}

	public ShaderInjector(ICoreClientAPI capi, string domain)
	{
		_capi = capi;
		_domain = domain;
		ShaderProperties = new List<IShaderProperty>();
		GeneratedValues = new Dictionary<string, string>();
		this.RegisterStaticProperty("#define VOLUMETRICSHADINGMOD\r\n");
	}

	public void RegisterShaderProperty(IShaderProperty property)
	{
		ShaderProperties.Add(property);
	}

	public void OnShaderLoaded(ShaderProgram program, EnumShaderType shaderType)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_000e: Invalid comparison between Unknown and I4
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Invalid comparison between Unknown and I4
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Invalid comparison between Unknown and I4
		string text = ".unknown";
		Shader val = null;
		if ((int)shaderType != 35632)
		{
			if ((int)shaderType != 35633)
			{
				if ((int)shaderType == 36313)
				{
					val = ((ShaderProgramBase)program).GeometryShader;
					text = ".geom";
				}
			}
			else
			{
				val = ((ShaderProgramBase)program).VertexShader;
				text = ".vert";
			}
		}
		else
		{
			val = ((ShaderProgramBase)program).FragmentShader;
			text = ".frag";
		}
		if (val == null)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (IShaderProperty shaderProperty in ShaderProperties)
		{
			stringBuilder.Append(shaderProperty.GenerateOutput());
		}
		Shader obj = val;
		obj.PrefixCode += stringBuilder.ToString();
		val.Code = HandleGenerated(val.Code);
		val.Code = HandleSnippets(val.Code);
		if (Debug)
		{
			string text2 = Path.Combine(GamePaths.DataPath, "ShaderDebug");
			Directory.CreateDirectory(text2);
			string path = Path.Combine(text2, ((ShaderProgramBase)program).PassName + text);
			string code = val.Code;
			int startIndex = code.IndexOf("\n", Math.Max(0, code.IndexOf("#version", StringComparison.Ordinal)), StringComparison.Ordinal) + 1;
			code = code.Insert(startIndex, val.PrefixCode);
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
		string key = match.Groups[1].Value.Trim();
		return GeneratedValues[key];
	}

	private string InsertSnippet(Match match)
	{
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003d: Expected O, but got Unknown
		string text = match.Groups[1].Value.Trim();
		return ((ICoreAPI)_capi).Assets.Get(new AssetLocation(_domain, "shadersnippets/" + text)).ToText();
	}
}
