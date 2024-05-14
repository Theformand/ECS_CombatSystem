using Unity.Entities;
using UnityEngine;

public class GizmoDrawer : MonoBehaviour
{
    private SystemHandle system;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        system = World.DefaultGameObjectInjectionWorld.GetExistingSystem<BeamSkillSystem>();
    }

    // Update is called once per frame
    void OnDrawGizmos()
    {
        //system.DrawGizmos();
    }
}
