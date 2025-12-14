// IMobileInputView.cs
using UnityEngine;

public interface IMobileInputView
{
    Vector2 MoveDirection { get; }
    Vector2 LookDirection { get; }

    bool ShootPressedThisFrame { get; }
    bool ShootReleasedThisFrame { get; }
    bool ShootHeld { get; }

    bool ReloadPressedThisFrame { get; }
    bool AimHeld { get; }
    bool JumpPressedThisFrame { get; }
    bool SprintHeld { get; }

    bool TryGetWeaponSwitch(out int index);

    bool FireModeSwitchPressedThisFrame { get; }

    bool ThrowGrenadePressedThisFrame { get; }
    bool ThrowGrenadeReleasedThisFrame { get; }
    bool SwitchGrenadePressedThisFrame { get; }

    // Use / Exit
    bool UsePressedThisFrame { get; }
    bool ExitPressedThisFrame { get; }

    // whether the view belongs to a local player (some mobile setups may have remote view)
    bool IsLocal { get; }
}
