using Unity.Entities;
using UnityEngine;

public class KnockbackAuth : MonoBehaviour
{
    public KnockBackSettings Settings;
    public class Baker : Baker<KnockbackAuth>
    {
        public override void Bake(KnockbackAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.None);
            AddComponent(ent, new KnockBack()
            {
                Settings = authoring.Settings,
                T = 0f
            });
        }
    }
}
