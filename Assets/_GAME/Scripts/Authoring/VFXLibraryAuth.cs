using Unity.Entities;
using UnityEngine;

public class VFXLibraryAuth : MonoBehaviour
{
    public VFXGenericAuth[] VFXPrefabs;

    public class Baker : Baker<VFXLibraryAuth>
    {
        public override void Bake(VFXLibraryAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<VFXLibrary>(ent);
            var buff = AddBuffer<VFXLibBufferElement>(ent);

            for (int i = 0; i < authoring.VFXPrefabs.Length; i++)
            {
                var curr = authoring.VFXPrefabs[i];
                buff.Add(new VFXLibBufferElement
                {
                    Prefab = GetEntity(curr, TransformUsageFlags.Dynamic),
                    PrefabType = curr.PrefabType
                });
            }
        }
    }
}
