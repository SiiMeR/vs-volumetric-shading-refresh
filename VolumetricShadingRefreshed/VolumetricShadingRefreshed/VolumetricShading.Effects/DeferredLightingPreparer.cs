using System;
using Vintagestory.API.Client;

namespace volumetricshadingupdated.VolumetricShading.Effects;

public class DeferredLightingPreparer : IRenderer, IDisposable
{
    private readonly DeferredLighting _lighting;

    public DeferredLightingPreparer(DeferredLighting lighting)
    {
        _lighting = lighting;
    }

    public double RenderOrder => 0.0;

    public int RenderRange => int.MaxValue;

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        _lighting.OnBeginRender();
    }

    public void Dispose()
    {
        _lighting.Dispose();
    }
}