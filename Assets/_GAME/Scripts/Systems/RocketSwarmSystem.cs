using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[UpdateInGroup(typeof(SkillSystemGroup))]
public partial struct RocketSwarmSystem : ISystem
{
    private float3 playerPos;
    private Random rng;
    private ComponentLookup<LocalTransform> transformLUT;

    public struct Rocket : IComponentData
    {
        public Entity Target;
        public LocalTransform TargetTransform;
        public RocketSettings RocketSettings;
        public float T;
    }

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CurveLib>();
        rng = new Random();
        rng.InitState();
        transformLUT = state.GetComponentLookup<LocalTransform>();
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var time = (float)SystemAPI.Time.ElapsedTime;
        var dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        float3 up = new float3(0, 1, 0);
        var physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>();

        foreach (var transform in SystemAPI.Query<LocalTransform>().WithAll<Player>())
        {
            playerPos = transform.Position;
        }

        transformLUT.Update(ref state);
        var hitList = new NativeList<DistanceHit>(Allocator.Temp);
        var filter = new CollisionFilter
        {
            CollidesWith = 1u<< 1,
            BelongsTo = 1u<< 3,
        };

        foreach (var (qSkill, qAc, qRl) in SystemAPI.Query<RefRW<RocketSwarmSkillData>, SkillActivationData, RefRW<SkillReloadData>>())
        {
            ref readonly var skillData = ref qSkill.ValueRO;
            ref var skillDataW = ref qSkill.ValueRW;
            ref var reloadW = ref qRl.ValueRW;
            bool canShoot = reloadW.MagCountCurrent > 0 && time > skillDataW.TimeStampNextShot;

            if (!canShoot)
                continue;

            // Find targets and shoot
            hitList.Clear();
            physics.OverlapSphere(playerPos, qAc.ActivationRange, ref hitList, filter);
            if (hitList.Length == 0)
                continue;

            var targetEnt = hitList[rng.NextInt(0, hitList.Length)].Entity;
            var targetTransform = transformLUT[targetEnt];
            var rocket = ecb.Instantiate(skillData.RocketPrefab);
            var settings = skillData.Settings;
            //float2 random = rng.NextFloat2() * rng.NextFloat(-15f, 15f);
            //settings.Destination = playerPos + new float3(random.x, 0f, random.y);
            settings.LaunchPoint = playerPos + (up * 4f);
            ecb.SetComponent(rocket, new Rocket
            {
                Target = targetEnt,
                TargetTransform = targetTransform,
                RocketSettings = settings,
                T = 0f
            });
            ecb.SetComponent(rocket, LocalTransform.FromPosition(playerPos + up));
            skillDataW.TimeStampNextShot = time + (1f / skillData.RocketsPerSecond);
            Utils.HandleReload(ref reloadW, 1);
        }

        // move active rockets
        var rocketCurve = SystemAPI.GetSingleton<CurveLib>().RocketHeightCurve;
        foreach (var (qr, transform, entity) in SystemAPI.Query<RefRW<Rocket>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            ref readonly var rocket = ref qr.ValueRO;
            ref var rocketW = ref qr.ValueRW;
            var t = qr.ValueRO.T;
            if (t >= 1f)
            {
                ecb.DestroyEntity(entity);
                continue;
            }

            if (SystemAPI.HasComponent<LocalTransform>(qr.ValueRO.Target)) 
                rocketW.RocketSettings.Destination = qr.ValueRO.TargetTransform.Position;

            var settings = rocket.RocketSettings;
            var curveEval = rocketCurve.GetValueAtFrac(qr.ValueRO.T);
            var height =  curveEval * settings.ApexHeight;
            var pos = transform.ValueRO.Position;
            var lastPos = pos;
            var horizontalLerp = math.lerp(settings.LaunchPoint, settings.Destination, t);
            //var xSin = math.sin((time + entity.Index)* settings.NoiseFreq) * curveEval * settings.NoiseScaleX;
            //var zSin = math.sin(time * settings.NoiseFreq) * curveEval * settings.NoiseScaleZ;
            //horizontalLerp.x += xSin;
            //horizontalLerp.z += zSin;
            pos.y = height;
            pos.xz = horizontalLerp.xz;
            var rot = quaternion.LookRotation(math.normalizesafe(pos - lastPos), up);
            transform.ValueRW.Position = pos;
            transform.ValueRW.Rotation = rot;
            qr.ValueRW.T += dt / settings.FlightTime;
        }

        ecb.Playback(state.EntityManager);
    }
}
