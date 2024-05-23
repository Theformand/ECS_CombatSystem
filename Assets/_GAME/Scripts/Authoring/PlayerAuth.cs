
using AutoAuthoring;
using Unity.Entities;
using UnityEngine;

public class PlayerAuth : MonoBehaviour
{
    public float PickupDistance;
    public float MiningInterval = 0.4f;
    public float BaseMoveSpeedScalar = 300f;

    public class Baker : Baker<PlayerAuth>
    {
        public override void Bake(PlayerAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new Player() { 
                Health = 100 ,
                PickupDistance = authoring.PickupDistance,
                MiningInterval = authoring.MiningInterval,
                BaseMoveSpeedScalar = authoring.BaseMoveSpeedScalar
            });
            AddComponent(ent, new PlayerPickupTrigger());
        }
    }
}


