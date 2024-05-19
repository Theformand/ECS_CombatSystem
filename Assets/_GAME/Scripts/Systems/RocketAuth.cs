using AutoAuthoring;
using Unity.Entities;
using UnityEngine;
using static RocketSwarmSystem;

public class RocketAuth : MonoBehaviour
{
    public RocketSettings Settings;
    public class Baker : Baker<RocketAuth>
    {
        public override void Bake(RocketAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new Rocket
            {
               
            });
        }
    }
}
