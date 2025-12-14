// Interfaces/IWeaponFactory.cs
using UnityEngine;

public interface IWeaponFactory
{
    WeaponBase Create(WeaponConfig config, Transform parent);
}
