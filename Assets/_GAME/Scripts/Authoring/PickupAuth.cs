using Unity.Entities;
using UnityEngine;

public class PickupAuth : MonoBehaviour
{
    public PickupType PickupType;
    public int Value = 2;

    public class Baker : Baker<PickupAuth>
    {
        public override void Bake(PickupAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(ent, new Pickup
            {
                PickupType = authoring.PickupType,
                Value = authoring.Value,
            });
        }
    }
}
