using Unity.Entities;
using UnityEngine;

public class RocketSwarmSkillAuth : MonoBehaviour
{
    public GameObject RocketPrefab;
    public int Damage;
    public DamageType DamageType;
    public float RocketsPerSecond;
    public RocketSettings Settings;

    public class Baker : Baker<RocketSwarmSkillAuth>
    {
        public override void Bake(RocketSwarmSkillAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            var rocket = GetEntity(authoring.RocketPrefab,TransformUsageFlags.Dynamic);
            AddComponent(ent, new RocketSwarmSkillData
            {
                Damage =authoring.Damage,
                DamageType = authoring.DamageType,
                RocketPrefab = rocket,
                RocketsPerSecond = authoring.RocketsPerSecond,
                Settings = authoring.Settings,
            });
        }
    }
}
