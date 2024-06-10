using Unity.Entities;
using UnityEngine;

/// <summary>
/// Example of how to bake values from a ScriptableObject into the components required for a weapon to function. 
/// A projectile weapon in this case
/// </summary>

[CreateAssetMenu(fileName = "WeaponSkillData", menuName = "Data/WeaponSkillData", order = 0)]
public class ProjectileSkillData : WeaponSkillData
{
    public float Range;
    public GameObject ProjectilePrefab;
    public string audioFire;
    public string audioReload;
    public FireConfig[] FireConfigs;

    public float Time;
    public int MagCountBase;

    public float ActivationRange;
    public bool RequireLOS;
    public SkillTargetingMode TargetingMode;

    public int NumBulletsPerAttack;
    public float AttacksPerSecond;
    public float AngleSpread;
    public float AccuracySpread;
    public float BulletMoveSpeed;
    public int Damage;
    public int Pierce;
    public float Lifetime;
    public DamageType DamageType;
    public Entity BulletPrefab;
    public SkillAudioSettings AudioSettings;

    public Entity Bake(IBaker baker)
    {
        var e = baker.CreateAdditionalEntity(TransformUsageFlags.None, false, name);
        var audioSettings = new SkillAudioSettings
        {
            AudioReload = Animator.StringToHash(audioReload),
            AudioShoot = Animator.StringToHash(audioFire),
        };

        baker.AddComponent(e, new BulletSkillData
        {
            AudioSettings = audioSettings,
            BulletPrefab = baker.GetEntity(ProjectilePrefab, TransformUsageFlags.Dynamic),
            AccuracySpread = AccuracySpread,
            AngleSpread = AngleSpread,
            AttacksPerSecond = AttacksPerSecond,
            Damage = Damage,
            BulletMoveSpeed = BulletMoveSpeed,
            DamageCurrent = Damage,
            TimeStampNextShot = 0f,
            DamageType = DamageType,
            Lifetime = Lifetime,
            NumBulletsPerAttack = NumBulletsPerAttack,
            Pierce = Pierce,
        });

        baker.AddComponent(e, new SkillActivationData()
        {
            ActivationRange = ActivationRange,
            ActivationRangeSqr = ActivationRange * ActivationRange,
            RequireLOS = RequireLOS,
            TargetingMode = TargetingMode
        });

        baker.AddComponent(e, new SkillReloadData
        {
            MagCountBase = MagCountBase,
            MagCountCurrent = MagCountBase,
            Time = Time,
            TimeCurrent = Time,
        });

        baker.AddComponent(e, new Prefab());
        return e;
    }
}


public class WeaponSkillData : ScriptableObject
{
    public string Guid;
}
