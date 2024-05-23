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
        private void Execute([ChunkIndexInQuery] int chunkIndex, in SpawnerData data, in Entity entity)
        {
            var seed = (uint)(BaseSeed + chunkIndex);
            var rng = new Random((uint)((chunkIndex + 1) * seed) + 1);
            for (int i = 0; i < data.InstanceCount; i++)
            {
                float radius = 30f * data.SparsityScalar;
                var pos = new float3(rng.NextFloat(-1f, 1f), rng.NextFloat(-1f, 1f), rng.NextFloat(-1f, 1f)) * rng.NextFloat(0f, radius);
                pos.y = 0f;
                var ent = buffer.Instantiate(chunkIndex, data.Prefab);
                var rot = quaternion.identity;
                if (data.RandomRot)
                    rot = RandomRot(rng);

                buffer.SetComponent(chunkIndex, ent, new LocalTransform() { Position = pos, Rotation = rot, Scale = data.Scale });
            }
            buffer.DestroyEntity(chunkIndex, entity);
        }
    }
}