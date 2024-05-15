using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public partial struct GrenadeSkillSystem : ISystem
{
    private EntityQuery entityQuery;
    private float3 playerPos;
    private float3 up;

    public void OnCreate(ref SystemState state)
    {
        entityQuery = state.GetEntityQuery(typeof(EnemyTag), typeof(LocalTransform));
        up = new float3(0f, 1f, 0f);

    }
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        NativeArray<LocalTransform> allTransforms = entityQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        foreach (var (_, transform) in SystemAPI.Query<Player, LocalTransform>())
        {
            playerPos = transform.Position;
        }

        foreach (var (rl, skillData, ac) in SystemAPI.Query<RefRW<SkillReloadData>, GrenadeSkillData, RefRW<SkillActivationData>>())
        {
            ref readonly var reload = ref rl.ValueRO;
            ref var reloadW = ref rl.ValueRW;
            ref readonly var activation = ref ac.ValueRO;
            ref var activationW = ref ac.ValueRW;
            LocalTransform target = LocalTransform.Identity;
            bool targetFound = false;

            if (reload.TimeCurrent > 0f || reload.MagCountCurrent <=0)
                continue;
            
            if (activation.TargetingMode == SkillTargetingMode.CLOSEST)
            {
                int idxTarget = Utils.GetIndexOfClosestWithLOS(ref allTransforms, ref playerPos, activation.ActivationRangeSqr, ref physicsWorld);
                if (idxTarget == -1)
                    continue;

                target = allTransforms[idxTarget];
                targetFound = true;
            }
            if (targetFound)
            {
                var dir = math.normalizesafe(target.Position - playerPos);
                var grenade = ecb.Instantiate(skillData.GrenadePrefab);
                dir.x *= skillData.ThrowForce;
                dir.y *= skillData.ThrowUpForce;
                dir.z *= skillData.ThrowForce;
                ecb.SetComponent(grenade, LocalTransform.FromPositionRotationScale(playerPos + up, quaternion.identity, 0.35f));
                ecb.SetComponent(grenade, new PhysicsVelocity { Linear = dir });
                ecb.SetComponent(grenade, new GrenadeData
                {
                    DamageAtCenter = skillData.DamageAtCenter,
                    ExplosionRadius = skillData.ExplosionRadius,
                    LifeTime = skillData.LifeTime
                });
                reloadW.MagCountCurrent--;
                if (reloadW.MagCountCurrent <= 0)
                    reloadW.TimeCurrent = reload.Time;
            }
        }
        ecb.Playback(state.EntityManager);
        allTransforms.Dispose();
    }
    
}

public partial struct GrenadeLifeTimeSystem : ISystem
{
    private ComponentLookup<EnemyTag> enemyLUT;
    private CollisionFilter filter;

    public void OnCreate(ref SystemState state)
    {
        enemyLUT = SystemAPI.GetComponentLookup<EnemyTag>();
        filter = new CollisionFilter()
        {
            BelongsTo = ~0u,
            CollidesWith = 1u << 1,
            GroupIndex = 0
        };
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        enemyLUT.Update(ref state);
        var hits = new NativeList<DistanceHit>(32, Allocator.Temp);
        foreach (var (grenade, transform, entity) in SystemAPI.Query<RefRW<GrenadeData>, LocalTransform>().WithEntityAccess())
        {
            grenade.ValueRW.LifeTime -= dt;

            if (grenade.ValueRO.LifeTime <= 0f)
            {
                if(physics.OverlapSphere(transform.Position, grenade.ValueRO.ExplosionRadius, ref hits, filter))
                {
                    for (int i = 0; i < hits.Length; i++)
                    {
                        var ent = hits[i].Entity;
                        if (enemyLUT.HasComponent(ent))
                        {
                            ecb.AddComponent(ent, new DamageData()
                            {
                                DamageType = grenade.ValueRO.DamageType,
                                Damage = grenade.ValueRO.DamageAtCenter
                            });
                        }
                    }
                }
                ecb.DestroyEntity(entity);
            }
        }
        ecb.Playback(state.EntityManager);
        hits.Dispose();
    }
}
