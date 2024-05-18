using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Mathematics;
using System;


partial struct MoveForwardSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (move, transform) in SystemAPI.Query<SkillMoveSpeed, RefRW<LocalTransform>>())
        {
            transform.ValueRW.Position += transform.ValueRO.Forward() * move.Speed * SystemAPI.Time.DeltaTime;
        }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {

    }
}

public partial struct DestroyEntitySystem : ISystem
{
    public void OnCreate(ref SystemState state) { }
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (_, entity) in SystemAPI.Query<DestroyEntityTag>().WithEntityAccess())
        {
            ecb.DestroyEntity(entity);
        }
        ecb.Playback(state.EntityManager);
    }
}


public partial struct DestroyOnTimerSystem : ISystem
{
    public void OnCreate(ref SystemState state) { }
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        var dt = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        foreach (var (timer, entity) in SystemAPI.Query<RefRW<DestroyOnTimer>>().WithEntityAccess())
        {
            timer.ValueRW.Time -= dt;
            if (timer.ValueRO.Time < 0f)
                ecb.AddComponent<DestroyEntityTag>(entity);
        }
        ecb.Playback(state.EntityManager);
    }
}


[UpdateInGroup(typeof(SkillSystemGroup), OrderFirst = true)]
public partial struct SkillReloadSystem : ISystem
{
    public void OnCreate(ref SystemState state) { }
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        foreach (var rl in SystemAPI.Query<RefRW<SkillReloadData>>())
        {
            var dt = SystemAPI.Time.DeltaTime;
            if (rl.ValueRW.TimeCurrent > 0f)
                rl.ValueRW.TimeCurrent -= dt;
            else if (rl.ValueRW.MagCountCurrent == 0)
                rl.ValueRW.MagCountCurrent = rl.ValueRO.MagCountBase;
        }
    }
}


[BurstCompile]
public class Utils
{
    public struct ShootBulletArgs
    {
        public Entity BulletPrefab;
        public float3 Origin;
        public float3 Direction;
        public int Damage;
        public DamageType DamageType;
        public float MoveSpeed;
        public float Lifetime;
        public int Pierce;

        public static ShootBulletArgs FromBulletSkillData(BulletSkillData data)
        {
            return new ShootBulletArgs
            {
                BulletPrefab = data.BulletPrefab,
                Damage = data.Damage,
                DamageType = data.DamageType,
                Lifetime = data.Lifetime,
                MoveSpeed = data.BulletMoveSpeed,
                Pierce = data.Pierce,
            };
        }
 
    }

    [BurstCompile]
    public static void ShootBulletInDirection(ref ShootBulletArgs args, ref EntityCommandBuffer ecb)
    {

        var bullet = ecb.Instantiate(args.BulletPrefab);
        var pos = args.Origin;
        var rot = quaternion.LookRotationSafe(args.Direction, new float3(0,1,0));
        ecb.SetComponent(bullet, LocalTransform.FromPositionRotation(pos, rot));
        ecb.AddComponent(bullet, new DamageData() { Damage = args.Damage, DamageType = args.DamageType });
        ecb.AddComponent(bullet, new SkillMoveSpeed { Speed = args.MoveSpeed });
        if (args.Pierce != 0)
            ecb.AddComponent(bullet, new DestroyAfterPierce() { PierceCurrent = args.Pierce });

        if (args.Lifetime > 0f)
            ecb.AddComponent(bullet, new DestroyOnTimer() { Time = args.Lifetime });

    }

    [BurstCompile]
    public static int GetIndexOfClosestWithLOS(ref NativeArray<LocalTransform> transforms,
                                               ref float3 referencePoint,
                                               float rangeSqr,
                                               ref PhysicsWorld physicsWorld)
    {
        float closestDist = 100000f;
        int idxClosest = -1;
        var filter = new CollisionFilter()
        {
            CollidesWith = 1u << 2, //Environment layer
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

    [BurstCompile] 
    public static void HandleReload(ref SkillReloadData reload, int ammoConsumed = 1)
    {
        reload.MagCountCurrent -= ammoConsumed;
        if (reload.MagCountCurrent <= 0)
            reload.TimeCurrent = reload.Time;
    }
}


[UpdateInGroup(typeof(SkillSystemGroup))]
public partial struct DamageOnTriggerSystem : ISystem
{
    private BufferLookup<DamageBufferElement> dmgBufferLookup;

    public void OnCreate(ref SystemState state)
    {
        dmgBufferLookup = SystemAPI.GetBufferLookup<DamageBufferElement>();
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        dmgBufferLookup.Update(ref state);

        foreach (var (hitBuffer, damageOnTrigger) in SystemAPI.Query<DynamicBuffer<HitInfo>, DamageData>())
        {
            foreach (var hit in hitBuffer)
            {
                if (hit.IsHandled) continue;

                var dmgBuf = dmgBufferLookup[hit.HitEntity];
                dmgBuf.Add(new DamageBufferElement
                {
                    DamageType = damageOnTrigger.DamageType,
                    HitPoints = damageOnTrigger.Damage
                });
            }
        }
    }
}

[UpdateInGroup(typeof(SkillSystemGroup), OrderLast = true)]
public partial struct HandleHitBufferSystem : ISystem
{
    private EntityQuery hitBufferQ;
    public void OnCreate(ref SystemState state)
    {
        hitBufferQ = state.GetEntityQuery(ComponentType.ReadWrite<HitInfo>());
    }
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var triggerEntities = hitBufferQ.ToEntityArray(state.WorldUpdateAllocator);
        var hitBufferLookup = SystemAPI.GetBufferLookup<HitInfo>();

        foreach (var triggerEnt in triggerEntities)
        {
            var hitBuffer = hitBufferLookup[triggerEnt];
            for (int i = 0; i < hitBuffer.Length; i++)
            {
                hitBuffer.ElementAt(i).IsHandled = true;
            }
        }
    }
}

public partial struct DestroyAfterPierceSystem : ISystem
{
    public void OnCreate(ref SystemState state) { }
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
        foreach (var (pierce, entity) in SystemAPI.Query<DestroyAfterPierce>().WithEntityAccess())
        {
            if (pierce.PierceCurrent <= 0)
                ecb.AddComponent(entity, new DestroyEntityTag());
        }
        ecb.Playback(state.EntityManager);
    }
}


[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
public partial struct BulletTriggerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var triggerJob = new BulletTriggerJob
        {
            HitInfoLookup = SystemAPI.GetBufferLookup<HitInfo>(),
            HitPointsLookup = SystemAPI.GetComponentLookup<HitPoints>(),
            TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(),
            PierceLookup = SystemAPI.GetComponentLookup<DestroyAfterPierce>(),
        };

        var simInstance = SystemAPI.GetSingleton<SimulationSingleton>();
        state.Dependency = triggerJob.Schedule(simInstance, state.Dependency);
    }


    [BurstCompile]
    public partial struct BulletTriggerJob : ITriggerEventsJob
    {
        [ReadOnly] public ComponentLookup<HitPoints> HitPointsLookup;
        [ReadOnly] public ComponentLookup<DestroyAfterPierce> PierceLookup;
        public BufferLookup<HitInfo> HitInfoLookup;
        public ComponentLookup<LocalTransform> TransformLookup;


        [BurstCompile]
        public void Execute(TriggerEvent triggerEvent)
        {
            Entity triggerEnt;
            Entity hitEnt;
            if (HitInfoLookup.HasBuffer(triggerEvent.EntityA) && HitPointsLookup.HasComponent(triggerEvent.EntityB))
            {
                triggerEnt = triggerEvent.EntityA;
                hitEnt = triggerEvent.EntityB;
            }
            else if (HitInfoLookup.HasBuffer(triggerEvent.EntityB) && HitPointsLookup.HasComponent(triggerEvent.EntityA))
            {
                triggerEnt = triggerEvent.EntityB;
                hitEnt = triggerEvent.EntityA;
            }
            else
                return;


            //Was the hit entity already added to the trigger entity's hit info buffer?
            var hitInfo = HitInfoLookup[triggerEnt];
            foreach (var hit in hitInfo)
            {
                if (hit.HitEntity == hitEnt)
                    return;
            }

            if (PierceLookup.HasComponent(hitEnt))
            {
                var pierce = PierceLookup.GetRefRW(hitEnt);
                pierce.ValueRW.PierceCurrent--;
            }

            var triggerEntPosition = TransformLookup[triggerEnt].Position;
            var hitEntPosition = TransformLookup[hitEnt].Position;

            var hitPos = math.lerp(triggerEntPosition, hitEntPosition, 0.5f);
            var normal = math.normalizesafe(hitEntPosition - triggerEntPosition);
            var newHitInfo = new HitInfo
            {
                IsHandled = false,
                HitEntity = hitEnt,
                Position = hitPos,
                Normal = normal,
            };

            HitInfoLookup[triggerEnt].Add(newHitInfo);
        }
    }

}
