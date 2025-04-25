using System;
using Vintagestory.API.Client;

namespace VolumetricShading.Effects;

public class DeferredLightingRenderer : IRenderer, IDisposable
{
	private readonly DeferredLighting _lighting;

	public double RenderOrder => 1.0;

	public int RenderRange => int.MaxValue;

	public DeferredLightingRenderer(DeferredLighting lighting)
	{
		_lighting = lighting;
	}

	public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
	{
		_lighting.OnEndRender();
	}

	public void Dispose()
	{
	}
}
