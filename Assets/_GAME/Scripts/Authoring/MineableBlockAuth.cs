using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class MineableBlockAuth : MonoBehaviour
{
    public int Health;
    public Transform GFXContainer;

    public class Baker : Baker<MineableBlockAuth>
    {
        public override void Bake(MineableBlockAuth authoring)
        {
            var rng = new Unity.Mathematics.Random();
            rng.InitState();
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new MineableBlock
            {
                Health = authoring.Health,
                MaxHealth = authoring.Health,
                GFXContainer = GetEntity(authoring.GFXContainer, TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic )
            });
        }
    }
}
