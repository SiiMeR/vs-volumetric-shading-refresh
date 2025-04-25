using System;
using Vintagestory.API.Client;
using Vintagestory.API.MathTools;

namespace volumetricshadingupdated.VolumetricShading;

public class Uniforms : IRenderer, IDisposable
{
    private readonly VolumetricShadingMod _mod;

    private readonly Vec4f _tempVec4f = new Vec4f();

    public readonly float[] InvProjectionMatrix = Mat4f.Create();

    public readonly float[] InvModelViewMatrix = Mat4f.Create();

    public readonly Vec4f CameraWorldPosition = new Vec4f();

    public float DayLight { get; private set; }

    public double RenderOrder => 0.1;

    public int RenderRange => int.MaxValue;

    public Uniforms(VolumetricShadingMod mod)
    {
        //IL_0001: Unknown result type (might be due to invalid IL or missing references)
        //IL_000b: Expected O, but got Unknown
        //IL_0022: Unknown result type (might be due to invalid IL or missing references)
        //IL_002c: Expected O, but got Unknown
        _mod = mod;
        mod.CApi.Event.RegisterRenderer((IRenderer)(object)this, (EnumRenderStage)0, (string)null);
    }

    public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
    {
        Mat4f.Invert(InvProjectionMatrix, _mod.CApi.Render.CurrentProjectionMatrix);
        Mat4f.Invert(InvModelViewMatrix, _mod.CApi.Render.CameraMatrixOriginf);
        _tempVec4f.Set(0f, 0f, 0f, 1f);
        Mat4f.MulWithVec4(InvModelViewMatrix, _tempVec4f, CameraWorldPosition);
        DayLight = 1.25f * GameMath.Max(new float[2]
        {
            _mod.CApi.World.Calendar.DayLightStrength - _mod.CApi.World.Calendar.MoonLightStrength / 2f,
            0.05f
        });
    }

    public void Dispose()
    {
    }
}