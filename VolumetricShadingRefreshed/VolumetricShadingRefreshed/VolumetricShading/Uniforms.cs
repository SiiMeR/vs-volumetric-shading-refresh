using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace volumetricshadingupdated.VolumetricShading;

public class Uniforms : IRenderer, IDisposable
{
    private readonly VolumetricShadingMod _mod;

    private readonly Vec4f _tempVec4f = new();

    public readonly Vec4f CameraWorldPosition = new();

    public readonly float[] InvModelViewMatrix = Mat4f.Create();

    public readonly float[] InvProjectionMatrix = Mat4f.Create();

    public Uniforms(VolumetricShadingMod mod)
    {
        _mod = mod;

        mod.CApi.Event.RegisterRenderer(this, EnumRenderStage.Before);
    }

    public float DayLight { get; private set; }

    public double RenderOrder => 0.1;

    public int RenderRange => int.MaxValue;

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        Mat4f.Invert(InvProjectionMatrix, _mod.CApi.Render.CurrentProjectionMatrix);
        Mat4f.Invert(InvModelViewMatrix, _mod.CApi.Render.CameraMatrixOriginf);
        _tempVec4f.Set(0f, 0f, 0f, 1f);
        Mat4f.MulWithVec4(InvModelViewMatrix, _tempVec4f, CameraWorldPosition);
        DayLight = 1.25f *
                   GameMath.Max(
                       _mod.CApi.World.Calendar.DayLightStrength - _mod.CApi.World.Calendar.MoonLightStrength / 2f,
                       0.05f);
    }

    public void Dispose()
    {
    }
}