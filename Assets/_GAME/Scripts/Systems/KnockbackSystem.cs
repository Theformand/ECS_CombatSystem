using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


public struct KnockBack : IComponentData, IEnableableComponent
{
    public KnockBackSettings Settings;
    public float T;
}

[Serializable]
public struct KnockBackSettings
{
    public float3 dir;
    public float Duration;
    public float Distance;
}


public partial struct KnockbackSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
       state.RequireForUpdate<CurveLib>();
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        //var curveLib = SystemAPI.GetSingleton<CurveLib>();
        //var dt = SystemAPI.Time.DeltaTime;
        //foreach (var (k, toggle, transform) in SystemAPI.Query<RefRW<KnockBack>, EnabledRefRW<KnockBack>, RefRW<LocalTransform>>())
        //{
        //    ref readonly var knock = ref k.ValueRO;
        //    ref var knockW = ref k.ValueRW;

        //    float power = curveLib.KnockbackCurve.Evaluate(knock.T);
        //    float dist = (dt * knock.Settings.Distance) / knock.Settings.Duration;
        //    float3 move = knock.Settings.dir * power * dist;
        //    transform.ValueRW.Position += move;

        //    knockW.T += dt / knock.Settings.Duration;

        //    // is knockback done?
        //    if (knock.T >= 1f)
        //        toggle.ValueRW = false;
        //}
    }
}
