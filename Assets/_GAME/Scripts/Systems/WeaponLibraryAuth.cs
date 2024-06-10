using Unity.Entities;
using UnityEngine;

public class WeaponLibraryAuth : MonoBehaviour
{
    public WeaponSkillData[] WeaponSkillDatas;

    public class Baker : Baker<WeaponLibraryAuth>
    {
        public override void Bake(WeaponLibraryAuth authoring)
        {
            var ent = GetEntity(TransformUsageFlags.None);
            var buf = AddBuffer<EntityLookupData>(ent);
            var datas = authoring.WeaponSkillDatas;

            for (int i = 0; i < datas.Length; i++)
            {
                var data = datas[i];
                var tup = new EntityLookupData { GuidHash = Animator.StringToHash(data.Guid) };
                if (data is ProjectileSkillData psd)
                    tup.WeaponPrefab = psd.Bake(this);


                buf.Add(tup);
            }
            AddComponent(ent, new WeaponLib());
        }

    }
}
