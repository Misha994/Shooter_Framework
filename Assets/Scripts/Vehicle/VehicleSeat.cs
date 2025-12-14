using System.Collections.Generic;
using Cinemachine;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class VehicleSeat : MonoBehaviour
{
	[Header("Mount / exit")]
	[Tooltip("Точка куди приаттачувати гравця у техніці")]
	public Transform mount;

	[Tooltip("Локальна точка виходу (світова позиція береться з неї)")]
	public Transform localExitPoint;

	[Header("Camera")]
	[Tooltip("Корінь камери сидіння (де знаходиться CinemachineVirtualCamera / SeatCamera)")]
	public GameObject seatCameraGO;

	[Header("Cinemachine")]
	[Tooltip("Пріоритет який буде встановлено для vcam коли сидіння зайняте")]
	public int occupiedPriority = 50;

	[Header("Meta")]
	public VehicleSeatRole role = VehicleSeatRole.Passenger;

	[Tooltip("Hotkey 1..10 (0 або <0 = не використовується)")]
	public int hotkey = -1;

	// runtime
	private GameObject _occupant;
	private CinemachineVirtualCamera _seatVcam;
	private int _originalVcamPriority;
	private bool _priorityOverridden = false;

	// кешований список споживачів інпуту
	private readonly List<ISeatInputConsumer> _cachedConsumers = new List<ISeatInputConsumer>();

	// ПУБЛІЧНІ ПОДІЇ
	[Tooltip("Викликається коли сидіння зайняте (передається occupant GameObject)")]
	public UnityEvent<GameObject> OnOccupied = new UnityEvent<GameObject>();

	[Tooltip("Викликається коли сидіння звільнено (передається попередній occupant GameObject або null)")]
	public UnityEvent<GameObject> OnVacated = new UnityEvent<GameObject>();

	public bool IsOccupied => _occupant != null;
	public GameObject Occupant => _occupant;

	private void Awake()
	{
		// Знайти Cinemachine vcam
		if (seatCameraGO != null)
			_seatVcam = seatCameraGO.GetComponentInChildren<CinemachineVirtualCamera>(true);

		if (_seatVcam == null)
			_seatVcam = GetComponentInChildren<CinemachineVirtualCamera>(true);

		if (_seatVcam != null)
			_originalVcamPriority = _seatVcam.Priority;
	}

	/// <summary>
	/// Задати / очистити occupant.
	/// ВАЖЛИВО: трансформ гравця та вкл/викл компонентів робить VehicleSeatManager.
	/// </summary>
	public void SetOccupied(GameObject occ, bool occupied)
	{
		var prev = _occupant;
		_occupant = occupied ? occ : null;

		// --- Камера сидіння: пріоритет vcam ---
		HandleSeatVcamPriority(occupied);

		// --- SeatCamera (якщо є) ---
		HandleSeatCameraComponentPriority(occupied);

		// --- Інпут у контролери техніки ---
		IInputService playerInput = null;
		if (occupied && occ != null)
			playerInput = occ.GetComponentInChildren<IInputService>();

		RouteInputToControllers(playerInput);

		// --- Переключення камери ---
		if (occupied && occ != null)
		{
			TrySwitchCameraToSeatForLocal(occ, this);
		}
		else if (prev != null)
		{
			TryRestoreCameraAfterExit(prev);
		}

		// --- Події ---
		if (occupied)
		{
			try { OnOccupied?.Invoke(occ); }
			catch (System.Exception ex) { Debug.LogException(ex); }
		}
		else
		{
			try { OnVacated?.Invoke(prev); }
			catch (System.Exception ex) { Debug.LogException(ex); }
		}
	}

	#region Камера сидіння

	private void HandleSeatVcamPriority(bool occupied)
	{
		if (_seatVcam == null) return;

		if (occupied)
		{
			if (!_priorityOverridden)
			{
				_originalVcamPriority = _seatVcam.Priority;
				_priorityOverridden = true;
			}
			_seatVcam.Priority = occupiedPriority;
		}
		else
		{
			_seatVcam.Priority = _originalVcamPriority;
			_priorityOverridden = false;
		}
	}

	private void HandleSeatCameraComponentPriority(bool occupied)
	{
		var seatCam = GetSeatCameraComponent();
		if (seatCam == null) return;

		if (occupied)
			seatCam.SetPriority(occupiedPriority);
		else
			seatCam.RestorePriorities();
	}

	#endregion

	#region Інпут у контролери

	private void RouteInputToControllers(IInputService playerInput)
	{
		var drive = GetComponentInParent<IDriveController>();
		if (drive != null)
			drive.SetSeatInput(playerInput);

		var turret = GetComponentInParent<VehicleTurretController>();
		if (turret != null)
			turret.SetSeatInput(playerInput);

		var gunner = GetComponent<VehicleGunnerController>();
		if (gunner != null)
			gunner.SetSeatInput(playerInput);

		// Інші споживачі
		var consumers = GetInputConsumers();
		foreach (var consumer in consumers)
			consumer.SetSeatInput(playerInput);
	}

	public List<ISeatInputConsumer> GetInputConsumers()
	{
		_cachedConsumers.Clear();
		var comps = GetComponentsInChildren<MonoBehaviour>(true);
		foreach (var c in comps)
		{
			if (c is ISeatInputConsumer consumer)
				_cachedConsumers.Add(consumer);
		}
		return _cachedConsumers;
	}

	#endregion

	#region Камера гравця / сидіння

	private void TrySwitchCameraToSeatForLocal(GameObject occ, VehicleSeat seat)
	{
		if (occ == null) return;

		var input = occ.GetComponent<IInputService>();
		if (input == null || !input.IsLocal) return;

		var sc = seat.GetSeatCameraComponent();
		if (sc == null) return;

		Transform follow = seat.GetCameraFollowTarget();
		Transform lookAt = seat.GetCameraLookAtTarget();

		// Переключаємо таргети камери сидіння
		sc.ApplyTargets(follow, lookAt);

		if (CameraManager.Instance != null)
		{
			CameraManager.Instance.SwitchToSeat(sc, follow, lookAt);
		}
	}

	private void TryRestoreCameraAfterExit(GameObject occ)
	{
		if (occ == null) return;

		var occInput = occ.GetComponent<IInputService>();
		if (occInput != null && occInput.IsLocal)
		{
			if (CameraManager.Instance != null)
				CameraManager.Instance.RestoreToDefault();
		}
	}

	public Transform GetCameraFollowTarget()
	{
		if (mount != null) return mount;
		return transform;
	}

	public Transform GetCameraLookAtTarget()
	{
		return GetCameraFollowTarget();
	}

	public SeatCamera GetSeatCameraComponent()
	{
		if (seatCameraGO != null)
		{
			var sc = seatCameraGO.GetComponentInChildren<SeatCamera>(true);
			if (sc != null) return sc;
		}
		return GetComponentInChildren<SeatCamera>(true);
	}

	#endregion
}
