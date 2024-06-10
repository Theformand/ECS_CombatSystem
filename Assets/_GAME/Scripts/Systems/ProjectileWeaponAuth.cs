using Unity.Entities;

public struct WeaponLib : IComponentData
{
    public int test;
    public Entity GetWeaponPrefab(DynamicBuffer<EntityLookupData> buf, int guidHash)
    {
        for (int i = 0; i < buf.Length; i++)
        {
            if (buf[i].GuidHash == guidHash)
                return buf[i].WeaponPrefab;
        }
        return Entity.Null;
    }
}

public struct EntityLookupData : IBufferElementData
{
    public Entity WeaponPrefab;
    public int GuidHash;
}

