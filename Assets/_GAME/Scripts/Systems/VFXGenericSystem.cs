using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;

public struct VFXLibrary : IComponentData {}

public struct VFXGeneric : IComponentData
{
    public UnityObjectRef<VisualEffect> Asset;
    public bool ShouldPlay;
}

public partial struct VFXGenericSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
        state.RequireForUpdate<VFXLibrary>();
    }

    public void OnDestroy(ref SystemState state) { }

    //No burst
    public void OnUpdate(ref SystemState state)
    {
        var vfxLib = SystemAPI.GetSingletonEntity<VFXLibrary>();
        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        var buffer = SystemAPI.GetBuffer<VFXLibBufferElement>(vfxLib);

        //Spawn requested VFX. They cannot be spawned from other systems that are bursted
        foreach (var (request, entity) in SystemAPI.Query<SpawnVFXRequest>().WithEntityAccess())
        {
            var prefab = GetVFX(request.VFXPrefabType, buffer);
            var vfx = ecb.Instantiate(prefab);
            ecb.SetComponent(vfx, LocalTransform.FromPositionRotationScale(request.Pos, request.Rotation, request.Scale));
            ecb.DestroyEntity(entity);
        }

        foreach (var vfx in SystemAPI.Query<RefRW<VFXGeneric>>())
        {
            if (vfx.ValueRO.ShouldPlay)
            {
                vfx.ValueRO.Asset.Value.Play();
                vfx.ValueRW.ShouldPlay = false;
            }
        }

        ecb.Playback(state.EntityManager);
    }

    private Entity GetVFX(VFXPrefabType vFXPrefabType, DynamicBuffer<VFXLibBufferElement> buf)
    {
        for (int i = 0; i < buf.Length; i++)
        {
            if (buf[i].PrefabType == vFXPrefabType)
            {
                return buf[i].Prefab;
            }
        }
        Debug.LogError($"ERROR! Could not find a VFX prefab with type: {vFXPrefabType}. Check VFXLibrary");
        return Entity.Null;
    }
}
