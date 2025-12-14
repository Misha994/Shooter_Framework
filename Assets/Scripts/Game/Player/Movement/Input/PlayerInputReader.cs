// PlayerInputReader.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// MonoBehaviour реалізація IInputService користуючись New Input System.
/// Працює самостійно (реєструє дії через asset.FindAction) та веде фреймові прапорці.
/// Поставте цей компонент на локального гравця і переконайтесь, що він IsLocal = true.
/// </summary>
[DefaultExecutionOrder(+100)]
public class PlayerInputReader : MonoBehaviour, IInputService
{
    [SerializeField] private bool isLocal = true; // виставляй на prefab (локальний гравець = true)
    private PlayerInputActions _actions;

    // Кеш action'ів (nullable)
    private InputAction aMove, aLook;
    private InputAction aFire, aReload, aAim, aJump, aSprint;
    private InputAction aWeapon1, aWeapon2, aWeapon3;
    private InputAction aThrowGrenade, aSwitchGrenade;
    private InputAction aFireModeSwitch;
    private InputAction aUse, aExit;

    // pressed/released flags set by callbacks; cleared each LateUpdate
    private readonly Dictionary<string, bool> _pressedThisFrame = new();
    private readonly Dictionary<string, bool> _releasedThisFrame = new();

    private void Awake()
    {
        _actions = new PlayerInputActions();
        _actions.Enable();

        // try to find actions by name inside the asset (works even if generated wrapper lacks specific properties)
        var asset = _actions.asset;
        var map = asset?.FindActionMap("Player", true);
        if (map != null)
        {
            aMove = map.FindAction("Move", true);
            aLook = map.FindAction("Look", true);

            aFire = map.FindAction("Fire", false);
            aReload = map.FindAction("Reload", false);
            aAim = map.FindAction("Aim", false);
            aJump = map.FindAction("Jump", false);
            aSprint = map.FindAction("Sprint", false);

            aWeapon1 = map.FindAction("Weapon1", false);
            aWeapon2 = map.FindAction("Weapon2", false);
            aWeapon3 = map.FindAction("Weapon3", false);

            aThrowGrenade = map.FindAction("ThrowGrenade", false);
            aSwitchGrenade = map.FindAction("SwitchGrenade", false);

            aFireModeSwitch = map.FindAction("FireModeSwitch", false);

            aUse = map.FindAction("Use", false);
            aExit = map.FindAction("Exit", false);
        }

        // subscribe to performed/canceled for actions we care about (buttons)
        SubscribeIfExist(aFire, "Fire");
        SubscribeIfExist(aReload, "Reload");
        SubscribeIfExist(aAim, "Aim");
        SubscribeIfExist(aJump, "Jump");
        SubscribeIfExist(aSprint, "Sprint");
        SubscribeIfExist(aWeapon1, "Weapon1");
        SubscribeIfExist(aWeapon2, "Weapon2");
        SubscribeIfExist(aWeapon3, "Weapon3");
        SubscribeIfExist(aThrowGrenade, "ThrowGrenade");
        SubscribeIfExist(aSwitchGrenade, "SwitchGrenade");
        SubscribeIfExist(aFireModeSwitch, "FireModeSwitch");
        SubscribeIfExist(aUse, "Use");
        SubscribeIfExist(aExit, "Exit");
    }

    private void SubscribeIfExist(InputAction act, string key)
    {
        if (act == null) return;
        act.performed += ctx => { _pressedThisFrame[key] = true; };
        act.canceled += ctx => { _releasedThisFrame[key] = true; };
        // Note: hold state queried via act.IsPressed() when needed
    }

    private void LateUpdate()
    {
        // clear per-frame pressed/released flags (they are valid only during the frame they were set)
        _pressedThisFrame.Clear();
        _releasedThisFrame.Clear();
    }

    // ---- Read axes ----
    public Vector2 GetMoveAxis() => aMove != null ? aMove.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 GetLookDelta() => aLook != null ? aLook.ReadValue<Vector2>() : Vector2.zero;

    // ---- Weapons & actions ----
    public bool IsShootPressed() => _pressedThisFrame.ContainsKey("Fire");
    public bool IsShootReleased() => _releasedThisFrame.ContainsKey("Fire");
    public bool IsShootHeld() => aFire != null && aFire.IsPressed();

    public bool IsReloadPressed() => _pressedThisFrame.ContainsKey("Reload");
    public bool IsAimHeld() => aAim != null && aAim.IsPressed();
    public bool IsJumpPressed() => _pressedThisFrame.ContainsKey("Jump");
    public bool IsSprintHeld() => aSprint != null && aSprint.IsPressed();

    public bool IsWeaponSwitchRequested(out int index)
    {
        index = -1;
        if (_pressedThisFrame.ContainsKey("Weapon1")) { index = 0; return true; }
        if (_pressedThisFrame.ContainsKey("Weapon2")) { index = 1; return true; }
        if (_pressedThisFrame.ContainsKey("Weapon3")) { index = 2; return true; }
        return false;
    }

    public bool IsFireModeSwitchPressed() => _pressedThisFrame.ContainsKey("FireModeSwitch");

    public bool IsGrenadeThrowPressed() => _pressedThisFrame.ContainsKey("ThrowGrenade");
    public bool IsGrenadeReleased() => _releasedThisFrame.ContainsKey("ThrowGrenade");
    public bool IsGrenadeSwitchPressed() => _pressedThisFrame.ContainsKey("SwitchGrenade");

    public bool IsUsePressed() => _pressedThisFrame.ContainsKey("Use");
    public bool IsExitPressed() => _pressedThisFrame.ContainsKey("Exit");

    public bool IsLocal => isLocal;
}
