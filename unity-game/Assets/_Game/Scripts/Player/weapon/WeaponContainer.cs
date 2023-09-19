using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "weaponContainer",menuName = "game/weapon contariner")]
public class WeaponContainer : ScriptableObject
{
    public WeaponSlot[] weapons;

    public GameObject GetWeaponWithID(int ID)
    {
        foreach (var weapon in weapons)
        {
            if (weapon.weaponID == ID) return weapon.prefab;
        }

        return null;
    }

}

[System.Serializable]
public struct WeaponSlot
{
    public int weaponID;
    public GameObject prefab;
}
