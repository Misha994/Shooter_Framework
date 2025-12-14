// NewInputService.cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Сервіс для DI (не Mono). Використовує PlayerInputActions asset,
/// підписується на performed/canceled і зберігає фреймові прапорці.
/// ВАЖЛИВО: викликайте Tick() кожен кадр (наприклад з GameLoop або з Mono який реєструє сервіс),
/// щоб очищати пер-frame флаги.
/// </summary>
public class NewInputService : IInputService, IDisposable
{
    private readonly PlayerInputActions _actions;
    private readonly Dictionary<string, InputAction> _map = new();

    private readonly HashSet<string> _pressed = new();
    private readonly HashSet<string> _released = new();

    public bool IsLocal { get; private set; }

    public NewInputService(bool isLocal = true)
    {
        _actions = new PlayerInputActions();
        _actions.Enable();
        IsLocal = isLocal;

        var asset = _actions.asset;
        var map = asset?.FindActionMap("Player", true);
        if (map != null)
        {
            Register(map.FindAction("Move", false), "Move");
            Register(map.FindAction("Look", false), "Look");
            Register(map.FindAction("Fire", false), "Fire");
            Register(map.FindAction("Reload", false), "Reload");
            Register(map.FindAction("Aim", false), "Aim");
            Register(map.FindAction("Jump", false), "Jump");
            Register(map.FindAction("Sprint", false), "Sprint");
            Register(map.FindAction("Weapon1", false), "Weapon1");
            Register(map.FindAction("Weapon2", false), "Weapon2");
            Register(map.FindAction("Weapon3", false), "Weapon3");
            Register(map.FindAction("ThrowGrenade", false), "ThrowGrenade");
            Register(map.FindAction("SwitchGrenade", false), "SwitchGrenade");
            Register(map.FindAction("FireModeSwitch", false), "FireModeSwitch");
            Register(map.FindAction("Use", false), "Use");
            Register(map.FindAction("Exit", false), "Exit");
        }
    }

    private void Register(InputAction act, string key)
    {
        if (act == null) return;
        _map[key] = act;
        act.performed += ctx => _pressed.Add(key);
        act.canceled += ctx => _released.Add(key);
    }

    /// <summary> Tick должен вызваться раз в кадр из MonoBehaviour (например из GameManager.Update) </summary>
    public void Tick()
    {
        _pressed.Clear();
        _released.Clear();
    }

    // Axes
    public Vector2 GetMoveAxis() => _map.TryGetValue("Move", out var a) ? a.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 GetLookDelta() => _map.TryGetValue("Look", out var a) ? a.ReadValue<Vector2>() : Vector2.zero;

    // Buttons
    public bool IsShootPressed() => _pressed.Contains("Fire");
    public bool IsShootReleased() => _released.Contains("Fire");
    public bool IsShootHeld() => _map.TryGetValue("Fire", out var fa) && fa.IsPressed();

    public bool IsReloadPressed() => _pressed.Contains("Reload");
    public bool IsAimHeld() => _map.TryGetValue("Aim", out var a) && a.IsPressed();
    public bool IsJumpPressed() => _pressed.Contains("Jump");
    public bool IsSprintHeld() => _map.TryGetValue("Sprint", out var a) && a.IsPressed();

    public bool IsWeaponSwitchRequested(out int index)
    {
        index = -1;
        if (_pressed.Contains("Weapon1")) { index = 0; return true; }
        if (_pressed.Contains("Weapon2")) { index = 1; return true; }
        if (_pressed.Contains("Weapon3")) { index = 2; return true; }
        return false;
    }

    public bool IsFireModeSwitchPressed() => _pressed.Contains("FireModeSwitch");

    public bool IsGrenadeThrowPressed() => _pressed.Contains("ThrowGrenade");
    public bool IsGrenadeReleased() => _released.Contains("ThrowGrenade");
    public bool IsGrenadeSwitchPressed() => _pressed.Contains("SwitchGrenade");

    public bool IsUsePressed() => _pressed.Contains("Use");
    public bool IsExitPressed() => _pressed.Contains("Exit");

    public void Dispose()
    {
        _actions?.Disable();
    }
}
