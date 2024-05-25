using AutoAuthoring;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class EnemyAuth : MonoBehaviour
{
    public float MoveSpeed;
    public float TurnSpeed;
    public float AttackRange;
    public float AttackInterval;


    public class Baker : Baker<EnemyAuth>
    {
        public override void Bake(EnemyAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new Enemy
            {
                HPCurrent = 100,
                HPMax = 100,
                PierceCost = 1,
            });
            AddComponent(ent, new EnemyMoveData
            {
                MoveSpeed = authoring.MoveSpeed,
                TurnSpeed = authoring.TurnSpeed,
                CatchupMultiplier = 1f
            });

            AddComponent(ent, new MeleeSkillData
            {
                AttackInterval = authoring.AttackInterval,
                AttackRangeSqr = authoring.AttackRange * authoring.AttackRange,
            });
       }
    }
}
