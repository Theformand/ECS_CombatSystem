using UnityEngine;

public class MonoWeaponSpawner : MonoBehaviour
{
    public WeaponSkillData skillData;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ToEntities.EquipWeapon(skillData);
    }
}
