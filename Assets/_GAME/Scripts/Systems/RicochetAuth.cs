using Unity.Entities;
using UnityEngine;

public class RicochetAuth : MonoBehaviour
{
    public class Baker : Baker<RicochetAuth>
    {
        public override void Bake(RicochetAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new Ricochet { });
        }
    }
}
