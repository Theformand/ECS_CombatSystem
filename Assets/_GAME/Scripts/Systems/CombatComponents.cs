using System;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public struct CurveLib : IComponentData
{
    public DotsCurve RocketHeightCurve;
    public DotsCurve XPPickupVelocityCurve;
    public DotsCurve PickupVelocityCurve;
    public DotsCurve BlockMiningTween;
    public DotsCurve BlockMiningTween_Driller;
    public DotsCurve KnockbackCurve;
}

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

public struct DotsCurve : IComponentData
{
    public BlobAssetReference<DiscretizedCurve> BlobRef;
    /// <summary>
    /// Evalute curve from 0-1
    /// </summary>
    /// <param name="time"></param>
    /// <returns></returns>
    public readonly float Evaluate(float time) => BlobRef.Value.Evaluate(time);
}

public struct DiscretizedCurve
{
    public BlobArray<float> Points;
    public int NumSamples;

    public float Evaluate(float time)
    {
        var approxIdx = (NumSamples - 1) * time;
        var prevIdx = (int)math.floor(approxIdx);
        if (prevIdx >= NumSamples - 1)
            return Points[NumSamples - 1];

        var idxRemainder = approxIdx - prevIdx;
        return math.lerp(Points[prevIdx], Points[prevIdx + 1], idxRemainder);
    }
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
    public int PierceCost;
}


[Serializable]
public struct SkillReloadData : IComponentData
{
    public float Time;
    public int MagCountBase;

    public int MagCountCurrent;
    public float TimeCurrent;
}

[Serializable]
public struct SkillActivationData : IComponentData
{
    public float ActivationRange;
    public float ActivationRangeSqr;
    public bool RequireLOS;
    public SkillTargetingMode TargetingMode;
}

[Serializable]
public struct BulletSkillData : IComponentData
{
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
    [HideInInspector]
    public int DamageCurrent;
    public float TimeStampNextShot;
}

[Serializable]
public struct RocketSettings
{
    public float3 Destination;
    public float3 LaunchPoint;
    public float FlightTime;
    public float ApexHeight;
    public float NoiseScaleX;
    public float NoiseScaleZ;
    public float NoiseFreq;
}


[Serializable]
public struct RocketSwarmSkillData : IComponentData
{
    public Entity RocketPrefab;
    public int Damage;
    public DamageType DamageType;
    public RocketSettings Settings;
    public float RocketsPerSecond;

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
public struct GrenadeSkillData : IComponentData
{
    public Entity GrenadePrefab;
    public GrenadeSettings GrenadeSettings;
    public Entity ClusterGrenade;
}


//shoot this struct out of ECS land every time state changes, so the UI can update?
public struct WeaponUIData
{
    public int AmmoMax;
    public int AmmoCurrent;
    public float ReloadDuration;
    public float ReloadTime;
}

public struct SpawnClusterGrenades : IComponentData
{
    public float3 Position;
    public float LifeTime;
    public Entity GrenadePrefab;
    public GrenadeSettings GrenadeSettings;
}

[Serializable]
public struct GrenadeSettings
{
    public float LifeTime;
    public float ExplosionRadius;
    public int DamageAtCenter;
    public DamageType DamageType;
    public GrenadeExplosionType ExplosionType;
    public bool Cluster;
    public int NumClusterGrenades;
    public float ThrowForce;
    public float ThrowUpForce;
}

[Serializable]
public struct GrenadeData : IComponentData
{
    public GrenadeSettings GrenadeSettings;
    public float LifeTime;
    public bool Cluster;
    [HideInInspector] public Entity ClusterGrenade;
}


[Serializable]
public struct Player : IComponentData
{
    public int Health;
    public float BaseMoveSpeedScalar;
    public float PickupDistance;
    public float MiningInterval;
    public int MiningDamage;
    public float TimestampLastMine;
}

public struct Pickup : IComponentData
{
    public PickupType PickupType;
    public int Value;
    public float PickupDistanceOverrideSqr;
    public bool HasDistanceOverride;
}

public enum PickupType
{
    XP,
    Mineral,
    Magnet
}

public enum SkillTargetingMode
{
    CLOSEST,
    HIGHEST_HP_MAX,
    HIGHEST_HP_CURRENT,
    RANDOM,
    PLAYER_FWD,
    PLAYER_BACK,
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
}

public struct SpawnVFXRequest : IComponentData
{
    public VFXPrefabType VFXPrefabType;
    public float3 Pos;
    public quaternion Rotation;
    public float Scale;
}


public enum VFXPrefabType
{
    Grenade_Explosion_HE
}

public enum GrenadeExplosionType
{
    Explosion,
    Bounce,
    SpinningBullets,
    BulletBloom,
}

[UpdateInGroup(typeof(SimulationSystemGroup))]
//[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
[UpdateBefore(typeof(TransformSystemGroup))]
public partial class SkillSystemGroup : ComponentSystemGroup { }
