// MobileInputView.cs
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Простий фасад мобільного UI: кнопки повинні викликати OnXXXDown / OnXXXUp.
/// Exposes TryConsumeUsePressed/TryConsumeExitPressed для одно-кадрових подій.
/// Also exposes joystick Direction через lightweight wrapper.
/// </summary>
public class MobileInputView : MonoBehaviour
{
    [Header("Assign your joystick components (must expose Direction Vector2)")]
    public MonoBehaviour LookJoystick; // any component that has Vector2 Direction property/field
    public MonoBehaviour MoveJoystick;

    // shooting etc. (set from UI button events)
    [HideInInspector] public bool ShootPressedThisFrame;
    [HideInInspector] public bool ShootReleasedThisFrame;
    [HideInInspector] public bool ShootButtonPressed;

    [HideInInspector] public bool ReloadButtonPressed;
    [HideInInspector] public bool AimButtonHeld;
    [HideInInspector] public bool JumpButtonPressed;
    [HideInInspector] public bool SprintButtonHeld;

    // grenade / weapon
    private int _weaponSwitchIndex = -1;
    private bool _weaponSwitchRequested;
    public bool FireModeSwitchPressed;
    public bool ThrowGrenadePressed;
    public bool ThrowGrenadeReleasedThisFrame;
    public bool SwitchGrenadePressed;

    // Use / Exit (one-frame semantics via TryConsume)
    bool _usePressedThisFrame;
    bool _exitPressedThisFrame;
    bool _useHeld;
    bool _exitHeld;

    void LateUpdate()
    {
        // clear single-frame release flags at frame end
        ShootPressedThisFrame = false;
        ShootReleasedThisFrame = false;
        ThrowGrenadeReleasedThisFrame = false;
        // Note: _usePressedThisFrame/_exitPressedThisFrame are cleared when consumed
    }

    // ---- Joystick wrappers (tolerant to various joystick implementations) ----
    public JoystickStub LookJoystickStub => new JoystickStub(LookJoystick);
    public JoystickStub MoveJoystickStub => new JoystickStub(MoveJoystick);

    public struct JoystickStub
    {
        readonly MonoBehaviour _owner;
        public JoystickStub(MonoBehaviour owner) { _owner = owner; }
        public Vector2 Direction
        {
            get
            {
                if (_owner == null) return Vector2.zero;
                var t = _owner.GetType();
                var prop = t.GetProperty("Direction");
                if (prop != null && prop.PropertyType == typeof(Vector2))
                    return (Vector2)prop.GetValue(_owner);
                var field = t.GetField("Direction");
                if (field != null && field.FieldType == typeof(Vector2))
                    return (Vector2)field.GetValue(_owner);
                return Vector2.zero;
            }
        }
    }

    // ---- Methods to be hooked from UI Buttons (EventTrigger or Button onPointerDown/Up) ----
    // Use
    public void OnUseDown(BaseEventData _ = null) { _usePressedThisFrame = true; _useHeld = true; }

    // Exit
    public void OnExitDown(BaseEventData _ = null) { _exitPressedThisFrame = true; _exitHeld = true; }

    // Shoot
    public void OnShootDown(BaseEventData _ = null) { ShootPressedThisFrame = true; ShootButtonPressed = true; }
    public void OnShootUp(BaseEventData _ = null) { ShootButtonPressed = false; ShootReleasedThisFrame = true; }

    // Weapon switch (UI): pass index
    public void RequestWeaponSwitch(int index)
    {
        _weaponSwitchRequested = true;
        _weaponSwitchIndex = index;
    }

    public bool TryGetWeaponSwitch(out int index)
    {
        if (_weaponSwitchRequested)
        {
            index = _weaponSwitchIndex;
            _weaponSwitchRequested = false;
            _weaponSwitchIndex = -1;
            return true;
        }
        index = -1;
        return false;
    }

    public void ResetFireModeSwitch() { FireModeSwitchPressed = false; }

    // TryConsume for one-frame events
    public bool TryConsumeUsePressed()
    {
        if (_usePressedThisFrame)
        {
            _usePressedThisFrame = false;
            return true;
        }
        return false;
    }

    public bool TryConsumeExitPressed()
    {
        if (_exitPressedThisFrame)
        {
            _exitPressedThisFrame = false;
            return true;
        }
        return false;
    }

    public bool IsUseHeld() => _useHeld;
    public bool IsExitHeld() => _exitHeld;
}
