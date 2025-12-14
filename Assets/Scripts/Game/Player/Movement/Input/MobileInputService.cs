// MobileInputService.cs
using UnityEngine;
using Zenject;

public class MobileInputService : IInputService
{
    private readonly IMobileInputView _view;

    [Inject]
    public MobileInputService(IMobileInputView view)
    {
        _view = view;
    }

    public Vector2 GetMoveAxis() => _view?.MoveDirection ?? Vector2.zero;
    public Vector2 GetLookDelta() => _view?.LookDirection ?? Vector2.zero;

    public bool IsShootPressed() => _view?.ShootPressedThisFrame ?? false;
    public bool IsShootReleased() => _view?.ShootReleasedThisFrame ?? false;
    public bool IsShootHeld() => _view?.ShootHeld ?? false;

    public bool IsReloadPressed() => _view?.ReloadPressedThisFrame ?? false;
    public bool IsAimHeld() => _view?.AimHeld ?? false;
    public bool IsJumpPressed() => _view?.JumpPressedThisFrame ?? false;
    public bool IsSprintHeld() => _view?.SprintHeld ?? false;

    public bool IsWeaponSwitchRequested(out int index) => _view?.TryGetWeaponSwitch(out index) ?? (index = -1) == -1 ? false : false;
    public bool IsFireModeSwitchPressed() => _view?.FireModeSwitchPressedThisFrame ?? false;

    public bool IsGrenadeThrowPressed() => _view?.ThrowGrenadePressedThisFrame ?? false;
    public bool IsGrenadeReleased() => _view?.ThrowGrenadeReleasedThisFrame ?? false;
    public bool IsGrenadeSwitchPressed() => _view?.SwitchGrenadePressedThisFrame ?? false;

    public bool IsUsePressed() => _view?.UsePressedThisFrame ?? false;
    public bool IsExitPressed() => _view?.ExitPressedThisFrame ?? false;

    public bool IsLocal => _view?.IsLocal ?? false;
}
