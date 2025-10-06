using System;
using Vintagestory.API.Client;

namespace VolumetricShadingRefreshed.VolumetricShading.Effects;

public class DeferredLightingRenderer : IRenderer, IDisposable
{
    private readonly DeferredLighting _lighting;

    public DeferredLightingRenderer(DeferredLighting lighting)
    {
        _lighting = lighting;
    }

    public double RenderOrder => 1.0;

    public int RenderRange => int.MaxValue;

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        _lighting.OnEndRender();
    }

    public void Dispose()
    {
    }
}