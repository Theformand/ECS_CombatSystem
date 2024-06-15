using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using static Utils;
using Random = Unity.Mathematics.Random;



[UpdateInGroup(typeof(SkillSystemGroup))]
public partial struct ProjectileSkillSystem : ISystem
{
    private EntityQuery entityQuery;
    private Random rng;

    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<Player>();
        entityQuery = state.GetEntityQuery(typeof(Enemy), typeof(LocalTransform));
        rng = new Random();
        rng.InitState();
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var physicsWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().PhysicsWorld;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var playerEnt = SystemAPI.GetSingletonEntity<Player>();
        var playerTransform = SystemAPI.GetComponent<LocalTransform>(playerEnt);

        var playerFwd = playerTransform.Forward();
        float3 playerPos = playerTransform.Position;
        var time = (float)SystemAPI.Time.ElapsedTime;
        NativeArray<LocalTransform> allTransforms = entityQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        NativeArray<Enemy> allEnemies = entityQuery.ToComponentDataArray<Enemy>(Allocator.Temp);
                
        foreach (var (qSkilldata, qRl, activationData) in SystemAPI.Query<RefRW<BulletSkillData>, RefRW<SkillReloadData>, SkillActivationData>())
        {
            ref readonly var skillData = ref qSkilldata.ValueRO;
            ref readonly var reload = ref qRl.ValueRO;
            ref var skillDataW = ref qSkilldata.ValueRW;
            ref var reloadW = ref qRl.ValueRW;

            var isReloaded = reload.TimeCurrent <= 0f;
            bool canShoot = time > skillData.TimeStampNextShot && reload.MagCountCurrent > 0;
            if (!canShoot)
                continue;

            bool targetFound = false;
            LocalTransform target = LocalTransform.Identity;
            float3 aimDir = float3.zero;

            // Aqcuire target based on the targeting mode of the skill
            if (activationData.TargetingMode == SkillTargetingMode.CLOSEST)
            {
                int idxTarget = Utils.GetIndexOfClosestWithLOS(ref allTransforms, ref playerPos, activationData.ActivationRangeSqr, ref physicsWorld);

                if (idxTarget >= 0)
                {
                    targetFound = true;
                    target = allTransforms[idxTarget];
                    aimDir = math.normalizesafe(target.Position - playerPos);
                    aimDir.y = 0f;
                }
            }
            else if (activationData.TargetingMode == SkillTargetingMode.HIGHEST_HP_MAX)
            {
                // - Find max base HP for all enemies in range
                // - Find all enemies in range with this maxHP value
                // - Pick the closest one of those

                NativeList<LocalTransform> maxHPTransforms = new NativeList<LocalTransform>(allEnemies.Length, Allocator.Temp);
                int highestMax = 0;
                //If I was a half decent programmer I could probably do without 2 loops
                for (int i = 0; i < allEnemies.Length; i++)
                {
                    var curr = allEnemies[i].HPMax;
                    if (curr > highestMax)
                        highestMax = curr;
                }
                
                               
                for (int i = 0; i < allEnemies.Length; i++)
                {
                    if (allEnemies[i].HPMax == highestMax)
                        maxHPTransforms.Add(allTransforms[i]);
                }
                var tArray = maxHPTransforms.AsArray();
                int idxTarget = Utils.GetIndexOfClosestWithLOS(ref tArray, ref playerPos, activationData.ActivationRangeSqr, ref physicsWorld);
                if (idxTarget >= 0)
                {
                    target = allTransforms[idxTarget];
                    targetFound = true;
                    aimDir = math.normalizesafe(target.Position - playerPos);
                }

                maxHPTransforms.Dispose();
            }
            else if (activationData.TargetingMode == SkillTargetingMode.PLAYER_FWD || activationData.TargetingMode == SkillTargetingMode.PLAYER_BACK)
            {
                float3 referenceFwd = activationData.TargetingMode == SkillTargetingMode.PLAYER_FWD ? playerFwd : -playerFwd;
                var filter = new CollisionFilter()
                {
                    CollidesWith = 1u << 1, //Environment layer
                    BelongsTo = 1u << 3, 
                    GroupIndex = 0
                };
                NativeList<DistanceHit> hitList = new(Allocator.Temp);
                physicsWorld.OverlapSphere(playerPos, activationData.ActivationRange, ref hitList, filter);
                
                if (hitList.Length == 0)
                    continue;

                for (int i = 0; i < hitList.Length; i++)
                {
                    var dirToTarget = math.normalizesafe(hitList[i].Position - playerPos);
                    if (math.dot(referenceFwd, dirToTarget) > 0.92f)
                    {
                        targetFound = true;
                        break;
                    }
                }
                aimDir = referenceFwd;
            }
            
            //TODO: if dt > shotInterval, do an extra loop to fire the missing bullets and shift their starting position accordingly so fire rate isnt limtied by framerate
            //float bulletTimer = 0f;
            //bulletTimer += dt;
            //float shotInterval = 1f / shotData.AttacksPerSecond;
            //while (bulletTimer > shotInterval)
            //{
            //    bulletTimer -= shotInterval;
            //}
            if (isReloaded && targetFound)
            {
                var up = math.up();
                //TODO: Figure out how to notify GO land that we need Audio here
                aimDir.y = 0;
                for (int i = 0; i < skillData.NumBulletsPerAttack; i++)
                {
                    float angle = 0f;
                    float3 dir;
                    if (skillData.NumBulletsPerAttack == 1)
                    {
                        dir = aimDir;
                    }
                    else
                    {
                        float startingAngle = (skillData.NumBulletsPerAttack - 1) * (skillData.AngleSpread * 0.5f);
                        angle = -startingAngle + (i * skillData.AngleSpread);
                    }
                    float spread = rng.NextFloat(-skillData.AccuracySpread, skillData.AccuracySpread);
                    quaternion rotOffset = quaternion.AxisAngle(up, math.radians(angle + spread));
                    dir = math.mul(rotOffset, aimDir);

                    var shotArgs = ShootBulletArgs.FromBulletSkillData(skillData);
                    shotArgs.Origin = playerPos + up; 
                    shotArgs.Direction = dir;
                    Utils.ShootBulletInDirection(ref shotArgs, ref ecb); 
                }
                skillDataW.TimeStampNextShot = time + (1f / skillData.AttacksPerSecond);
                Utils.HandleReload(ref reloadW);
            }
        }

        ecb.Playback(state.EntityManager);
        allEnemies.Dispose();
        allTransforms.Dispose();
    }
}
