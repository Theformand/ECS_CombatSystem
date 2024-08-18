using Unity.Entities;
using UnityEngine.VFX;
using UnityEngine;

public class VFXGenericAuth : MonoBehaviour
{
    public VisualEffect Asset;
    public bool AutoPlay;
    public VFXPrefabType PrefabType;

    public class Baker : Baker<VFXGenericAuth>
    {
        public override void Bake(VFXGenericAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new VFXGeneric
            {
                Asset = authoring.Asset,
                ShouldPlay = authoring.AutoPlay
            });
        }
    }
}

public struct VFXLibBufferElement : IBufferElementData
{
    public Entity Prefab;
    public VFXPrefabType PrefabType;
}
