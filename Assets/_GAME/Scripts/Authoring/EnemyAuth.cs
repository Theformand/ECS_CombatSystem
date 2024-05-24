using AutoAuthoring;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class EnemyAuth : MonoBehaviour
{
    public class Baker : Baker<EnemyAuth>
    {
        public override void Bake(EnemyAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new EnemyTag
            {
                HPCurrent = 100,
                HPMax = 100,
                PierceCost = 1,
            });
       }
    }
}
