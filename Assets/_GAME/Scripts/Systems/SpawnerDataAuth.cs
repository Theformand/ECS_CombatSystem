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
    public bool Singleton;
    public int InstanceCount;
}

public class SpawnerBaker : Baker<SpawnerAuth>
{
    public override void Bake(SpawnerAuth authoring)
    {
        foreach (var spawnInfo in authoring.Prefabs)
        {
            SpawnerData sd = default;
            sd.Prefab = GetEntity(spawnInfo.Prefab, TransformUsageFlags.Dynamic);
            sd.Singleton = spawnInfo.Singleton;
            sd.InstanceCount = spawnInfo.InstanceCount;
            var ent = CreateAdditionalEntity(TransformUsageFlags.Dynamic, false, spawnInfo.Prefab.name);
            AddComponent(ent, sd);
        }
    }
}

