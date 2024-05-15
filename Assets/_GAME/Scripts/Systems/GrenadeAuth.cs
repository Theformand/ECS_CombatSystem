using AutoAuthoring;
using Unity.Entities;
using UnityEngine;

public class GrenadeAuth : MonoBehaviour
{
    public class Baker : Baker<GrenadeAuth>
    {
        public override void Bake(GrenadeAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new GrenadeData() { UniformScale = authoring.transform.localScale.x});
        }
    }
}