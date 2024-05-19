using AutoAuthoring;
using Unity.Entities;
using UnityEngine;

public class SkillActivationAuth : MonoBehaviour
{
    public float ActivationRange = 10f;
    public bool RequiresLOS;
    public SkillTargetingMode TargetingMode;

    public class Baker : Baker<SkillActivationAuth>
    {
        public override void Bake(SkillActivationAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new SkillActivationData()
            {
                ActivationRange = authoring.ActivationRange,
                ActivationRangeSqr = authoring.ActivationRange * authoring.ActivationRange,
                RequireLOS = authoring.RequiresLOS,
                TargetingMode = authoring.TargetingMode
            });
        }
    }
}
