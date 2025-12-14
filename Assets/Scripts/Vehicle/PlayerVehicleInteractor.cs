using UnityEngine;

public class PlayerVehicleInteractor : MonoBehaviour
{
	[Header("Raycast")]
	[SerializeField] private Transform rayOrigin;   // Зазвичай це PlayerCamera transform
	[SerializeField] private float useRange = 3.5f;
	[SerializeField] private LayerMask mask = ~0;

	private GameObject _currentVehicle;

	public GameObject CurrentVehicle => _currentVehicle;

	private void Update()
	{
		// Якщо вже сидимо — слухаємо вихід
		if (_currentVehicle)
		{
			if (IsExitPressed())
			{
				var vm = _currentVehicle.GetComponent<VehicleSeatManager>();
				if (vm && vm.TryExit(gameObject))
				{
					_currentVehicle = null;
					Debug.Log("[Interactor] Вийшов з транспорту.");
				}
			}
			return;
		}

		// Посадка
		if (!IsUsePressed()) return;

		if (!Physics.Raycast(rayOrigin.position, rayOrigin.forward, out var hit, useRange, mask, QueryTriggerInteraction.Collide))
			return;

		var vmgr = hit.collider.GetComponentInParent<VehicleSeatManager>();
		if (!vmgr) return;

		// Спочатку пробуємо сісти як Driver, потім Gunner
		if (vmgr.TryEnter(gameObject, VehicleSeatRole.Driver))
		{
			_currentVehicle = vmgr.gameObject;
			Debug.Log("[Interactor] Сів у транспорт (Driver).");
		}
		else if (vmgr.TryEnter(gameObject, VehicleSeatRole.Gunner))
		{
			_currentVehicle = vmgr.gameObject;
			Debug.Log("[Interactor] Сів у транспорт (Gunner).");
		}
		else
		{
			Debug.Log("[Interactor] Немає вільних місць.");
		}
	}

	private bool IsUsePressed()
	{
		// Можна адаптувати під нову input system, якщо треба
		return Input.GetKeyDown(KeyCode.E);
	}

	private bool IsExitPressed()
	{
		// Можна адаптувати під нову input system, якщо треба
		return Input.GetKeyDown(KeyCode.F);
	}
}
