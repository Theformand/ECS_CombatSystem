using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;
using Unity.Transforms;

public struct Ricochet : IComponentData
{

}


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(PhysicsSystemGroup))]
public partial struct RicochetSystem : ISystem
{
    private ComponentLookup<Projectile> projectileLUT;
    private ComponentLookup<MineableBlock> blockLUT;
    private ComponentLookup<Ricochet> ricochetLUT;
    private ComponentLookup<LocalTransform> transformLUT;

    public void OnCreate(ref SystemState state)
    {
        projectileLUT = SystemAPI.GetComponentLookup<Projectile>();
        blockLUT = SystemAPI.GetComponentLookup<MineableBlock>();
        ricochetLUT = SystemAPI.GetComponentLookup<Ricochet>();
        transformLUT = SystemAPI.GetComponentLookup<LocalTransform>();
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        blockLUT.Update(ref state);
        projectileLUT.Update(ref state);
        ricochetLUT.Update(ref state);
        transformLUT.Update(ref state);
        var ecb = SystemAPI.GetSingleton<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        var world = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        var job = new RicochetCollisionJob
        {
            PhysicsWorld = world,
            ProjectileLUT = projectileLUT,
            WallLUT = blockLUT,
            RicochetLUT = ricochetLUT,
            TransformLUT = transformLUT,
            ECB = ecb,
        };
        state.Dependency = job.Schedule(SystemAPI.GetSingleton<SimulationSingleton>(), state.Dependency);
    }

    public struct RicochetCollisionJob : ICollisionEventsJob
    {
        public ComponentLookup<Projectile> ProjectileLUT;
        public ComponentLookup<MineableBlock> WallLUT;
        public ComponentLookup<Ricochet> RicochetLUT;
        public ComponentLookup<LocalTransform> TransformLUT;
        public PhysicsWorld PhysicsWorld;
        public EntityCommandBuffer ECB;

        public void Execute(CollisionEvent evt)
        {
            Entity projEnt;
            Entity wallEnt;
            int bodyIdx;
            float3 projVel;

            if (ProjectileLUT.HasComponent(evt.EntityA) && WallLUT.HasComponent(evt.EntityB))
            {
                projEnt = evt.EntityA;
                wallEnt = evt.EntityB;
                bodyIdx = evt.BodyIndexA;
            }
            else if (ProjectileLUT.HasComponent(evt.EntityB) && WallLUT.HasComponent(evt.EntityA))
            {
                projEnt = evt.EntityB;
                wallEnt = evt.EntityA;
                bodyIdx = evt.BodyIndexB;
            }
            else
                return;

            if (!RicochetLUT.HasComponent(projEnt))
                return;

            var projectile = ProjectileLUT[projEnt];
            //var block = WallLUT[wallEnt];

            // TODO : if first hit (use a hitlist)
            var normal = evt.Normal;
            var incidence = math.normalizesafe(projectile.Heading);
            var currentSpeed = math.length(projectile.Heading);
            var reflected = math.reflect(incidence, normal);
            projectile.Heading = reflected * currentSpeed;
            var pos = TransformLUT[projEnt].Translate(reflected * 0.1f);
            pos.Rotation = quaternion.LookRotation(reflected, math.up());

            ECB.SetComponent(projEnt, new PhysicsVelocity
            {
                Linear = math.normalizesafe(reflected) * currentSpeed,
                Angular = float3.zero
            });
            ECB.SetComponent(projEnt, pos);

            //PhysicsWorld.SetLinearVelocity(bodyIdx, math.normalizesafe(reflected) * currentSpeed);
            //PhysicsWorld.SetAngularVelocity(bodyIdx, float3.zero);
        }
    }
}
