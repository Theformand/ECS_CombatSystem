using Unity.Entities;
using UnityEngine.VFX;
using UnityEngine;

public class VFXGenericAuth : MonoBehaviour
{
    public VisualEffect Asset;
    public bool AutoPlay;
    public VFXPrefabType PrefabType;

  }

public struct VFXLibBufferElement : IBufferElementData
{
    public Entity Prefab;
    public VFXPrefabType PrefabType;
}
