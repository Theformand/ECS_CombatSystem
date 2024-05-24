using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using static PickupAttractorSystem;
using Unity.Physics.Extensions;

public struct PlayerPickupTrigger : IComponentData
{

}

public partial struct PickupTriggerSystem : ISystem
{
    private ComponentLookup<PlayerPickupTrigger> TriggerLUT;
    private ComponentLookup<Pickup> PickupLUT;
    private ComponentLookup<LocalTransform> transformLUT;
    private ComponentLookup<AttractedToPlayer> AttracteesLUT;
    private ComponentLookup<PhysicsCollider> colliderLUT;

    public void OnCreate(ref SystemState state)
    {
        TriggerLUT = state.GetComponentLookup<PlayerPickupTrigger>();
        PickupLUT = state.GetComponentLookup<Pickup>();
        transformLUT = state.GetComponentLookup<LocalTransform>();
        AttracteesLUT = state.GetComponentLookup<AttractedToPlayer>();
        colliderLUT = state.GetComponentLookup<PhysicsCollider>();
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        float3 playerPos = float3.zero;
        foreach (var transform in SystemAPI.Query<LocalTransform>().WithAll<Player>())
        {
            playerPos = transform.Position;
        }

        // Check for picked up magnet entities
        var magnetEcb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (collect, collectEnt) in SystemAPI.Query<CollectAllXP>().WithEntityAccess())
        {
            magnetEcb.DestroyEntity(collectEnt);
            // NON-Archetype-breaking version, not working yet
            //foreach (var (attract, attractEnable, pickup) in SystemAPI.Query<RefRW<AttractedToPlayer>, EnabledRefRW<AttractedToPlayer>, Pickup>().WithOptions(EntityQueryOptions.IncludeDisabledEntities))
            //{
            //    attractEnable.ValueRW = true;
            //    attract.ValueRW.RampUpT = 0f;
            //    attract.ValueRW.RampUpDuration = 1f;
            //    attract.ValueRW.IsXP = !pickup.HasDistanceOverride;
            //}

            foreach (var (pickup, entity) in SystemAPI.Query<Pickup>().WithEntityAccess())
            {
                magnetEcb.AddComponent(entity, new AttractedToPlayer
                {
                    IsXP = pickup.PickupType == PickupType.XP,
                    RampUpDuration = 1f,
                    RampUpT = 0f,
                });
            }
        }
        magnetEcb.Playback(state.EntityManager);


        //Prep for regular trigger job
        TriggerLUT.Update(ref state);
        AttracteesLUT.Update(ref state);
        PickupLUT.Update(ref state);
        transformLUT.Update(ref state);
        colliderLUT.Update(ref state);

        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        SimulationSingleton simulation = SystemAPI.GetSingleton<SimulationSingleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        state.Dependency = new PickupTriggerJob
        {
            ecb = ecb,
            ExistingAttractees = AttracteesLUT,
            TriggerLUT = TriggerLUT,
            Pickups = PickupLUT,
            PickupTransforms = transformLUT,
            ColliderLUT = colliderLUT,
            PlayerPos = playerPos,

        }.Schedule(simulation, state.Dependency);
    }

    [BurstCompile]
    public partial struct PickupTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public float3 PlayerPos;
        [ReadOnly] public ComponentLookup<PlayerPickupTrigger> TriggerLUT;
        [ReadOnly] public ComponentLookup<Pickup> Pickups;
        [ReadOnly] public ComponentLookup<LocalTransform> PickupTransforms;
        [ReadOnly] public ComponentLookup<AttractedToPlayer> ExistingAttractees;
        [ReadOnly] public ComponentLookup<PhysicsCollider> ColliderLUT;
        //[ReadOnly] public ComponentLookup<EnabledRefRW<AttractedToPlayer>> ExistingAttracteesEnable;

        public EntityCommandBuffer ecb;

        [BurstCompile]
        public void Execute(TriggerEvent triggerEvent)
        {

            Entity pickup;

            if (TriggerLUT.HasComponent(triggerEvent.EntityA) && Pickups.HasComponent(triggerEvent.EntityB))
                pickup = triggerEvent.EntityB;
            else if (TriggerLUT.HasComponent(triggerEvent.EntityB) && Pickups.HasComponent(triggerEvent.EntityA))
                pickup = triggerEvent.EntityA;
            else
                return;

            bool closeEnough = true;
            float distanceToPlayerSqr = math.lengthsq(PickupTransforms[pickup].Position - PlayerPos);
            var pickupData = Pickups[pickup];
            if (pickupData.HasDistanceOverride && pickupData.PickupDistanceOverrideSqr < distanceToPlayerSqr)
                closeEnough = false;

            // Ignore already attracted pickups
            if (ExistingAttractees.HasComponent(pickup) || !closeEnough)
                return;

            bool isXP = !pickupData.HasDistanceOverride;
            // Replace collisionFilter on larger pickups so they dont scrape along the ground
            if (!isXP)
            {
                var collider = ColliderLUT[pickup];
                BlobAssetReference<Collider> ColliderBlobReference = collider.Value.Value.Clone();
                CollisionFilter oldFilter = collider.Value.Value.GetCollisionFilter();
                PhysicsCollider newCollider;

                ColliderBlobReference.Value.SetCollisionFilter(new CollisionFilter
                {
                    BelongsTo = 0b_0000_0000_0000_0000_0000_0000_0000_0000,
                    CollidesWith = 0b_0000_0000_0000_0000_0000_0000_0000_0000,
                    GroupIndex = oldFilter.GroupIndex,
                });
                newCollider = ColliderBlobReference.AsComponent();
                ecb.SetComponent(pickup, newCollider);
            }
            ecb.AddComponent(pickup, new AttractedToPlayer
            {
                RampUpT = 0f,
                RampUpDuration = 1f,
                IsXP = isXP,
            });

            if (Hint.Likely(isXP))
            {
                ecb.SetComponent(pickup, new PhysicsVelocity
                {
                    Angular = new float3(1f, 2f, 3f) * 10f,
                });
            }

        }
    }
}

public struct AttractedToPlayer : IComponentData, IEnableableComponent
{
    public float RampUpT;
    public float RampUpDuration;
    public float Vel;
    public bool IsXP;
}


public partial struct PickupAttractorSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<CurveLib>();
        state.RequireForUpdate<Player>();
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float3 playerPos = float3.zero;
        foreach (var transform in SystemAPI.Query<LocalTransform>().WithAll<Player>())
        {
            playerPos = transform.Position;
        }
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        state.Dependency = new AttractPickupsJob
        {
            XPAttractSpeedCurve = SystemAPI.GetSingleton<CurveLib>().XPPickupVelocityCurve,
            AttractSpeedCurve = SystemAPI.GetSingleton<CurveLib>().PickupVelocityCurve,
            Dt = SystemAPI.Time.DeltaTime,
            PlayerPos = playerPos,
            ecb = ecb,

        }.Schedule(state.Dependency);
    }

    public struct CollectAllXP : IComponentData
    {

    }

    [BurstCompile]
    public partial struct AttractPickupsJob : IJobEntity
    {
        [ReadOnly] public float3 PlayerPos;
        [ReadOnly] public float Dt;
        [ReadOnly] public DotsCurve XPAttractSpeedCurve;
        [ReadOnly] public DotsCurve AttractSpeedCurve;
        public EntityCommandBuffer.ParallelWriter ecb;

        [BurstCompile]
        public void Execute(
            [EntityIndexInQuery] int sortkey,
            ref AttractedToPlayer attract,
            ref PhysicsVelocity velocity,
            in LocalTransform transform,
            in Pickup pickup,
            in Entity entity)
        {
            const float collectRadius = 1f;
            const float speed = 35f;
            var t = attract.RampUpT;
            t += (Dt / attract.RampUpDuration);
            t = math.clamp(t, 0f, 1f);
            var curve = attract.IsXP ? XPAttractSpeedCurve : AttractSpeedCurve;
            attract.Vel = curve.Evaluate(t) * speed;
            attract.RampUpT = t;
            var dirToPlayer = PlayerPos - transform.Position;

            if (Hint.Likely(math.lengthsq(dirToPlayer) > collectRadius))
            {
                velocity.Linear = math.normalizesafe(dirToPlayer) * attract.Vel;
            }
            else
            {
                ecb.DestroyEntity(sortkey, entity);
                if (pickup.PickupType == PickupType.Magnet)
                {
                    var ent = ecb.CreateEntity(sortkey);
                    ecb.AddComponent(sortkey, ent, new CollectAllXP { });
                }
            }
        }
    }
}
