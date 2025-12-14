using System.Collections.Generic;
using UnityEngine;

public class VehicleSeatManager : MonoBehaviour
{
	[Header("Seats")]
	[SerializeField] private VehicleSeat[] seats;
	[SerializeField] private Transform defaultExitPoint;

	// Стан гравця ДО входу в техніку (для повернення орієнтації)
	private class PreSeatState
	{
		public Quaternion worldRotation;
	}

	private readonly Dictionary<GameObject, PreSeatState> _preSeatStates =
		new Dictionary<GameObject, PreSeatState>();

	// Які компоненти були вимкнені при вході (рух, зброя, колайдер, рендери)
	private class DisabledState
	{
		public readonly List<Behaviour> behaviours = new List<Behaviour>();
		public readonly List<CharacterController> colliders = new List<CharacterController>();
		public readonly List<Renderer> renderers = new List<Renderer>();
	}

	private readonly Dictionary<GameObject, DisabledState> _disabledStates =
		new Dictionary<GameObject, DisabledState>();

	public bool TryEnter(GameObject occupant, VehicleSeatRole role)
	{
		if (!occupant || IsAlreadyInside(occupant)) return false;

		var seat = FindFreeSeat(role);
		if (!seat) return false;

		// Запамʼятовуємо ротацію гравця ДО входу (для виходу)
		if (!_preSeatStates.TryGetValue(occupant, out var state))
		{
			state = new PreSeatState();
			_preSeatStates[occupant] = state;
		}
		state.worldRotation = occupant.transform.rotation;

		// Готуємо гравця до перебування в техніці (відключаємо рух, зброю, рендери)
		PrepareOccupantForVehicle(occupant);

		MountOccupant(seat, occupant);
		return true;
	}

	public bool TryExit(GameObject occupant)
	{
		if (!occupant) return false;

		var seat = FindSeatByOccupant(occupant);
		if (!seat) return false;

		UnmountOccupant(seat, occupant);
		return true;
	}

	private void MountOccupant(VehicleSeat seat, GameObject occupant)
	{
		if (!seat || !occupant) return;

		// На всякий випадок — переключення камери (далі VehicleSeat теж це робить)
		var input = occupant.GetComponent<IInputService>();
		TrySwitchCameraToSeatForLocal(input, seat);

		Transform mount = seat.mount ? seat.mount : seat.transform;
		occupant.transform.SetParent(mount, false);
		occupant.transform.localPosition = Vector3.zero;
		occupant.transform.localRotation = Quaternion.identity;
		occupant.SetActive(true);

		seat.SetOccupied(occupant, true);

		Debug.Log($"[SeatManager] {occupant.name} ENTER → {seat.role} on {name}");
	}

	private void UnmountOccupant(VehicleSeat seat, GameObject occupant)
	{
		if (!seat || !occupant) return;

		// 0. Куди виходимо по позиції
		Vector3 exitPos = GetDesiredExitWorldPoint(seat);

		// 0.5. Орієнтація при виході:
		//      беремо Y-оберт з того стану, який був ДО входу в техніку.
		Quaternion exitRot;
		if (_preSeatStates.TryGetValue(occupant, out var state))
		{
			Vector3 e = state.worldRotation.eulerAngles;
			exitRot = Quaternion.Euler(0f, e.y, 0f);
		}
		else
		{
			// Фолбек — якщо з якоїсь причини не зберегли стан
			Vector3 e = occupant.transform.eulerAngles;
			exitRot = Quaternion.Euler(0f, e.y, 0f);
		}

		// 1. Розконектимо інпут від цього сидіння (запобігаємо фантомному керуванню)
		var consumers = seat.GetInputConsumers();
		foreach (var consumer in consumers)
			consumer.SetSeatInput(null);

		// 2. Кажемо сидінню, що воно звільнилось:
		//    - камера повернеться на "піхоту"
		//    - техніка перестане слухати цей інпут
		seat.SetOccupied(null, false);

		// 3. Відʼєднуємо гравця від mount і переносимо в точку виходу
		occupant.transform.SetParent(null, true);
		occupant.SetActive(true);
		occupant.transform.SetPositionAndRotation(exitPos, exitRot);

		// 4. Тепер, коли гравець вже ПОЗА технікою,
		//    повертаємо CharacterController, рух, модель, зброю, гранати
		RestoreOccupantAfterVehicle(occupant);

		// 5. Чистимо кеш стану — при наступному вході запишемо новий
		_preSeatStates.Remove(occupant);

		Debug.Log($"[SeatManager] {occupant.name} EXIT from {seat.role} on {name}");
	}

	#region Пошук сидінь

	private VehicleSeat FindFreeSeat(VehicleSeatRole role)
	{
		foreach (var seat in seats)
		{
			if (!seat) continue;
			if (seat.role == role && !seat.IsOccupied)
				return seat;
		}
		return null;
	}

	private VehicleSeat FindSeatByOccupant(GameObject occupant)
	{
		foreach (var seat in seats)
		{
			if (!seat) continue;
			if (seat.IsOccupied && seat.Occupant == occupant)
				return seat;
		}
		return null;
	}

	private bool IsAlreadyInside(GameObject occupant)
	{
		return FindSeatByOccupant(occupant) != null;
	}

	#endregion

	#region Вихідна точка

	private Vector3 GetDesiredExitWorldPoint(VehicleSeat seat)
	{
		if (seat != null && seat.localExitPoint != null)
			return seat.localExitPoint.position;

		if (defaultExitPoint != null)
			return defaultExitPoint.position;

		// Фолбек — праворуч від техніки
		return transform.TransformPoint(Vector3.right * 1.5f);
	}

	#endregion

	#region Пересадка по хоткеях

	/// <summary>
	/// Пересадка гравця по хоткею 1..10 в межах цієї техніки.
	/// ВАЖЛИВО: тут ми НЕ чіпаємо стан гравця (він уже вимкнений для техніки),
	///          не змінюємо _preSeatStates — стан до входу той самий.
	/// </summary>
	public bool TryChangeSeatByHotkey(GameObject occupant, int hotkey)
	{
		if (!occupant) return false;
		if (hotkey <= 0) return false;

		var currentSeat = FindSeatByOccupant(occupant);
		if (!currentSeat)
		{
			Debug.Log("[SeatManager] Occupant is not in this vehicle.");
			return false;
		}

		var targetSeat = FindSeatByHotkey(hotkey);
		if (!targetSeat)
		{
			Debug.Log($"[SeatManager] Seat #{hotkey} not found.");
			return false;
		}

		if (targetSeat == currentSeat)
		{
			Debug.Log($"[SeatManager] Already in seat #{hotkey}.");
			return false;
		}

		if (targetSeat.IsOccupied)
		{
			Debug.Log($"[SeatManager] Seat #{hotkey} already occupied.");
			return false;
		}

		// --- Пересадка без активації CharacterController між сидіннями ---

		// 1. Розконектимо інпут від старого сидіння
		var oldConsumers = currentSeat.GetInputConsumers();
		foreach (var consumer in oldConsumers)
			consumer.SetSeatInput(null);

		// 2. Звільняємо старе сидіння (рух/колайдери/рендери НЕ чіпаємо)
		currentSeat.SetOccupied(null, false);

		// 3. Пересаджуємо трансформ у новий mount
		Transform mount = targetSeat.mount ? targetSeat.mount : targetSeat.transform;
		occupant.transform.SetParent(mount, false);
		occupant.transform.localPosition = Vector3.zero;
		occupant.transform.localRotation = Quaternion.identity;

		// 4. Займаємо нове сидіння (інпут + камера)
		targetSeat.SetOccupied(occupant, true);

		Debug.Log($"[SeatManager] {occupant.name} switched to seat #{hotkey}");
		return true;
	}

	private VehicleSeat FindSeatByHotkey(int hotkey)
	{
		foreach (var seat in seats)
		{
			if (!seat) continue;
			if (seat.hotkey == hotkey)
				return seat;
		}
		return null;
	}

	#endregion

	#region Керування станом гравця (рух/зброя/рендери)

	private void PrepareOccupantForVehicle(GameObject occ)
	{
		if (occ == null) return;

		if (!_disabledStates.TryGetValue(occ, out var state))
		{
			state = new DisabledState();
			_disabledStates[occ] = state;
		}

		state.behaviours.Clear();
		state.colliders.Clear();
		state.renderers.Clear();

		// Рух гравця
		var move = occ.GetComponentInChildren<PlayerMovementController>(true);
		if (move != null && move.enabled)
		{
			move.enabled = false;
			state.behaviours.Add(move);
		}

		// Зброя / стрільба
		var weaponInventory = occ.GetComponentInChildren<PlayerWeaponInventory>(true);
		if (weaponInventory != null && weaponInventory.enabled)
		{
			weaponInventory.enabled = false;
			state.behaviours.Add(weaponInventory);
		}

		var weaponController = occ.GetComponentInChildren<WeaponController>(true);
		if (weaponController != null && weaponController.enabled)
		{
			weaponController.enabled = false;
			state.behaviours.Add(weaponController);
		}

		// Гранати
		var grenadeHandler = occ.GetComponentInChildren<PlayerGrenadeHandler>(true);
		if (grenadeHandler != null && grenadeHandler.enabled)
		{
			grenadeHandler.enabled = false;
			state.behaviours.Add(grenadeHandler);
		}

		// CharacterController (колайдер руху)
		var cc = occ.GetComponent<CharacterController>();
		if (cc != null && cc.enabled)
		{
			cc.enabled = false;
			state.colliders.Add(cc);
		}

		// Візуалка гравця + його зброї
		var renderers = occ.GetComponentsInChildren<Renderer>(true);
		foreach (var r in renderers)
		{
			if (r == null) continue;
			if (!r.enabled) continue; // вже вимкнені не чіпаємо
			r.enabled = false;
			state.renderers.Add(r);
		}
	}

	private void RestoreOccupantAfterVehicle(GameObject occ)
	{
		if (occ == null) return;

		if (!_disabledStates.TryGetValue(occ, out var state))
			return;

		// Повертаємо всі Behaviour
		foreach (var b in state.behaviours)
			if (b != null) b.enabled = true;
		state.behaviours.Clear();

		// Повертаємо CharacterController-и
		foreach (var cc in state.colliders)
			if (cc != null) cc.enabled = true;
		state.colliders.Clear();

		// Повертаємо рендери (модель гравця, зброя тощо)
		foreach (var r in state.renderers)
			if (r != null) r.enabled = true;
		state.renderers.Clear();

		_disabledStates.Remove(occ);
	}

	#endregion

	#region Камера

	private void TrySwitchCameraToSeatForLocal(IInputService input, VehicleSeat seat)
	{
		if (input == null || !input.IsLocal) return;
		if (seat == null) return;

		var sc = seat.GetSeatCameraComponent();
		if (sc == null) return;

		Transform follow = seat.GetCameraFollowTarget();
		Transform lookAt = seat.GetCameraLookAtTarget();

		sc.ApplyTargets(follow, lookAt);
		CameraManager.Instance?.SwitchToSeat(sc, follow, lookAt);
	}

	#endregion
}
