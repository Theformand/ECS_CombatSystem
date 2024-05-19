using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEditor.Rendering;

[UpdateInGroup(typeof(SkillSystemGroup))]
public partial struct RocketSwarmSystem : ISystem
{
    private float3 playerPos;
    private Random rng;

    public struct Rocket : IComponentData
    {
        public RocketSettings RocketSettings;
        public float T;
    }

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Config>();
        rng = new Random();
        rng.InitState();
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var time = (float)SystemAPI.Time.ElapsedTime;
        var dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        float3 up = new float3(0, 1, 0);
        foreach (var (_, transform) in SystemAPI.Query<Player, LocalTransform>())
        {
            playerPos = transform.Position;
        }

        foreach (var (qSkill, _, qRl) in SystemAPI.Query<RefRW<RocketSwarmSkillData>, SkillActivationData, RefRW<SkillReloadData>>())
        {
            ref readonly var skillData = ref qSkill.ValueRO;
            ref var skillDataW = ref qSkill.ValueRW;
            ref var reloadW = ref qRl.ValueRW;
            bool canShoot = reloadW.MagCountCurrent > 0 && time > skillDataW.TimeStampNextShot;

            if (!canShoot)
                continue;


            var rocket = ecb.Instantiate(skillData.RocketPrefab);
            var settings = skillData.Settings;
            float2 random = rng.NextFloat2() * rng.NextFloat(-15f, 15f);
            settings.Destination = playerPos + new float3(random.x, 0f, random.y);
            settings.LaunchPoint = playerPos + (up*4f);
            ecb.SetComponent(rocket, new Rocket
            {
                RocketSettings = settings,
                T = 0f
            });
            ecb.SetComponent(rocket, LocalTransform.FromPosition(settings.LaunchPoint));
            skillDataW.TimeStampNextShot = time + (1f / skillData.RocketsPerSecond);
            Utils.HandleReload(ref reloadW, 1);
        }

        // move active rockets
        var cfg = SystemAPI.GetSingleton<Config>();
        foreach (var (rocket, transform, entity) in SystemAPI.Query<RefRW<Rocket>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            var t = rocket.ValueRO.T;
            if (t >= 1f)
            {
                ecb.DestroyEntity(entity);
                continue;
            }
            var settings = rocket.ValueRO.RocketSettings;
            var height = cfg.RocketHeightCurve.GetValueAtFrac(rocket.ValueRO.T) * settings.ApexHeight;
            var pos = transform.ValueRO.Position;
            var lastPos = pos;
            var horizontalLerp = math.lerp(settings.LaunchPoint, settings.Destination, t);

            pos.y = height;
            pos.xz = horizontalLerp.xz;
            var rot = quaternion.LookRotation(math.normalizesafe(pos - lastPos), up);
            transform.ValueRW.Position = pos;
            transform.ValueRW.Rotation = rot;
            rocket.ValueRW.T += dt / settings.FlightTime;
        }

        ecb.Playback(state.EntityManager);
    }


}
