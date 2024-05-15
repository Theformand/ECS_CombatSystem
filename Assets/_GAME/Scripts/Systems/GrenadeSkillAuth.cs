using AutoAuthoring;
using Unity.Entities;
using UnityEngine;

[RequireComponent(typeof(SkillReloadTimeAuth), typeof(SkillActivationAuth))]
public class GrenadeSkillAuth : AutoAuthoring<GrenadeSkillData>
{
    //public GameObject GrenadePrefab;
    //public int DamageAtCenter;
    //public float LifeTime;
    //public float ExplosionRadius;
    //public float ThrowForce;
    //public float ThrowUpForce;
    //public DamageType DamageType;
    //public class Baker : Baker<GrenadeSkillAuth>
    //{
    //    public override void Bake(GrenadeSkillAuth authoring)
    //    {
    //        var ent = GetEntity(TransformUsageFlags.Dynamic);
    //        AddComponent(ent, new GrenadeSkillData
    //        {
    //            DamageAtCenter = authoring.DamageAtCenter,
    //            LifeTime = authoring.LifeTime,
    //            DamageType = authoring.DamageType,
    //            ThrowForce = authoring.ThrowForce,
    //            ExplosionRadius = authoring.ExplosionRadius,
                
    //        });
    //    }
    //}
}
