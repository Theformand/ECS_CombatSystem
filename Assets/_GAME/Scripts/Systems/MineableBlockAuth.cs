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
            var ent = GetEntity(TransformUsageFlags.NonUniformScale | TransformUsageFlags.Dynamic);
            AddComponent(ent, new MineableBlock
            {
                Health = authoring.Health,
                MaxHealth = authoring.Health,
                //GFXContainer = authoring.GFXContainer,
                
            });
            var tween = new BlockMiningTween
            {
                Duration = 0.35f,
                Power = 2f,
                T = 0f,
                CycleMode = TweenCycleMode.Loop
            };
            tween.SetAsymmetryXZ(ref rng, 2f);
            AddComponent(ent, tween);
            AddComponent(ent, new PostTransformMatrix
            {
                Value = float4x4.identity,
            });
        }
    }
}
