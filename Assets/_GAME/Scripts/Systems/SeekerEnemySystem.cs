using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;


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
        foreach (var (qEnemy, move, t) in SystemAPI.Query<RefRW<Enemy>, EnemyMoveData, RefRW<LocalTransform>>().WithNone<KnockBack>())
        {

            ref readonly var enemy = ref qEnemy.ValueRO;
            ref var enemyW = ref qEnemy.ValueRW;

            ref readonly var transform = ref t.ValueRO;
            ref var transformW = ref t.ValueRW;

            var dirToPlayer = math.normalizesafe(playerTransform.Position - transform.Position);
            dirToPlayer.y = 0f;
            var rot = quaternion.LookRotationSafe(dirToPlayer, math.up());


            float catcUp = move.CatchupMultiplier;
            float speed = move.MoveSpeed * catcUp;
            transformW.Rotation = math.nlerp(transform.Rotation, rot, dt * move.TurnSpeed);
            transformW.Position += transform.Forward() * dt * speed;
        }
    }
}

public struct MeleeSkillData : IComponentData
{
    public float AttackRangeSqr;
    public float AttackInterval;
    public float TimeStampNextAttack;
}

public partial struct MeleeEnemySystem : ISystem
{
    public void OnCreate(ref SystemState state) 
    {
        state.RequireForUpdate<Player>();
    }
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var player = SystemAPI.GetSingletonEntity<Player>(); 
        var playerTransform = SystemAPI.GetComponent<LocalTransform>(player);
        var time = (float)SystemAPI.Time.ElapsedTime;

        foreach (var (msd, transform) in SystemAPI.Query<RefRW<MeleeSkillData>, LocalTransform>().WithAll<Enemy>())
        {
            ref readonly var skillData = ref msd.ValueRO;
            ref var skillDataW = ref msd.ValueRW;
            if (math.distancesq(playerTransform.Position, transform.Position) < skillData.AttackRangeSqr && time > skillData.TimeStampNextAttack)
            {

                // do attack
                skillDataW.TimeStampNextAttack = time + skillData.AttackInterval; 
            }
        }
    }
}
