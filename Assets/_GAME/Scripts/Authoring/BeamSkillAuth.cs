using Unity.Entities;
using UnityEngine;

public class BeamSkillAuth : MonoBehaviour
{
    public int Damage;
    public DamageType DamageType;
    public float BeamLengthBase;
    public float TicksPerSecond;
    public float BeamRotationSpeedBase;
    public int BeamCountBase;
    public float LifeTime;

    public class Baker : Baker<BeamSkillAuth>
    {
        public override void Bake(BeamSkillAuth a)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new BeamSkillData()
            {
                DamageBase = a.Damage,
                DamageType = a.DamageType,
                DamageCurrent = a.Damage,
                BeamCountBase = a.BeamCountBase,
                BeamCountCurrent = a.BeamCountBase,
                TicksPerSecond = a.TicksPerSecond,
                BeamLengthBase = a.BeamLengthBase,
                BeamLengthCurrent = a.BeamLengthBase,
                AngleCurrent = 0f,
                BeamRotationSpeedBase = a.BeamRotationSpeedBase,
                BeamRotationSpeedCurrent = a.BeamRotationSpeedBase,
                LifeTime = a.LifeTime,
                LifetimeCurrent = a.LifeTime,
                TimeStampNextTick = 0f
            });
        }
    }
}
