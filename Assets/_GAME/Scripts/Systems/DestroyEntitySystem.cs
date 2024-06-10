using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;

public partial struct DestroyEntitySystem : ISystem
{
    private BufferLookup<Child> bufferLookup;

    public void OnCreate(ref SystemState state) 
    {
        bufferLookup = SystemAPI.GetBufferLookup<Child>();
    }

    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        bufferLookup.Update(ref state);

        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (_, entity) in SystemAPI.Query<DestroyEntityTag>().WithEntityAccess())
        {
            DestroyHierarchy(entity, ecb, bufferLookup);
        }
        ecb.Playback(state.EntityManager);
    }

    public static void DestroyHierarchy(Entity entity, EntityCommandBuffer ecb, in BufferLookup<Child> childBufferFromEntity)
    {
        ecb.DestroyEntity(entity);

        if (childBufferFromEntity.HasBuffer(entity))
        {
            DynamicBuffer<Child> childBuffer = childBufferFromEntity[entity];
            for (int i = 0; i < childBuffer.Length; i++)
            {
                DestroyHierarchy(childBuffer[i].Value, ecb, in childBufferFromEntity);
            }
        }
    }
}
