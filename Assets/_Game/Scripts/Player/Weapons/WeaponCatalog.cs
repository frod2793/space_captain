using System;
using System.Collections.Generic;
using UnityEngine;

public static class WeaponCatalog
{
    private static WeaponDataSO[] s_all;

    public static IReadOnlyList<WeaponDataSO> All => s_all ??= Resources.LoadAll<WeaponDataSO>("Weapons");

    public static WeaponDataSO Get(string weaponID)
    {
        if (string.IsNullOrEmpty(weaponID))
        {
            return null;
        }

        for (int i = 0; i < All.Count; i++)
        {
            WeaponDataSO weapon = All[i];
            if (weapon != null && string.Equals(weapon.WeaponID, weaponID, StringComparison.OrdinalIgnoreCase))
            {
                return weapon;
            }
        }

        return null;
    }
}
