using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

public struct MineableBlock : IComponentData
{
    public Entity GFXContainer;
    public bool HasMinerals;
    public MineralType MineralType;
    public int Health;
    public int MaxHealth;
}

public enum MineralType
{
    Gold,
    Red_Sugar,
    Morkite
}

public partial struct MiningSystem : ISystem
{
    private CollisionFilter miningFilter;
    private Random random;
    public void OnCreate(ref SystemState state)
    {
        miningFilter = new CollisionFilter
        {
            BelongsTo = ~0u,
            CollidesWith = 1u << 8,
            GroupIndex = 0,
        };
        random = new Random();
        random.InitState();
        state.RequireForUpdate<Player>();
    }
    public void OnDestroy(ref SystemState state) { }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var player = SystemAPI.GetSingletonRW<Player>();
        var playerEnt = SystemAPI.GetSingletonEntity<Player>();
        var playerTransform = SystemAPI.GetComponent<LocalTransform>(playerEnt);
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var physics = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        Debug.DrawLine(playerTransform.Position, playerTransform.Position + playerTransform.Forward());
        var time = (float)SystemAPI.Time.ElapsedTime;
        if (time > player.ValueRO.TimestampLastMine + player.ValueRO.MiningInterval)
        {
            var hits = new NativeList<DistanceHit>(Allocator.Temp);
            physics.OverlapSphere(playerTransform.Position + playerTransform.Forward(), 1f, ref hits, miningFilter);
            for (int i = 0; i < hits.Length; i++)
            {
                var ent =  hits[i].Entity;
                ref var blockW = ref SystemAPI.GetComponentRW<MineableBlock>(ent).ValueRW;
                ref readonly var block = ref SystemAPI.GetComponentRO<MineableBlock>(ent).ValueRO;
                var gfxContainerEnt = block.GFXContainer;

                //TODO: Notify Minimap
                blockW.Health -= player.ValueRO.MiningDamage;
                if (blockW.Health <= 0)
                {
                    //Death VFX
                    //Spawn any loot here
                    ecb.DestroyEntity(ent);
                }
                else
                {
                    if (SystemAPI.HasComponent<BlockMiningTween>(gfxContainerEnt))
                    {
                        var tween = SystemAPI.GetComponentRW<BlockMiningTween>(gfxContainerEnt);
                        tween.ValueRW.T = 0f;
                        tween.ValueRW.Duration = math.min(player.ValueRO.MiningInterval, 0.35f);
                    }
                    else
                    {
                        var tween = new BlockMiningTween
                        {
                            Duration = math.min(player.ValueRO.MiningInterval, 0.35f),
                            Power = 2f,
                            T = 0f,
                        };
                        tween.SetAsymmetryXZ(ref random, 2f);
                        ecb.AddComponent(gfxContainerEnt, tween);
                    }
                }
            }
            player.ValueRW.TimestampLastMine = time;
            ecb.Playback(state.EntityManager);
        }
    }
}


public enum TweenCycleMode
{
    Once,
    Loop, // Currently just used for previewing
}

public partial struct BlockMiningTween : IComponentData
{
    public float Duration;
    public float Power;
    public float T;
    public TweenCycleMode CycleMode;
    public float XScaleOffset;
    public float ZScaleOffset;
    public float AsymmetryScale;

    public void SetAsymmetryXZ(ref Random random, float asymmetry)
    {
        AsymmetryScale = asymmetry;
        if (random.NextBool())
        {
            XScaleOffset = random.NextFloat(asymmetry / 2f, asymmetry);
            ZScaleOffset = random.NextFloat(asymmetry, asymmetry / 2f);
        }
        else
        {

            XScaleOffset = random.NextFloat(asymmetry, asymmetry / 2f);
            ZScaleOffset = random.NextFloat(asymmetry / 2f, asymmetry);
        }
    }


    public partial struct TweeningSystem : ISystem
    {
        private Random random;
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CurveLib>();
            random = new Random();
            random.InitState();
        }

        public void OnDestroy(ref SystemState state) { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var dt = SystemAPI.Time.DeltaTime;
            var curveLib = SystemAPI.GetSingleton<CurveLib>();
            var curve = curveLib.BlockMiningTween;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (tween, matrix, entity) in SystemAPI.Query<RefRW<BlockMiningTween>, RefRW<PostTransformMatrix>>().WithEntityAccess())
            {
                var t = tween.ValueRO.T;
                var eval = curve.Evaluate(t);
                var mat = matrix.ValueRW.Value;
                float xAsym = tween.ValueRO.XScaleOffset;
                float zAsym = tween.ValueRO.ZScaleOffset;
                mat = float4x4.TRS(mat.Translation(), mat.Rotation(), new float3(math.lerp(0f + xAsym, 1f, eval), math.lerp(0f, 1f, eval), math.lerp(0f + zAsym, 1f, eval)));
                matrix.ValueRW.Value = mat;
                t += dt / tween.ValueRO.Duration;
                t = math.clamp(t, 0f, 1f);
                if (t < 1f)
                {
                    tween.ValueRW.T = t;
                }
                else
                {
                    if (tween.ValueRO.CycleMode == TweenCycleMode.Once)
                    {
                        ecb.RemoveComponent<BlockMiningTween>(entity);
                    }
                    else if (tween.ValueRO.CycleMode == TweenCycleMode.Loop)
                    {
                        tween.ValueRW.T = 0f;
                        tween.ValueRW.SetAsymmetryXZ(ref random, tween.ValueRO.AsymmetryScale);
                    }
                }
            }

            ecb.Playback(state.EntityManager);
        }
    }
}