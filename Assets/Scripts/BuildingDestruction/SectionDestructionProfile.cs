using Combat.Core;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SectionDestructionProfile", menuName = "Destruction/Section Profile")]
public class SectionDestructionProfile : ScriptableObject
{
    [Header("Health")]
    [Tooltip("Скільки HP має секція у повному стані.")]
    public float maxHealth = 200f;

    [Tooltip("Урон нижче цього порогу ігнорується (наприклад, пістолет проти бетону).")]
    public float minDamageThreshold = 1f;

    [Header("Type Multipliers (DamageType)")]
    [SerializeField] private float bulletMult = 0.25f;
    [SerializeField] private float laserMult = 0.25f;
    [SerializeField] private float explosionMult = 3.0f;
    [SerializeField] private float meleeMult = 0.1f;
    [SerializeField] private float fireMult = 0.5f;

    [Header("WeaponTag overrides (опціонально)")]
    [Tooltip("Якщо WeaponTag співпадає — множник перемножується додатково.")]
    public List<TagMultiplier> weaponTagMultipliers = new();

    [Serializable]
    public struct TagMultiplier
    {
        public WeaponTag tag;
        public float multiplier;
    }

    public float GetTypeMultiplier(DamageType type)
    {
        return type switch
        {
            DamageType.Kinetic => bulletMult,
            DamageType.Laser => laserMult,
            DamageType.Explosive => explosionMult,
            DamageType.Melee => meleeMult,
            DamageType.Fire => fireMult,
            _ => 1f
        };
    }

    public float GetWeaponTagMultiplier(WeaponTag weaponTag)
    {
        if (weaponTagMultipliers == null) return 1f;
        for (int i = 0; i < weaponTagMultipliers.Count; i++)
        {
            if (weaponTagMultipliers[i].tag == weaponTag)
                return Mathf.Max(0f, weaponTagMultipliers[i].multiplier);
        }
        return 1f;
    }
}
