using System;

public interface IWeaponInventoryEvents
{
    event Action<WeaponBase, WeaponBase> WeaponChanged;
}
