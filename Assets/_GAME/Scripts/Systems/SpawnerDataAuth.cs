using Unity.Entities;
using UnityEngine;

public class SpawnerAuth : MonoBehaviour
{
    public SpawnInfo[] Prefabs;
}

[System.Serializable]
public class SpawnInfo
{
    public GameObject Prefab;
    public int InstanceCount;
    public bool RandomRot;
    public float Scale = 1f;
    public float SparsityScalar = 1f;
    public SpawnPlacementMode PlacementMode = SpawnPlacementMode.Disc;
}

public enum SpawnPlacementMode
{
    Grid,
    Disc
}

public class SpawnerBaker : Baker<SpawnerAuth>
{
    public override void Bake(SpawnerAuth authoring)
    {
        foreach (var spawnInfo in authoring.Prefabs)
        {
            SpawnerData sd = default;
            sd.Prefab = GetEntity(spawnInfo.Prefab, TransformUsageFlags.Dynamic);
            sd.InstanceCount = spawnInfo.InstanceCount;
            sd.RandomRot = spawnInfo.RandomRot;
            sd.Scale = spawnInfo.Scale;
            sd.SparsityScalar = spawnInfo.SparsityScalar;
            sd.PlacementMode = spawnInfo.PlacementMode;
            var ent = CreateAdditionalEntity(TransformUsageFlags.Dynamic, false, spawnInfo.Prefab.name);
            AddComponent(ent, sd);
        }
    }
}

