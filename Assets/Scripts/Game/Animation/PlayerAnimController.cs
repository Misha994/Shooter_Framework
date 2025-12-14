using UnityEngine;
using Zenject;

public class PlayerAnimController : CharacterAnimController
{
    private PlayerMovementController _move;
    private IInputService _input;
    private WeaponController _weaponCtrl;
    private PlayerWeaponInventory _inv;

    [Inject]
    public void Construct(PlayerMovementController move, IInputService input,
                          WeaponController weaponCtrl, PlayerWeaponInventory inv)
    {
        _move = move; _input = input; _weaponCtrl = weaponCtrl; _inv = inv;
    }

    private void OnEnable()
    {
        if (_inv != null) _inv.WeaponChanged += OnWeaponChanged;
    }
    private void OnDisable()
    {
        if (_inv != null) _inv.WeaponChanged -= OnWeaponChanged;
    }

    private void Update()
    {
        var horiz = new Vector2(_move.transform.InverseTransformDirection(_move.GetComponent<CharacterController>().velocity).x,
                                _move.transform.InverseTransformDirection(_move.GetComponent<CharacterController>().velocity).z);
        float speed = horiz.magnitude;
        SetLocomotion(speed, horiz.x, horiz.y, _move.IsGrounded);
        SetAim(_input.IsAimHeld());
    }

    private void OnWeaponChanged(WeaponBase prev, WeaponBase next)
    {
        SetupWeapon(next?.Config?.animSet);
    }

    public override void CompleteReload()
    {
        //                      /           "              "             ammo.TryReload()
        var w = _inv?.GetCurrentWeapon();
        if (w?.Ammo != null) w.Ammo.TryReload();
    }
}
