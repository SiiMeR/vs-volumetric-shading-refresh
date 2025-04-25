using System;
using Vintagestory.API.Client;

namespace VolumetricShading.Effects;

public class DeferredLightingPreparer : IRenderer, IDisposable
{
	private readonly DeferredLighting _lighting;

	public double RenderOrder => 0.0;

	public int RenderRange => int.MaxValue;

	public DeferredLightingPreparer(DeferredLighting lighting)
	{
		_lighting = lighting;
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		_lighting.OnBeginRender();
	}

	public void Dispose()
	{
		_lighting.Dispose();
	}
}
