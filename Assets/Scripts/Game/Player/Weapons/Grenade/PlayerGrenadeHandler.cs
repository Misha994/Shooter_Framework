using UnityEngine;
using Zenject;

public class PlayerGrenadeHandler : MonoBehaviour
{
    [SerializeField] private Transform throwPoint;
    [SerializeField] private float throwForce = 15f;

    private IInputService _input;
    private GrenadeInventory _inventory;

    [Inject]
    public void Construct(IInputService input, GrenadeInventory inventory)
    {
        _input = input;
        _inventory = inventory;
    }

    private void Awake()
    {
        if (!throwPoint)
            Debug.LogError("[PlayerGrenadeHandler] throwPoint is not assigned.");
    }

    private void Update()
    {
        if (_input == null || _inventory == null || !throwPoint) return;

        if (_input.IsGrenadeSwitchPressed())
        {
            _inventory.SwitchNext();
            Debug.Log($"[Grenade] Switched type idx={_inventory.CurrentIndex}, count={_inventory.CurrentCount}");
        }

        if (_input.IsGrenadeThrowPressed())
        {
            TryThrow();
        }
    }

    private void TryThrow()
    {
        var grenade = _inventory.TakeCurrent(throwPoint);
        if (grenade == null) return;

        var dir = throwPoint.forward;
        grenade.Throw(throwPoint.position, dir, throwForce);
        Debug.Log($"[Grenade] Thrown. Left in current stack: {_inventory.CurrentCount}, total: {_inventory.TotalCount}");
    }
}
