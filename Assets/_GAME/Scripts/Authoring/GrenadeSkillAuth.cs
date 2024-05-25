using AutoAuthoring;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(SkillReloadTimeAuth), typeof(SkillActivationAuth))]
public class GrenadeSkillAuth : MonoBehaviour
{
    public GameObject GrenadePrefab;
    public GrenadeSettings GrenadeSettings;

    public class Baker : Baker<GrenadeSkillAuth>
    {
        public override void Bake(GrenadeSkillAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            var prefab = GetEntity(authoring.GrenadePrefab, TransformUsageFlags.Dynamic);
            AddComponent(ent, new GrenadeSkillData
            {
                GrenadePrefab = prefab,
                GrenadeSettings = authoring.GrenadeSettings, 
                ClusterGrenade = prefab 
            });
        }
    }
}