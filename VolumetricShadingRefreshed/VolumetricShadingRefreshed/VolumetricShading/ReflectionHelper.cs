using System;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShadingRefreshed.VolumetricShading;

public static class ReflectionHelper
{
    public static ClientMain GetClient(this ICoreClientAPI api)
    {
        //IL_0006: Unknown result type (might be due to invalid IL or missing references)
        //IL_000c: Expected O, but got Unknown
        return (ClientMain)api.World;
    }

    public static ClientPlatformAbstract GetClientPlatformAbstract(this ClientMain client)
    {
        var field = typeof(ClientMain).GetField("Platform", BindingFlags.Instance | BindingFlags.Public);
        var clientPlatformAbstract =
            (ClientPlatformAbstract)(field != null ? field.GetValue(client) : null);
        if (clientPlatformAbstract == null)
        {
            throw new Exception("Could not fetch platform via reflection!");
        }

        return clientPlatformAbstract;
    }

    public static ClientPlatformWindows GetClientPlatformWindows(this ClientMain client)
    {
        //IL_0006: Unknown result type (might be due to invalid IL or missing references)
        //IL_000c: Expected O, but got Unknown
        return (ClientPlatformWindows)client.GetClientPlatformAbstract();
    }

    public static ClientPlatformAbstract GetClientPlatformAbstract(this ICoreClientAPI api)
    {
        return api.GetClient().GetClientPlatformAbstract();
    }

    public static ClientPlatformWindows GetClientPlatformWindows(this ICoreClientAPI api)
    {
        return api.GetClient().GetClientPlatformWindows();
    }

    public static ChunkRenderer GetChunkRenderer(this ClientMain client)
    {
        var field = typeof(ClientMain).GetField("chunkRenderer", BindingFlags.Instance | BindingFlags.NonPublic);
        var chunkRenderer = (ChunkRenderer)(field != null ? field.GetValue(client) : null);
        if (chunkRenderer == null)
        {
            throw new Exception("Could not fetch chunk renderer!");
        }

        return chunkRenderer;
    }


    public static MeshRef GetScreenQuad(this ClientPlatformWindows platform)
    {
        var field =
            typeof(ClientPlatformWindows).GetField("screenQuad", BindingFlags.Instance | BindingFlags.NonPublic);
        var meshRef = (MeshRef)(field != null ? field.GetValue(platform) : null);
        if (meshRef == null)
        {
            throw new Exception("Could not fetch screen quad");
        }

        return meshRef;
    }

    public static void TriggerOnlyOnMouseUp(this GuiElementSlider slider, bool trigger = true)
    {
        var method =
            typeof(GuiElementSlider).GetMethod("TriggerOnlyOnMouseUp", BindingFlags.Instance | BindingFlags.NonPublic);
        if (method == null)
        {
            throw new Exception("Could not get trigger only on mouse up method.");
        }

        method.Invoke(slider, new object[] { trigger });
    }
}