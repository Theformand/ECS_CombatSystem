using AutoAuthoring;
using Unity.Entities;
using UnityEngine;

public class SkillReloadTimeAuth : MonoBehaviour
{
    public float Time;
    public int MagCountBase;

    
    public class Baker : Baker<SkillReloadTimeAuth>
    {
        public override void Bake(SkillReloadTimeAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new SkillReloadData
            {
                MagCountBase = authoring.MagCountBase,
                MagCountCurrent = authoring.MagCountBase,
                Time = authoring.Time,
                TimeCurrent = authoring.Time
            });
        }
    }
}
