using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;


public struct EnemyMoveData : IComponentData
{
    public float TurnSpeed;
    public float MoveSpeed;
    public float CatchupMultiplier;
}

public partial struct SeekerEnemySystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var dt = SystemAPI.Time.DeltaTime;
        var playerTransform = SystemAPI.GetComponent<LocalTransform>(SystemAPI.GetSingletonEntity<Player>());
        foreach (var (qEnemy, qMove, t) in SystemAPI.Query<RefRW<EnemyTag>, RefRW<EnemyMoveData>, RefRW<LocalTransform>>().WithNone<KnockBack>())
        {
            ref readonly var move = ref qMove.ValueRO;
            ref var moveW = ref qMove.ValueRW;

            ref readonly var enemy = ref qEnemy.ValueRO;
            ref var enemyW = ref qEnemy.ValueRW;

            ref readonly var transform = ref t.ValueRO;
            ref var transformW = ref t.ValueRW;

            var dirToPlayer = math.normalizesafe(playerTransform.Position - transform.Position);
            float catcUp = move.CatchupMultiplier;
            float speed = move.MoveSpeed * catcUp;
            transformW.RotateY(dt *  move.TurnSpeed);
            transformW.Position += dt * speed;


        }
    }
}

public partial struct MeleeEnemySystem : ISystem
{
    public void OnCreate(ref SystemState state) { }
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

    }
}
