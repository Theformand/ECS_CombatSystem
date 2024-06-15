using Unity.Entities;
using UnityEngine;

public class ProjectileAuth : MonoBehaviour
{
    public class Baker : Baker<ProjectileAuth>
    {
        public override void Bake(ProjectileAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new Projectile { });
        }
    }
}
