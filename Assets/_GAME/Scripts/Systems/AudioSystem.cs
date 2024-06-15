using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct PlayAudioRequest : IComponentData
{
    public int Hash;
    public float3 Position;
    public bool HasBeenTriggered;
}

public struct ECSAudioBufferElement : IBufferElementData
{
    public Entity ECS_Source;
}

public struct ECSAudioSource : IComponentData
{
    public UnityObjectRef<AudioSource> Source;
}

public partial struct AudioSystem : ISystem
{
    private DynamicBuffer<ECSAudioBufferElement> sourceEntPool;
    private DynamicBuffer<ECSAudioBufferElement> liveEnts;
    private const int MAX_SOURCES = 65;
    private int numPlayingSources;
    private EntityQuery query;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ECSAudioSource>();
        query = state.GetEntityQuery(typeof(ECSAudioSource), ComponentType.ReadOnly<ECSAudioBufferElement>());
    }

    public void OnDestroy(ref SystemState state) { }

    private void InitSources(ref SystemState state)
    {
        var singleton   = SystemAPI.GetSingletonEntity<ECSAudioSource>();
        var poolEnt     = state.EntityManager.CreateEntity();
        var liveEnt     = state.EntityManager.CreateEntity();
        sourceEntPool   = state.EntityManager.AddBuffer<ECSAudioBufferElement>(poolEnt);
        liveEnts        = state.EntityManager.AddBuffer<ECSAudioBufferElement>(liveEnt);

        for (int i = 0; i < MAX_SOURCES; i++)
        {
            sourceEntPool.Add(new ECSAudioBufferElement
            {
                ECS_Source = state.EntityManager.Instantiate(singleton)
            });
        }
    }

    public void OnUpdate(ref SystemState state)
    {
        return;
        if (!sourceEntPool.IsCreated)
            InitSources(ref state);

        // Handle new requests from other systems
        foreach (var req in SystemAPI.Query<RefRW<PlayAudioRequest>>())
        {
            if (!req.ValueRO.HasBeenTriggered)
            {
                if (numPlayingSources >= MAX_SOURCES)
                {
                    // Pop off the least significant source. Likely the oldest.
                }

                req.ValueRW.HasBeenTriggered = true;
                ToGameObjects.PlaySoundAtPosition(req.ValueRO.Hash, req.ValueRW.Position);
                numPlayingSources++;
            }
        }


        // Put back sources that are no longer playing - this is not the right approach, I think
        var sources = query.ToComponentDataArray<ECSAudioSource>(Allocator.Temp);
        foreach (var srcWrapper in liveEnts)
        {
            //var src = SystemAPI.GetComponent<ECSAudioBufferElement>(srcWrapper);
            //if (!src.ECS_Sourc.Value.isPlaying)
            //{
            //    sourceEntPool.Add(srcWrapper)
            //}
        }
    }
}
