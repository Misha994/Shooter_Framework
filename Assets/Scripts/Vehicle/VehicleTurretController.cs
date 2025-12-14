using UnityEngine;

[DisallowMultipleComponent]
public class VehicleTurretController : MonoBehaviour, ISeatInputConsumer
{
	[Header("References")]
	public Transform turretYaw;
	public Transform gunPitch;
	public Transform aimSource;

	[Header("Pivot (optional)")]
	public Transform yawPivot;
	public bool usePivot = false;

	[Header("Axes (local)")]
	public Vector3 yawAxisLocal = Vector3.up;
	public Vector3 pitchAxisLocal = Vector3.right;

	[Header("Speeds & limits")]
	public float yawSpeedDeg = 120f;
	public float pitchSpeedDeg = 75f;
	public float minPitch = -10f;
	public float maxPitch = 25f;
	public float snapEpsilonDeg = 0.25f; // зараз майже не використовується, можна залишити

	[Header("Input tuning")]
	public bool invertY = true;
	public float inputScale = 1.0f;
	public float yawDeadzone = 0.0f;
	public float pitchDeadzone = 0.0f;

	[Header("External/DI input (optional)")]
	public MonoBehaviour diInputProvider;

	[Header("Behavior")]
	public bool allowExternalInput = false;
	public bool requireOccupant = true;

	[Header("Smoothing")]
	public float yawSmoothTime = 0.02f;
	public float pitchSmoothTime = 0.02f;

	// runtime
	private IInputService _diInput;
	private IInputService _input;

	private Quaternion _yawBaseLocal;
	private Quaternion _pitchBaseLocal;

	// те, куди хочемо прийти
	private float _targetYawDeg;
	private float _targetPitchDeg;

	// те, де реально зараз башня (з урахуванням згладжування)
	private float _currentYawDeg;
	private float _currentPitchDeg;

	private float _yawVel;
	private float _pitchVel;

	// буфер зовнішніх дельт (з Gunner)
	private float _extYawDeltaDeg;
	private float _extPitchDeltaDeg;

	private void Awake()
	{
		if (turretYaw == null || gunPitch == null)
		{
			Debug.LogError("[VehicleTurretController] turretYaw або gunPitch не задані. Вимикаю.", this);
			enabled = false;
			return;
		}

		_yawBaseLocal = turretYaw.localRotation;
		_pitchBaseLocal = gunPitch.localRotation;

		_currentYawDeg = NormalizeAngle(turretYaw.localEulerAngles.y);
		_currentPitchDeg = NormalizeAngle(gunPitch.localEulerAngles.x);

		_targetYawDeg = _currentYawDeg;
		_targetPitchDeg = _currentPitchDeg;

		if (diInputProvider != null)
			_diInput = diInputProvider as IInputService;
	}

	// ===== API для сидіння / зовнішніх контролерів =====
	public void SetSeatInput(IInputService input) => _input = input;

	public void ApplyExternalYaw(float deltaDeg) => _extYawDeltaDeg += deltaDeg;
	public void ApplyExternalPitch(float deltaDeg) => _extPitchDeltaDeg += deltaDeg;

	public float GetCurrentYawDeg() => _currentYawDeg;
	public float GetCurrentPitchDeg() => _currentPitchDeg;

	private void Update()
	{
		if (yawAxisLocal.sqrMagnitude < 1e-6f || pitchAxisLocal.sqrMagnitude < 1e-6f)
			return;

		Vector3 yawAxisLocalN = yawAxisLocal.normalized;
		Vector3 pitchAxisLocalN = pitchAxisLocal.normalized;

		// вибираємо джерело інпуту
		var effectiveInput = _input ?? (_diInput != null && allowExternalInput ? _diInput : null);

		// якщо вимагається пасажир — без інпуту і без зовнішніх дельт нічого не робимо
		if (requireOccupant && effectiveInput == null &&
			Mathf.Approximately(_extYawDeltaDeg, 0f) &&
			Mathf.Approximately(_extPitchDeltaDeg, 0f))
			return;

		// 1) інпут від гравця / AI
		if (effectiveInput != null)
		{
			Vector2 look = effectiveInput.GetLookDelta();

			if (Mathf.Abs(look.x) < yawDeadzone) look.x = 0f;
			if (Mathf.Abs(look.y) < pitchDeadzone) look.y = 0f;

			float xMul = inputScale;
			float yMul = (invertY ? -1f : 1f) * inputScale;

			_targetYawDeg += look.x * xMul * yawSpeedDeg * Time.deltaTime;
			_targetPitchDeg += look.y * yMul * pitchSpeedDeg * Time.deltaTime;
		}
		// 2) фолбек по aimSource (коли немає пасажира й можна крутити до цілі)
		else if (aimSource != null && !requireOccupant)
		{
			float ay = ComputeSignedAngleRelativeToBase(turretYaw, _yawBaseLocal, yawAxisLocalN, aimSource.forward);
			float ap = ComputeSignedAngleRelativeToBase(gunPitch, _pitchBaseLocal, pitchAxisLocalN, aimSource.forward);
			_targetYawDeg = ay;
			_targetPitchDeg = Mathf.Clamp(ap, minPitch, maxPitch);
		}

		// 3) додаємо зовнішні дельти (з GunnerController)
		if (!Mathf.Approximately(_extYawDeltaDeg, 0f))
		{
			_targetYawDeg += _extYawDeltaDeg;
			_extYawDeltaDeg = 0f;
		}
		if (!Mathf.Approximately(_extPitchDeltaDeg, 0f))
		{
			_targetPitchDeg += _extPitchDeltaDeg;
			_extPitchDeltaDeg = 0f;
		}

		// обмеження пітча
		_targetPitchDeg = Mathf.Clamp(_targetPitchDeg, minPitch, maxPitch);

		// 4) згладжуємо до таргетів
		_currentYawDeg = Mathf.SmoothDampAngle(_currentYawDeg, _targetYawDeg, ref _yawVel, yawSmoothTime);
		_currentPitchDeg = Mathf.SmoothDampAngle(_currentPitchDeg, _targetPitchDeg, ref _pitchVel, pitchSmoothTime);

		// 5) застосовуємо до трансформів
		Quaternion desiredYawLocal = _yawBaseLocal * Quaternion.AngleAxis(_currentYawDeg, yawAxisLocalN);
		Quaternion desiredPitchLocal = _pitchBaseLocal * Quaternion.AngleAxis(_currentPitchDeg, pitchAxisLocalN);

		// usePivot зараз, по суті, не потрібен – але залишаю гілку на всяк випадок
		if (usePivot && yawPivot != null)
		{
			Quaternion desiredYawWorld = (turretYaw.parent != null ? turretYaw.parent.rotation * desiredYawLocal : desiredYawLocal);
			turretYaw.rotation = desiredYawWorld;
		}
		else
		{
			turretYaw.localRotation = desiredYawLocal;
		}

		gunPitch.localRotation = desiredPitchLocal;
	}

	// ===== helpers =====
	private static float NormalizeAngle(float a)
	{
		a = (a + 180f) % 360f;
		if (a < 0f) a += 360f;
		return a - 180f;
	}

	private static Quaternion GetBaseWorldRotation(Transform t, Quaternion baseLocal)
	{
		return t.parent != null ? t.parent.rotation * baseLocal : baseLocal;
	}

	private static float ComputeSignedAngleRelativeToBase(Transform t, Quaternion baseLocal, Vector3 axisLocalN, Vector3 aimDirWorld)
	{
		Quaternion baseWorld = GetBaseWorldRotation(t, baseLocal);
		Vector3 axisWorld = baseWorld * axisLocalN;
		Vector3 baseForwardWorld = baseWorld * Vector3.forward;

		Vector3 projBase = Vector3.ProjectOnPlane(baseForwardWorld, axisWorld);
		Vector3 projAim = Vector3.ProjectOnPlane(aimDirWorld.normalized, axisWorld);

		if (projBase.sqrMagnitude < 1e-6f || projAim.sqrMagnitude < 1e-6f)
			return 0f;

		projBase.Normalize();
		projAim.Normalize();

		return Vector3.SignedAngle(projBase, projAim, axisWorld);
	}
}
