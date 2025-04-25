using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace volumetricshadingupdated.VolumetricShading.Patch;

public class YamlPatchLoader
{
    private class PatchEntry
    {
        public string Type;

        public string Filename;

        public string Content;

        public string Snippet;

        public string Tokens;

        public string Regex;

        public bool Multiple;

        public bool Optional;
    }

    public static readonly AssetCategory ShaderPatches = new AssetCategory("shaderpatches", false, (EnumAppSide)2);

    public static readonly AssetCategory ShaderSnippets = new AssetCategory("shadersnippets", false, (EnumAppSide)2);

    private readonly ShaderPatcher _patcher;

    private readonly string _domain;

    private readonly ICoreClientAPI _capi;

    public YamlPatchLoader(ShaderPatcher patcher, string domain, ICoreClientAPI capi)
    {
        _patcher = patcher;
        _domain = domain;
        _capi = capi;
    }

    public void Load()
    {
        ((ICoreAPI)_capi).Assets.Reload(ShaderPatches);
        ((ICoreAPI)_capi).Assets.Reload(ShaderSnippets);
        foreach (IAsset item in ((ICoreAPI)_capi).Assets.GetMany("shaderpatches", _domain, true))
        {
            LoadFromYaml(item.ToText());
        }
    }

    public void LoadFromYaml(string yaml)
    {
        //IL_0061: Unknown result type (might be due to invalid IL or missing references)
        //IL_006b: Expected O, but got Unknown
        foreach (PatchEntry item in new DeserializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance)
                     .Build().Deserialize<IList<PatchEntry>>(yaml))
        {
            string content = item.Content;
            if (!string.IsNullOrEmpty(item.Snippet))
            {
                content = ((ICoreAPI)_capi).Assets.Get(new AssetLocation(_domain, "shadersnippets/" + item.Snippet))
                    .ToText();
            }

            switch (item.Type)
            {
                case "start":
                    AddStartPatch(item, content);
                    break;
                case "end":
                    AddEndPatch(item, content);
                    break;
                case "regex":
                    AddRegexPatch(item, content);
                    break;
                case "token":
                    AddTokenPatch(item, content);
                    break;
                default:
                    throw new ArgumentException("Invalid type " + item.Type);
            }
        }
    }

    private void AddTokenPatch(PatchEntry patch, string content)
    {
        if (patch.Filename == null)
        {
            _patcher.AddTokenPatch(patch.Tokens, content);
        }
        else
        {
            _patcher.AddTokenPatch(patch.Filename, patch.Tokens, content);
        }
    }

    private void AddRegexPatch(PatchEntry patch, string content)
    {
        RegexPatch regexPatch = ((patch.Filename == null)
            ? new RegexPatch(patch.Regex)
            : new RegexPatch(patch.Filename, patch.Regex));
        regexPatch.Multiple = patch.Multiple;
        regexPatch.Optional = patch.Optional;
        regexPatch.ReplacementString = content;
        _patcher.AddPatch(regexPatch);
    }

    private void AddEndPatch(PatchEntry patch, string content)
    {
        if (patch.Filename == null)
        {
            _patcher.AddAtEnd(content);
        }
        else
        {
            _patcher.AddAtEnd(patch.Filename, content);
        }
    }

    private void AddStartPatch(PatchEntry patch, string content)
    {
        if (patch.Filename == null)
        {
            _patcher.AddAtStart(content);
        }
        else
        {
            _patcher.AddAtStart(patch.Filename, content);
        }
    }
}