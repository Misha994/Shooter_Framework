using UnityEngine;

public interface IInputService
{
    Vector2 GetMoveAxis();
    Vector2 GetLookDelta();

    bool IsShootPressed();
    bool IsShootReleased();
    bool IsShootHeld();

    bool IsReloadPressed();
    bool IsAimHeld();
    bool IsJumpPressed();
    bool IsSprintHeld();

    bool IsWeaponSwitchRequested(out int index);
    bool IsFireModeSwitchPressed();

    bool IsGrenadeThrowPressed();
    bool IsGrenadeReleased();
    bool IsGrenadeSwitchPressed();

    bool IsUsePressed();
    bool IsExitPressed();

    bool IsLocal { get; }
}
