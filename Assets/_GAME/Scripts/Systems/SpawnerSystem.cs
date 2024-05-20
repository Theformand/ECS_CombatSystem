using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Transforms;

public struct SpawnerData : IComponentData
{
    public bool Singleton;
    public int InstanceCount;
    public Entity Prefab;
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
    public static quaternion RandomRot(Unity.Mathematics.Random rng)
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
            if (data.Singleton)
            {
                var seed = (uint)(BaseSeed + chunkIndex);
                var rng = new Unity.Mathematics.Random((uint)((chunkIndex + 1) * seed) + 1);
                for (int i = 0; i < data.InstanceCount; i++)
                {
                    const float radius = 30f;
                    var pos = new float3(rng.NextFloat(-1f, 1f), rng.NextFloat(-1f, 1f), rng.NextFloat(-1f, 1f)) * rng.NextFloat(0f, radius);
                    pos.y = 1f;
                    var ent = buffer.Instantiate(chunkIndex, data.Prefab);
                    buffer.SetComponent(chunkIndex, ent, new LocalTransform() { Position = pos , Rotation = RandomRot(rng), Scale = 0.25f});
                }
                buffer.DestroyEntity(chunkIndex, entity);
            }
        }
    }
}