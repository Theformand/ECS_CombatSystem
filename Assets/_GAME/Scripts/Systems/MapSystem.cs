using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Physics;
using UnityEngine;
using Collider = Unity.Physics.Collider;
using Unity.Transforms;
using Unity.Physics.Extensions;


public partial class MapSystem : SystemBase
{
    private Entity SingletonEnt;

    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<MapData>();
    }

    public void SetMapData(MapGenOutput data)
    {
        if (SingletonEnt == Entity.Null)
            SingletonEnt = EntityManager.CreateEntity();

        EntityManager.AddComponent(SingletonEnt, typeof(MapData));

        var md = new MapData
        {
            Layer = new NativeArray<int>(data.Layer, Allocator.Persistent),
            HoleLayer = new NativeArray<int>(data.HoleLayer, Allocator.Persistent),
            GroundMesh = data.GroundMesh
        };
        EntityManager.SetComponentData(SingletonEnt, md);
        EntityManager.AddComponent(SingletonEnt, typeof(GenerateMapRequest));
    }

    protected override void OnUpdate()
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (data, requestEntity) in SystemAPI.Query<MapData>().WithAll<GenerateMapRequest>().WithEntityAccess())
        {
            // Create Ground Collider
            var groundColliderEnt = EntityManager.CreateEntity();
            Mesh mesh = data.GroundMesh.Value;
            NativeArray<BlobAssetReference<Collider>> BlobCollider = new NativeArray<BlobAssetReference<Collider>>(1, Allocator.TempJob);
            NativeArray<float3> nativeVerts = new NativeArray<Vector3>(mesh.vertices, Allocator.TempJob).Reinterpret<float3>();
            NativeArray<int> nativeTris = new(mesh.triangles, Allocator.TempJob);

            CreateMeshColliderJob createMeshJob = new CreateMeshColliderJob { MeshVerts = nativeVerts, MeshTris = nativeTris, BlobCollider = BlobCollider };
            createMeshJob.Run();

            ecb.AddSharedComponent(groundColliderEnt, new PhysicsWorldIndex
            {
                Value = 0
            });
            ecb.AddComponent(groundColliderEnt, LocalTransform.FromPositionRotationScale(new float3(0f, 1f, 0f), quaternion.identity, 1f));
            var col = new PhysicsCollider { Value = BlobCollider[0] };
            ecb.AddComponent(groundColliderEnt, col);
            ecb.AddComponent(groundColliderEnt, new GroundColliderGizmo { Collider = col });

            nativeVerts.Dispose();
            nativeTris.Dispose();
            BlobCollider.Dispose();


            // Delete the request component, so we stop executing this query when generation is done
            ecb.RemoveComponent<GenerateMapRequest>(requestEntity);

        }

        ecb.Playback(EntityManager);
        foreach (var (giz, transform) in SystemAPI.Query<GroundColliderGizmo, LocalTransform>())
        {
            var m = giz.Collider.Value.Value.ToMesh();
            var verts = m.vertices;
            float3 normal = m.normals[0];
            Debug.DrawLine(transform.Position, transform.Position + normal);
            Vector3 offset = transform.Position;
            for (int i = 0; i < verts.Length - 1; i++)
            {
                Debug.DrawLine(verts[i] + offset, verts[i + 1] + offset, Color.green);
            }
        }
    }
}

public struct GroundColliderGizmo : IComponentData
{
    public PhysicsCollider Collider;
}

[BurstCompile]
public struct CreateMeshColliderJob : IJob
{
    [ReadOnly] public NativeArray<float3> MeshVerts;
    [ReadOnly] public NativeArray<int> MeshTris;
    public NativeArray<BlobAssetReference<Collider>> BlobCollider;

    public void Execute()
    {
        NativeArray<float3> CVerts = new NativeArray<float3>(MeshVerts.Length, Allocator.Temp);
        NativeArray<int3> CTris = new NativeArray<int3>(MeshTris.Length / 3, Allocator.Temp);

        for (int i = 0; i < MeshVerts.Length; i++) { CVerts[i] = MeshVerts[i]; }
        int ii = 0;
        for (int j = 0; j < MeshTris.Length; j += 3)
        {
            CTris[ii++] = new int3(MeshTris[j], MeshTris[j + 1], MeshTris[j + 2]);
        }

        CollisionFilter filter = new CollisionFilter { BelongsTo = 1 << 2, CollidesWith = 1 << 4, GroupIndex = 0 };
        BlobCollider[0] = Unity.Physics.MeshCollider.Create(CVerts, CTris, filter);
        CVerts.Dispose();
        CTris.Dispose();

    }
}


public struct MapGenOutput
{
    public int Width;
    public int Height;
    public int[] Layer;
    public int[] HoleLayer;
    public Mesh GroundMesh;
}

public struct MapCell
{
    public int X;
    public int Y;
    public MapCellType Type;
}

public enum MapCellType
{
    Hole,
    Ground,
    Edge,
    Rock,
}

public struct GenerateMapRequest : IComponentData
{

}

public struct MapData : IComponentData
{
    public NativeArray<int> Layer;
    public NativeArray<int> HoleLayer;
    public UnityObjectRef<Mesh> GroundMesh;
}
