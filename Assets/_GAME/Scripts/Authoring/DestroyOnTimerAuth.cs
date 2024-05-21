
using Unity.Entities;
using UnityEngine;

public class DestroyOnTimerAuth : MonoBehaviour
{
    public float Time;
    public class Baker : Baker<DestroyOnTimerAuth>
    {
        public override void Bake(DestroyOnTimerAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new DestroyOnTimer
            {
                Time = authoring.Time 
            });
        }
    }
}
