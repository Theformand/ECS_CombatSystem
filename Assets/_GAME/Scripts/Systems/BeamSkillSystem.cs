using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Debug = UnityEngine.Debug;

[UpdateInGroup(typeof(SkillSystemGroup))]
public partial struct BeamSkillSystem : ISystem
{
    private float3 up;
    private ComponentLookup<EnemyTag> enemyLUT;

    public void OnCreate(ref SystemState state)
    {
        up = new float3(0, 1, 0);
        enemyLUT = state.GetComponentLookup<EnemyTag>(true);
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float3 playerPos = float3.zero;

        foreach (var (_, transform) in SystemAPI.Query<Player, LocalTransform>())
        {
            playerPos = transform.Position + up;
        }

        var dt = SystemAPI.Time.DeltaTime;
        var time = ((float)SystemAPI.Time.ElapsedTime);
        var physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        var filter = new CollisionFilter()
        {
            CollidesWith = 1u << 1, //Enemy layer
            BelongsTo = ~0u,
            GroupIndex = 0
        };
        NativeList<RaycastHit> hits = new(Allocator.Temp);

        enemyLUT.Update(ref state);
        foreach (var (rl, qb) in SystemAPI.Query<RefRW<SkillReloadData>, RefRW<BeamSkillData>>())
        {
            ref readonly var beamData = ref qb.ValueRO;
            ref var beamDataW = ref qb.ValueRW;
            ref readonly var reload = ref rl.ValueRO;
            ref var reloadW = ref rl.ValueRW;

            var isReloaded = reload.TimeCurrent <= 0f;
            if (!isReloaded)
                continue;

            var startAngle = beamData.AngleCurrent + beamData.BeamRotationSpeedCurrent * dt;
            float angleBetween = 360f / beamData.BeamCountCurrent;
            for (int i = 0; i < beamData.BeamCountCurrent; i++)
            {
                // figure out the angle offset for this beam, and construct a direction vector from it
                var angle = (angleBetween * i) + startAngle;
                var dir = float3.zero;
                var rads = math.radians(angle);
                dir.x = math.cos(rads);
                dir.z = math.sin(rads);
                dir = math.normalize(dir);
                var start = playerPos + dir * 0.2f;
                var end = start + dir * beamData.BeamLengthCurrent;
                Debug.DrawLine(start, end);


                if (beamData.TimeStampNextTick < time)
                {
                  

                    var raycastInput = new RaycastInput()
                    {
                        Start = start,
                        End = end,
                        Filter = filter
                    };

                    if (physics.CastRay(raycastInput, ref hits))
                    {
                        for (int j = 0; j < hits.Length; j++)
                        {
                            var ent = hits[j].Entity;
                            if (enemyLUT.HasComponent(ent))
                            {
                                ecb.AddComponent(ent, new DamageData()
                                {
                                    DamageType = beamData.DamageType,
                                    Damage = beamData.DamageCurrent
                                });
                            }
                        }
                    }
                    // Reset dmg tick on the last beam
                    if (i == beamData.BeamCountCurrent - 1)
                        beamDataW.TimeStampNextTick = time + (1f / beamData.TicksPerSecond);
                }
            }

            if (startAngle > 360f)
                startAngle = 0f;

            beamDataW.AngleCurrent = startAngle;


            //reload
            beamDataW.LifetimeCurrent -= dt;
            if (beamData.LifetimeCurrent <= 0f)
            {
                beamDataW.LifetimeCurrent = beamData.LifeTime;
                reloadW.TimeCurrent = reload.Time;
            }

        }
        ecb.Playback(state.EntityManager);
        hits.Dispose();
    }
}
