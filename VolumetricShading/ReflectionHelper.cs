using System;
using System.Reflection;
using Vintagestory.API.Client;
using Vintagestory.Client.NoObf;

namespace VolumetricShading;

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
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		ClientPlatformAbstract val = (ClientPlatformAbstract)(typeof(ClientMain).GetField("Platform", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(client));
		if ((int)val == 0)
		{
			throw new Exception("Could not fetch platform via reflection!");
		}
		return val;
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
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		ChunkRenderer val = (ChunkRenderer)(typeof(ClientMain).GetField("chunkRenderer", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(client));
		if ((int)val == 0)
		{
			throw new Exception("Could not fetch chunk renderer!");
		}
		return val;
	}

	public static MeshRef GetScreenQuad(this ClientPlatformWindows platform)
	{
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Expected O, but got Unknown
		MeshRef val = (MeshRef)(typeof(ClientPlatformWindows).GetField("screenQuad", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(platform));
		if ((int)val == 0)
		{
			throw new Exception("Could not fetch screen quad");
		}
		return val;
	}

	public static void TriggerOnlyOnMouseUp(this GuiElementSlider slider, bool trigger = true)
	{
		MethodInfo? method = typeof(GuiElementSlider).GetMethod("TriggerOnlyOnMouseUp", BindingFlags.Instance | BindingFlags.NonPublic);
		if (method == null)
		{
			throw new Exception("Could not get trigger only on mouse up method.");
		}
		method.Invoke(slider, new object[1] { trigger });
	}
}
