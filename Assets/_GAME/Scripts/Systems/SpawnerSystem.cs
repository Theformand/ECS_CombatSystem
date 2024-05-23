using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

public struct SpawnerData : IComponentData
{
    public int InstanceCount;
    public Entity Prefab;
    public float Scale;
    public bool RandomRot;
    public float SparsityScalar;

    public SpawnPlacementMode PlacementMode { get; internal set; }
}

public partial struct SpawnerSystem : ISystem
{
    private int numSpawns;

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

        numSpawns++;
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
                
                pos.y = 0f;
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