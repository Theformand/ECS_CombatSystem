using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

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
    public static void SpawnVFX(VFXPrefabType vFXPrefabType, ref float3 position, ref quaternion rotation, float scale, ref EntityCommandBuffer ecb)
    {
        var marker = ecb.CreateEntity();
        ecb.AddComponent(marker, new SpawnVFXCommand
        {
            Pos = position,
            Rotation = rotation,
            Scale = scale,
            VFXPrefabType = vFXPrefabType
        });
    }

    [BurstCompile]
    public static void ShootBulletInDirection(ref ShootBulletArgs args, ref EntityCommandBuffer ecb)
    {

        var bullet = ecb.Instantiate(args.BulletPrefab);
        var pos = args.Origin;
        var rot = quaternion.LookRotationSafe(args.Direction, new float3(0, 1, 0));
        ecb.SetComponent(bullet, LocalTransform.FromPositionRotation(pos, rot));
        ecb.SetComponent(bullet, new Projectile
        {
            Heading = args.Direction,
            Speed = args.MoveSpeed
        });
        ecb.AddComponent(bullet, new DamageData() { Damage = args.Damage, DamageType = args.DamageType });
        //ecb.AddComponent(bullet, new SkillMoveSpeed { Speed = args.MoveSpeed });
        ecb.SetComponent(bullet, new PhysicsVelocity
        {
            Linear = args.Direction * args.MoveSpeed
        });

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
        float3 up = math.up();

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
