﻿using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public partial class InputSystem : SystemBase
{
    private InputAction moveInputAction;
    protected override void OnCreate()
    {
        base.OnCreate();
        EntityManager.CreateSingleton<InputData>();
        SetupPlayerInput();
    }
    private void SetupPlayerInput()
    {
        var playerInputActionMap = new InputActionMap();
        moveInputAction = playerInputActionMap.AddAction("move", type: InputActionType.PassThrough, binding: "<Gamepad>/leftStick");
        moveInputAction.AddCompositeBinding("Dpad").With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s").With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        playerInputActionMap.Enable();
    }

    protected override void OnUpdate()
    {
        var inputData = SystemAPI.GetSingletonRW<InputData>();
        inputData.ValueRW.MoveDir = moveInputAction.ReadValue<Vector2>();
    }
}


public struct InputData : IComponentData
{
    public float2 MoveDir;
}


public partial struct PlayerMoveSystem : ISystem
{
    private float3 dir3d;
    private float3 up;
    private UnityObjectRef<Transform> cam;
    private float3 lastMoveDir;

    public void OnCreate(ref SystemState state)
    {
        up = new(0f, 1f, 0f);
        state.RequireForUpdate<InputData>();
        cam = Camera.main.transform;
    }

    public void OnDestroy(ref SystemState state) { }

    //[BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var dt = SystemAPI.Time.DeltaTime;
        var inputData = SystemAPI.GetSingletonRW<InputData>();
        foreach (var (_, transform) in SystemAPI.Query<Player, RefRW<LocalTransform>>())
        {
            var dir2d = inputData.ValueRW.MoveDir;
            dir3d.xz = dir2d;
            float3 camDir = cam.Value.rotation * dir3d;
            dir3d.xz = camDir.xy;
            var dir = math.normalizesafe(dir3d);
            if (math.length(dir2d) > 0.5f)
                lastMoveDir = math.normalize(dir);

            var targetRot = quaternion.LookRotation(lastMoveDir, up);
            if (dir.Equals(float3.zero))
                targetRot = transform.ValueRO.Rotation;

            transform.ValueRW.Position += dt * 6f * dir3d;
            transform.ValueRW.Rotation = math.nlerp(transform.ValueRO.Rotation, targetRot, dt * 20f);
        }
    }
}


[UpdateAfter(typeof(TransformSystemGroup))]
public partial class CameraFollowSystem : SystemBase
{
    private Transform target;
    protected override void OnCreate()
    {
        base.OnCreate();
        RequireForUpdate<Player>();
    }

    protected override void OnUpdate()
    {
        if (target == null)
        {
            target = new GameObject("CameraFollow").transform;
            var cmCamera = GameObject.FindFirstObjectByType<CinemachineCamera>();
            cmCamera.Follow = target;
            cmCamera.LookAt = target;
        }


        foreach (var(_,transform) in SystemAPI.Query<Player,LocalTransform>())
        {
            target.position = transform.Position;
        }

    }
}
