using Unity.Entities;
using Unity.Mathematics;

public class ToEntities
{
    public static void SendMapData(MapGenOutput data)
    {
        World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<MapSystem>().SetMapData(data);
    }

    public static void EquipWeapon(WeaponSkillData skillData)
    {
        World.DefaultGameObjectInjectionWorld.GetExistingSystemManaged<WeaponSpawnSystem>().EquipWeapon(skillData);
    }
}


public class ToGameObjects
{
    public static void PlaySoundAtPosition(int hash, float3 position)
    {
        
    }
}
