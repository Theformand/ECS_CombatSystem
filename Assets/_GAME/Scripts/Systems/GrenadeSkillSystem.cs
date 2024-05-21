using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using Unity.Mathematics;


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

            if (reload.TimeCurrent > 0f || reload.MagCountCurrent <= 0)
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
                dir.y = 0f;
                var grenade = ecb.Instantiate(skillData.GrenadePrefab);
                var settings = skillData.GrenadeSettings;
                dir.x *= settings.ThrowForce;
                dir.y = settings.ThrowUpForce;
                dir.z *= settings.ThrowForce;
                ecb.SetComponent(grenade, LocalTransform.FromPositionRotationScale(playerPos + up, quaternion.identity, 0.35f));
                ecb.SetComponent(grenade, new PhysicsVelocity { Linear = dir });
                ecb.SetComponent(grenade, new GrenadeData
                {
                    LifeTime = settings.LifeTime,
                    GrenadeSettings = settings,
                    Cluster = settings.Cluster,
                    ClusterGrenade = skillData.ClusterGrenade,
                });

                Utils.HandleReload(ref reloadW);
            }
        }
        ecb.Playback(state.EntityManager);
        allTransforms.Dispose();
    }
}

[UpdateInGroup(typeof(SkillSystemGroup))]
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

    public void OnDestroy(ref SystemState state) 
    {
        state.RequireForUpdate<VFXLibrary>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        enemyLUT.Update(ref state);
        var hits = new NativeList<DistanceHit>(32, Allocator.Temp);
        foreach (var (qgrenade, transform, entity) in SystemAPI.Query<RefRW<GrenadeData>, LocalTransform>().WithEntityAccess())
        {
            ref readonly var grenade = ref qgrenade.ValueRO;
            ref var grenadeW = ref qgrenade.ValueRW;
            grenadeW.LifeTime -= dt;
            var pos = transform.Position;

            //cluster
            if (grenade.Cluster && (transform.Position.y < .98f || grenade.LifeTime <= 0f))
            {
                var ent = ecb.CreateEntity();
                ecb.AddComponent(ent, new SpawnClusterGrenades
                {
                    GrenadeSettings = grenade.GrenadeSettings,
                    GrenadePrefab = grenade.ClusterGrenade,
                    Position = transform.Position,
                    LifeTime = grenade.LifeTime +3f
                });

                ecb.DestroyEntity(entity);
                continue;
            }

            if (grenade.LifeTime <= 0f)
            {
                var rot = quaternion.identity;
                switch (grenade.GrenadeSettings.ExplosionType)
                {
                    case GrenadeExplosionType.Explosion:
                        Explode(ref grenadeW, transform.Position, ref hits, ref filter, ref ecb, ref physics);
                        Utils.SpawnVFX(VFXPrefabType.Grenade_Explosion_HE, ref pos, ref rot, 1f, ref ecb);
                        ecb.DestroyEntity(entity);
                        break;
                    case GrenadeExplosionType.SpinningBullets:
                        break;
                    case GrenadeExplosionType.BulletBloom:
                        ecb.DestroyEntity(entity);
                        break;
                    default:
                        break;
                }

            }
        }
        ecb.Playback(state.EntityManager);
        hits.Dispose();
    }

    public partial struct SpawnClusterGrenadeSystem : ISystem
    {
        private Unity.Mathematics.Random rng;

        public void OnCreate(ref SystemState state)
        {
            rng = new Unity.Mathematics.Random();
            rng.InitState();
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (qc, entity) in SystemAPI.Query<RefRW<SpawnClusterGrenades>>().WithEntityAccess())
            {
                ref readonly var cluster = ref qc.ValueRO;
                var settings = cluster.GrenadeSettings;
                var ang = 360f / settings.NumClusterGrenades;
                var angRotation = rng.NextFloat(0,360f);
                for (int i = 0; i < settings.NumClusterGrenades; i++)
                {
                    settings.Cluster = false;
                    var rngOffset = rng.NextFloat(0f, 60f);
                    
                    var rads = math.radians((i * ang) + rngOffset + angRotation);
                    var dir = float3.zero;
                    dir.x = math.cos(rads);
                    dir.z = math.sin(rads);
                    dir = math.normalize(dir);

                    var grenade = ecb.Instantiate(cluster.GrenadePrefab);
                    float scalar = .3f;
                    dir.x *= settings.ThrowForce * scalar + rng.NextFloat(0f, 2f);
                    dir.z *= settings.ThrowForce * scalar + rng.NextFloat(0f, 2f);
                    dir.y = 5f + rng.NextFloat(0f, 2f);
                    var newSettings = new GrenadeSettings()
                    {
                        Cluster = false,
                        DamageAtCenter = settings.DamageAtCenter,
                        DamageType = settings.DamageType,
                        ExplosionRadius = settings.ExplosionRadius,
                        ExplosionType = settings.ExplosionType,
                        LifeTime = cluster.LifeTime
                    };

                    ecb.SetComponent(grenade, LocalTransform.FromPositionRotationScale(cluster.Position, quaternion.identity, 0.35f));
                    ecb.SetComponent(grenade, new PhysicsVelocity { Linear = dir });
                    ecb.SetComponent(grenade, new GrenadeData
                    {
                        GrenadeSettings = newSettings,
                        LifeTime = cluster.LifeTime,
                    });
                }
                ecb.DestroyEntity(entity);
            }
            ecb.Playback(state.EntityManager);
        }
    }

    [BurstCompile]
    private void Explode(ref GrenadeData grenade, float3 position, ref NativeList<DistanceHit> hits, ref CollisionFilter filter, ref EntityCommandBuffer ecb, ref PhysicsWorldSingleton physics)
    {
        if (physics.OverlapSphere(position, grenade.GrenadeSettings.ExplosionRadius, ref hits, filter))
        {
            for (int i = 0; i < hits.Length; i++)
            {
                var ent = hits[i].Entity;
                if (enemyLUT.HasComponent(ent))
                {
                    ecb.AddComponent(ent, new DamageData()
                    {
                        DamageType = grenade.GrenadeSettings.DamageType,
                        Damage = grenade.GrenadeSettings.DamageAtCenter
                    });
                }
            }
        }
    }
}
