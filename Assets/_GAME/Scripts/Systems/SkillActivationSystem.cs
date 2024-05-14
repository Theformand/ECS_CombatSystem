using System.Diagnostics;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

[UpdateInGroup(typeof(SkillSystemGroup))]
public partial struct SkillActivationSystem : ISystem
{
    private EntityQuery entityQuery;
    private NativeArray<LocalTransform> allTransforms;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
        entityQuery = state.GetEntityQuery(typeof(EnemyTag), typeof(LocalTransform));
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float3 playerPos = float3.zero;
        float3 up = new(0, 1, 0);
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (player, transform) in SystemAPI.Query<Player, LocalTransform>())
        {
            playerPos = transform.Position;
        }
        var time = (float)SystemAPI.Time.ElapsedTime;
        NativeArray<LocalTransform> allTransforms = entityQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        NativeArray<EnemyTag> allEnemies = entityQuery.ToComponentDataArray<EnemyTag>(Allocator.Temp);

        foreach (var (qshotData, reload, activationData) in SystemAPI.Query<RefRW<BulletSkillShotData>, RefRW<SkillReloadData>, SkillActivationData>())
        {
            ref readonly var shotData = ref qshotData.ValueRO;
            ref var shotDataW = ref qshotData.ValueRW;

            var isReloaded = reload.ValueRO.TimeCurrent <= 0f;
            bool canShoot = time > shotData.TimeStampNextShot  && reload.ValueRO.MagCountCurrent > 0;
            if (!canShoot)
                continue;

            bool targetFound = false;
            LocalTransform target = LocalTransform.Identity;

            // Aqcuire target based on the targeting mode of the skill
            if (activationData.TargetingMode == SkillTargetingMode.CLOSEST)
            {
                int idxTarget = GetIndexOfClosestWithLOS(allTransforms, playerPos, activationData.ActivationRangeSqr, physicsWorld);
                if (idxTarget == -1)
                    continue;

                target = allTransforms[idxTarget];
                targetFound = true;

            }
            else if (activationData.TargetingMode == SkillTargetingMode.HIGHEST_HP_MAX)
            {
                // - Find max base HP for all enemies in range
                // - Find all enemies in range with this maxHP value
                // - Pick the closest one of those

                NativeList<LocalTransform> maxHPTransforms = new NativeList<LocalTransform>(allEnemies.Length, Allocator.Temp);
                int highestMax = 0;
                //If I was a half decent programmer I could probably do without 2 loops
                for (int i = 0; i < allEnemies.Length; i++)
                {
                    var curr = allEnemies[i].HPMax;
                    if (curr > highestMax)
                        highestMax = curr;
                }

                for (int i = 0; i < allEnemies.Length; i++)
                {
                    if (allEnemies[i].HPMax == highestMax)
                        maxHPTransforms.Add(allTransforms[i]);
                }

                int idxTarget = GetIndexOfClosestWithLOS(maxHPTransforms.AsArray(), playerPos, activationData.ActivationRangeSqr, physicsWorld);
                if (idxTarget == -1)
                    continue;

                target = allTransforms[idxTarget];
                targetFound = true;

                maxHPTransforms.Dispose();
            }

            if (isReloaded && targetFound)
            {
                //TODO: Figure out how to notify GO land that we need Audio here

                //we are reloaded and we found a target
                var aimDir = math.normalizesafe(target.Position - playerPos);
                for (int i = 0; i < shotData.NumBulletsPerAttack; i++)
                {
                    float3 dir = float3.zero;
                    if (shotData.NumBulletsPerAttack == 1)
                    {
                        dir = aimDir;
                    }
                    else
                    {
                        float startingAngle = (shotData.NumBulletsPerAttack - 1) * (shotData.AngleSpread * 0.5f);
                        float angle = -startingAngle + (i * shotData.AngleSpread);
                        var rotOffset = quaternion.AxisAngle(up, math.radians(angle));
                        dir = math.mul(rotOffset, aimDir);
                    }

                    var bullet = ecb.Instantiate(shotData.BulletPrefab);
                    var pos = playerPos + up;
                    var rot = quaternion.LookRotationSafe(dir, up);
                    ecb.SetComponent(bullet, LocalTransform.FromPositionRotation(pos, rot));
                    ecb.AddComponent(bullet, new DamageData() { Damage = shotData.DamageCurrent, DamageType = shotData.DamageType });
                    ecb.AddComponent(bullet, new SkillMoveSpeed { Speed = shotData.BulletMoveSpeed });
                    if (shotData.Pierce != 0)
                        ecb.AddComponent(bullet, new DestroyAfterPierce() { PierceCurrent = shotData.Pierce });

                    if (shotData.Lifetime > 0f)
                        ecb.AddComponent(bullet, new DestroyOnTimer() { Time = shotData.Lifetime });  

                }
                shotDataW.TimeStampNextShot = time + 1f / shotData.AttacksPerSecond;
                reload.ValueRW.MagCountCurrent--;
                if (reload.ValueRO.MagCountCurrent == 0)
                {
                    reload.ValueRW.TimeCurrent = reload.ValueRO.Time;
                }
            }
        }

        ecb.Playback(state.EntityManager);
        allEnemies.Dispose();
        allTransforms.Dispose();
    }

    [BurstCompile]
    private int GetIndexOfClosestWithLOS(NativeArray<LocalTransform> transforms, float3 referencePoint, float rangeSqr, PhysicsWorld physicsWorld)
    {
        float closestDist = 100000f;
        int idxClosest = -1;
        var filter = new CollisionFilter()
        {
            CollidesWith = 1u<<2, //Environment layer
            BelongsTo = 1u << 2, //Environment layer - should probably just be ~0u
            GroupIndex = 0
        };
        float3 up = new(0f, 1f, 0f);

        for (int i = 0; i < transforms.Length; i++)
        {
            var pos = transforms[i].Position;
            var raycastInput = new RaycastInput()
            {
                Start = referencePoint + up,
                End = pos + up,
                Filter = filter
            };

            var distSqr = math.distancesq(referencePoint, pos);
            if (distSqr < rangeSqr && distSqr < closestDist && !physicsWorld.CastRay(raycastInput))
            {
                closestDist = distSqr;
                idxClosest = i;
            }
        }
        return idxClosest;
    }
}
