using UnityEngine;

[DefaultExecutionOrder(+70)]
public class PlayerSeatHotkeys : MonoBehaviour
{
	[SerializeField] private PlayerVehicleInteractor interactor; // можна не задавати — знайдемо на собі

	private void Reset()
	{
		if (!interactor) interactor = GetComponent<PlayerVehicleInteractor>();
	}

	private void Update()
	{
		int number = GetPressedNumberKey();
		if (number <= 0) return;

		var vehicle = GetCurrentVehicle();
		if (!vehicle) return;

		var vm = vehicle.GetComponent<VehicleSeatManager>();
		if (!vm) return;

		// 1..10 → спробувати гарячий ключ/індекс
		if (vm.TryChangeSeatByHotkey(gameObject, number))
		{
			Debug.Log($"[SeatHotkeys] Switched to seat #{number}");
		}
		else
		{
			Debug.Log($"[SeatHotkeys] Seat #{number} unavailable.");
		}
	}

	private GameObject GetCurrentVehicle()
	{
		if (interactor && interactor.CurrentVehicle) return interactor.CurrentVehicle;

		// фолбек: шукаємо VehicleSeat в батьках (якщо гравець приаттачений до місця)
		var seat = GetComponentInParent<VehicleSeat>();
		return seat ? seat.GetComponentInParent<VehicleSeatManager>()?.gameObject : null;
	}

	/// <summary>Повертає 1..10 коли натиснута відповідна цифра (0 = 10).</summary>
	private int GetPressedNumberKey()
	{
#if ENABLE_INPUT_SYSTEM
		var kb = UnityEngine.InputSystem.Keyboard.current;
		if (kb == null) return 0;

		if (kb.digit1Key.wasPressedThisFrame) return 1;
		if (kb.digit2Key.wasPressedThisFrame) return 2;
		if (kb.digit3Key.wasPressedThisFrame) return 3;
		if (kb.digit4Key.wasPressedThisFrame) return 4;
		if (kb.digit5Key.wasPressedThisFrame) return 5;
		if (kb.digit6Key.wasPressedThisFrame) return 6;
		if (kb.digit7Key.wasPressedThisFrame) return 7;
		if (kb.digit8Key.wasPressedThisFrame) return 8;
		if (kb.digit9Key.wasPressedThisFrame) return 9;
		if (kb.digit0Key.wasPressedThisFrame) return 10;
		return 0;
#else
        if (Input.GetKeyDown(KeyCode.Alpha1)) return 1;
        if (Input.GetKeyDown(KeyCode.Alpha2)) return 2;
        if (Input.GetKeyDown(KeyCode.Alpha3)) return 3;
        if (Input.GetKeyDown(KeyCode.Alpha4)) return 4;
        if (Input.GetKeyDown(KeyCode.Alpha5)) return 5;
        if (Input.GetKeyDown(KeyCode.Alpha6)) return 6;
        if (Input.GetKeyDown(KeyCode.Alpha7)) return 7;
        if (Input.GetKeyDown(KeyCode.Alpha8)) return 8;
        if (Input.GetKeyDown(KeyCode.Alpha9)) return 9;
        if (Input.GetKeyDown(KeyCode.Alpha0)) return 10;
        return 0;
#endif
	}
}
