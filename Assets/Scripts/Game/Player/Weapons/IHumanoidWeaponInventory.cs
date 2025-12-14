// Assets/_Project/Code/Runtime/Combat/Weapons/IHumanoidWeaponInventory.cs
using UnityEngine;

public interface IHumanoidWeaponInventory
{
    int Count { get; }
    int CurrentIndex { get; }
    WeaponBase Current { get; }
    WeaponBase GetCurrentWeapon();

    void SwitchWeapon(int index);
    void SwitchNext();
    void SwitchPrev();

    void AddOrReplaceCurrent(WeaponConfig config);
}
