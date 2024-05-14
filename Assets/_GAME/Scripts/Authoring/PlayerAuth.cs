
using AutoAuthoring;
using Unity.Entities;
using UnityEngine;

public class PlayerAuth : MonoBehaviour
{
    public class Baker : Baker<PlayerAuth>
    {
        public override void Bake(PlayerAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new Player() { Health = 100 });
        }
    }
}


