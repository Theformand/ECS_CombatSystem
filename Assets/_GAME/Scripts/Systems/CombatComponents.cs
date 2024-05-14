using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[Serializable]
public struct SkillMoveSpeed : IComponentData
{
    public float Speed;
}

public struct HitInfo : IBufferElementData
{
    public bool IsHandled;
    public float3 Position;
    public float3 Normal;
    public Entity HitEntity;
}

public struct SkillPrefab : IComponentData
{
    public Entity Prefab;
}

[Serializable]
public struct HitPoints : IComponentData
{
    public int Max;
    public int Current;
    public int PierceCost;
}

[Serializable]
public struct DestroyOnTimer : IComponentData
{
    public float Time;
}

public struct DestroyAfterPierce : IComponentData
{
    public int PierceCurrent;
}

[Serializable]
public struct DamageData : IComponentData
{
    public int Damage;
    public DamageType DamageType;
}

[Serializable]
public struct EnemyTag : IComponentData
{
    public int HPMax;
    public int HPCurrent;
}


[Serializable]
public struct SkillReloadData : IComponentData
{
    public float Time;
    public int MagCountBase;

    [HideInInspector]
    public int MagCountCurrent;
    [HideInInspector]
    public float TimeCurrent;
}

[Serializable]
public struct SkillActivationData : IComponentData
{
    public float ActivationRange;
    [HideInInspector]
    public float ActivationRangeSqr;
    public bool RequireLOS;
    public SkillTargetingMode TargetingMode;
}

[Serializable]
public struct BulletSkillShotData : IComponentData
{
    public int NumBulletsPerAttack;
    public float AttacksPerSecond;
    public float AngleSpread;
    public float BulletMoveSpeed;
    public int Damage;
    public int Pierce;
    public float Lifetime;
    public DamageType DamageType;
    public Entity BulletPrefab;
    [HideInInspector]
    public int DamageCurrent;
    [HideInInspector]
    public float TimeStampNextShot;
}

[Serializable]
public struct BeamSkillData : IComponentData
{
    public float BeamLengthBase;
    public float TicksPerSecond;
    public float BeamRotationSpeedBase;
    public int BeamCountBase;
    public float LifeTime;
    public int DamageBase;
    public DamageType DamageType;

    public float BeamLengthCurrent;
    public float TimeStampNextTick;
    public int BeamCountCurrent;
    public float BeamRotationSpeedCurrent;
    public float AngleCurrent;
    public float LifetimeCurrent;
    public int DamageCurrent;
}


[Serializable]
public struct Player : IComponentData
{
    public int Health;
}

public enum SkillTargetingMode
{
    CLOSEST,
    HIGHEST_HP_MAX,
    HIGHEST_HP_CURRENT,
    RANDOM
}

public struct DestroyEntityTag : IComponentData { }

// Queues damage elements throughout duration of the frame so they can all be applied once at the end of the frame.
// Avoids the problem where multiple damage elements are applied to a single entity and only one actually applies.
[InternalBufferCapacity(1)]
public struct DamageBufferElement : IBufferElementData
{
    public DamageType DamageType;
    public int HitPoints;
}

public enum DamageType
{
    KINETIC,
    FIRE,
    COLD,
    ACID,
    EXPLOSIVE,
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(TransformSystemGroup))]
public partial class SkillSystemGroup : ComponentSystemGroup { }
