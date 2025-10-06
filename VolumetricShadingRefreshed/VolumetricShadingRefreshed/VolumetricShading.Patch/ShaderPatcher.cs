using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.Client;

namespace VolumetricShadingRefreshed.VolumetricShading.Patch;

public class ShaderPatcher
{
    private readonly YamlPatchLoader _yamlPatchLoader;

    public readonly Dictionary<string, string> Cache = new();
    public readonly List<IShaderPatch> Patches = new();

    public ShaderPatcher(ICoreClientAPI capi, string domain)
    {
        _yamlPatchLoader = new YamlPatchLoader(this, domain, capi);
    }

    public event Action OnReload;

    public void Reload()
    {
        Cache.Clear();
        Patches.Clear();
        _yamlPatchLoader.Load();
        OnReload?.Invoke();
    }

    public void AddPatch(IShaderPatch patch)
    {
        Patches.Add(patch);
    }

    public void AddTokenPatch(string token, string replacement)
    {
        AddPatch(new TokenPatch(token)
        {
            ReplacementString = replacement
        });
    }

    public void AddTokenPatch(string filename, string token, string replacement)
    {
        AddPatch(new TokenPatch(filename, token)
        {
            ReplacementString = replacement
        });
    }

    public void AddAtStart(string content)
    {
        AddPatch(new StartPatch
        {
            Content = content
        });
    }

    public void AddAtStart(string filename, string content)
    {
        AddPatch(new StartPatch(filename)
        {
            Content = content
        });
    }

    public void AddAtEnd(string content)
    {
        AddPatch(new RegexPatch("$")
        {
            ReplacementString = content
        });
    }

    public void AddAtEnd(string filename, string content)
    {
        AddPatch(new RegexPatch(filename, "$")
        {
            ReplacementString = content
        });
    }

    public string Patch(string filename, string code, bool cache = false)
    {
        if (cache && Cache.ContainsKey(filename))
        {
            return Cache[filename];
        }

        foreach (var item in Patches.Where(patch => patch.ShouldPatch(filename, code)))
        {
            code = item.Patch(filename, code);
        }

        if (cache)
        {
            Cache[filename] = code;
        }

        return code;
    }
}