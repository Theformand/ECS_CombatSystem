using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;

public struct PlayerPickupTrigger : IComponentData
{

}

public partial struct PickupTriggerSystem : ISystem
{
    private ComponentLookup<PlayerPickupTrigger> TriggerLUT;
    private ComponentLookup<Pickup> PickupLUT;
    private ComponentLookup<AttractedToPlayer> AttracteesLUT;

    public void OnCreate(ref SystemState state)
    {
       TriggerLUT = state.GetComponentLookup<PlayerPickupTrigger>();
       PickupLUT = state.GetComponentLookup<Pickup>();
       AttracteesLUT = state.GetComponentLookup<AttractedToPlayer>();
}

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        TriggerLUT.Update(ref state);
        AttracteesLUT.Update(ref state);
        PickupLUT.Update(ref state);

        SimulationSingleton simulation = SystemAPI.GetSingleton<SimulationSingleton>();
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        state.Dependency = new PickupTriggerJob
        {
            ecb = ecb,
            ExistingAttractees = AttracteesLUT,
            TriggerLUT = TriggerLUT,
            Pickups = PickupLUT,
            
        }.Schedule(simulation, state.Dependency);
    }

    [BurstCompile]
    public partial struct PickupTriggerJob : ITriggerEventsJob
    {
        public ComponentLookup<PlayerPickupTrigger> TriggerLUT;
        public ComponentLookup<Pickup> Pickups;
        public ComponentLookup<AttractedToPlayer> ExistingAttractees;
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

            // Ignore already attracted pickups
            if (ExistingAttractees.HasComponent(pickup))
                return;

            ecb.AddComponent(pickup, new AttractedToPlayer
            {
                RampUpT = 0f,
                RampUpDuration = 1f
            });
            ecb.SetComponent(pickup, new PhysicsVelocity
            {
                Angular = new float3(1f, 2f, 3f) * 10f,
            });

        }
    }
}

public struct AttractedToPlayer : IComponentData
{
    public float RampUpT;
    public float RampUpDuration;
    public float Vel;
}


public partial struct PickupAttractorSystem : ISystem
{
    public void OnCreate(ref SystemState state) 
    {
        state.RequireForUpdate<CurveLib>();
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
            AttractSpeedCurve = SystemAPI.GetSingleton<CurveLib>().PickupVelocityCurve,
            Dt = SystemAPI.Time.DeltaTime,
            PlayerPos = playerPos,
            ecb = ecb,

        }.Schedule(state.Dependency); 
    }

    [BurstCompile]
    public partial struct AttractPickupsJob : IJobEntity
    {
        [ReadOnly] public float3 PlayerPos;
        [ReadOnly] public float Dt;
        [ReadOnly] public DotsCurve AttractSpeedCurve;
        public EntityCommandBuffer.ParallelWriter ecb;

        [BurstCompile]
        public void Execute([EntityIndexInQuery] int sortkey, ref AttractedToPlayer attract, in LocalTransform transform, ref PhysicsVelocity velocity, in Entity entity)
        {
            const float collectRadius = 1f;
            const float speed = 35f;
            var t = attract.RampUpT;
            t += (Dt / attract.RampUpDuration);
            t = math.clamp(t, 0f, 1f);
            attract.Vel = AttractSpeedCurve.GetValueAtFrac(t) * speed;
            attract.RampUpT = t;
            var dirToPlayer = PlayerPos - transform.Position;

            if (math.lengthsq(dirToPlayer) < collectRadius)
                ecb.DestroyEntity(sortkey, entity);
            else
                 velocity.Linear = math.normalizesafe(dirToPlayer) * attract.Vel;
        }
    }
}
