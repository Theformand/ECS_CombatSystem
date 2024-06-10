using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public struct SpawnerData : IComponentData
{
    public int InstanceCount;
    public Entity Prefab;
    public float Scale;
    public bool RandomRot;
    public float SparsityScalar;
    public float YOffset;

    public SpawnPlacementMode PlacementMode { get; internal set; }
}

public partial class WeaponSpawnSystem : SystemBase
{
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<WeaponLib>();
    }

    public void EquipWeapon(WeaponSkillData weaponSkillData)
    {
        var lib = SystemAPI.GetSingleton<WeaponLib>();
        var buf = SystemAPI.GetSingletonBuffer<EntityLookupData>();
        var prefab = lib.GetWeaponPrefab(buf,Animator.StringToHash(weaponSkillData.Guid));
        EntityManager.Instantiate(prefab);
    }

    protected override void OnUpdate()
    {

    }
}



public partial struct SpawnerSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<WeaponLib>();
    }

    public void OnDestroy(ref SystemState state) { }

    //[BurstCompile]
    void OnUpdate(ref SystemState state)
    {

        var buffer = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
        state.Dependency = new SpawnJob()
        {
            BaseSeed = System.DateTime.Now.Millisecond,
            buffer = buffer,
        }.ScheduleParallel(state.Dependency);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static quaternion RandomRot(Random rng)
    {
        return quaternion.Euler(new float3(rng.NextFloat(-1f, 1f), rng.NextFloat(-1f, 1f), rng.NextFloat(-1f, 1f)));
    }

    [BurstCompile]
    public partial struct SpawnJob : IJobEntity
    {
        [ReadOnly] public int BaseSeed;
        public EntityCommandBuffer.ParallelWriter buffer;
        [BurstCompile]
        private void Execute([EntityIndexInQuery] int entIdx, in SpawnerData data, in Entity entity)
        {
            var seed = (uint)(BaseSeed + entIdx);
            var rng = new Random((uint)((entIdx + 1) * seed) + 1);

            for (int i = 0; i < data.InstanceCount; i++)
            {
                float3 pos = float3.zero;
                if (data.PlacementMode == SpawnPlacementMode.Disc)
                {
                    float radius = 30f * data.SparsityScalar;
                    pos = new float3(rng.NextFloat(-1f, 1f), rng.NextFloat(-1f, 1f), rng.NextFloat(-1f, 1f)) * rng.NextFloat(0f, radius);
                }

                pos.y = data.YOffset;
                var ent = buffer.Instantiate(entIdx, data.Prefab);
                var rot = quaternion.identity;
                if (data.RandomRot)
                    rot = RandomRot(rng);

                buffer.SetComponent(entIdx, ent, new LocalTransform() { Position = pos, Rotation = rot, Scale = data.Scale });
            }
            buffer.DestroyEntity(entIdx, entity);
        }
    }
}