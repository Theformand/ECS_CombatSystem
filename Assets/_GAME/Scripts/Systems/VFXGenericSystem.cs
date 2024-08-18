using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.VFX;

public struct VFXLibrary : IComponentData { }

public struct VFXGeneric : IComponentData
{
    public UnityObjectRef<VisualEffect> Asset;
    public bool ShouldPlay;
}

public struct VFXPropertySetter : IComponentData
{
    public float RadiusProperty;
}


public partial struct SpawnVFXSystem: ISystem
{
    public void OnCreate(ref SystemState state) { }
    public void OnDestroy(ref SystemState state) { }

    public void OnUpdate(ref SystemState state)
    {
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

        for (int i = 0; i < 2; i++)
        {
            var e = ecb.CreateEntity();
            ecb.AddComponent(e, new SpawnVFXCommand
            {
                Pos = new float3(0f, 0f, i),
                Rotation = quaternion.identity,
                Scale = 1f,
                VFXPrefabType = VFXPrefabType.Grenade_Explosion_HE
            });
        }
        state.Enabled = false;
    }
}

public partial struct VFXPropertiesSystem: ISystem
{
    private static int ID_RADIUS = Shader.PropertyToID("Radius");

    public void OnCreate(ref SystemState state) { }
    public void OnDestroy(ref SystemState state) { }

    public void OnUpdate(ref SystemState state)
    {
        int count = 0;
        var time = (float)SystemAPI.Time.ElapsedTime;
        foreach (var (vfx ,asset ) in SystemAPI.Query<RefRW<VFXGeneric>, SystemAPI.ManagedAPI.UnityEngineComponent<VisualEffect>>())
        {
            float radius = math.sin(time + count) * 0.5f + 0.5f;
            radius *= 5f;
            asset.Value.SetFloat(ID_RADIUS,radius);
            count++;
        }

    }
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
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var buffer = SystemAPI.GetBuffer<VFXLibBufferElement>(vfxLib);

        //Spawn requested VFX. They cannot be spawned from other systems that are bursted
        foreach (var (request, entity) in SystemAPI.Query<SpawnVFXCommand>().WithEntityAccess())
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
