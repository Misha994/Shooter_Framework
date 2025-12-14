using UnityEngine;

/// <summary>
/// Мотор ходячого меха, сумісний із системою техніки:
/// - рухає Rigidbody меха в площині,
/// - підтримує legacy Input і IInputService з сидіння,
/// - реалізує IDriveController + ISeatInputConsumer для інтеграції з VehicleSeat/VehicleSeatManager,
/// - опціонально рухається у системі координат башти (як людина відносно камери),
///   та підкручує нижню частину корпусу (bodyVisual/таз) під КУРС РУХУ (з фолбеком по башті).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class MechWalkerMotor : MonoBehaviour, IDriveController, ISeatInputConsumer
{
	[Header("References")]
	public Rigidbody rb;

	/// <summary>
	/// Фрейм, відносно якого вважаємо up/базовий forward.
	/// Якщо порожньо — беремо transform цього об'єкта.
	/// </summary>
	public Transform driveFrame;

	[Header("Speed / Accel")]
	public float maxSpeed = 6f;
	public float accel = 20f;
	public float brakeAccel = 30f;

	/// <summary>
	/// Використовується тільки коли useTurretRelativeMovement == false.
	/// </summary>
	public float turnSpeedDeg = 90f;

	[Header("Input")]
	[Tooltip("Дозволити legacy осі, коли немає сидячого гравця/зовнішнього інпуту.")]
	public bool allowLegacyAxes = true;
	public string verticalAxis = "Vertical";
	public string horizontalAxis = "Horizontal";

	[Header("Seat / vehicle integration")]
	[Tooltip("Якщо true — мех реагує тільки тоді, коли хтось сидить (є IInputService або зовнішня команда).")]
	public bool requireOccupantForInput = true;

	[Header("Turret-relative movement")]
	[Tooltip("Якщо true — W/S/A/D інтерпретуються у координатах башти (turretYaw), а корпус/ноги йдуть відносно неї.")]
	public bool useTurretRelativeMovement = false;

	[Tooltip("Турель меха (опційно, але бажано, якщо використовуєш turret-relative рух).")]
	public VehicleTurretController turret;

	[Tooltip("Yaw-трансформ башти. Якщо не задано, при Awake візьметься turret.turretYaw.")]
	public Transform turretYaw;

	[Header("Body (таз / нижня частина)")]
	[Tooltip("Візуал нижньої частини меха, який треба докручувати (рекомендується окремий 'BodyFrame' над BodyRoot).")]
	public Transform bodyVisual;

	[Tooltip("Швидкість вирівнювання нижньої частини (deg/sec).")]
	public float bodyAlignSpeedDeg = 180f;

	[Tooltip("Мінімальна горизонтальна швидкість, при якій починаємо докручувати bodyVisual по курсу руху.")]
	public float alignMinPlanarSpeed = 0.5f;

	// Внутрішній стан
	private IInputService _input;
	private bool _disabled;
	private float _throttleCmd;
	private float _steerCmd;
	private bool _haveExternalCmd;

	public bool IsActive => enabled && !_disabled;

	private void Reset()
	{
		rb = GetComponent<Rigidbody>();
	}

	private void Awake()
	{
		if (rb == null)
			rb = GetComponent<Rigidbody>();

		if (driveFrame == null)
			driveFrame = transform;

		if (turret != null && turretYaw == null)
			turretYaw = turret.turretYaw;

		rb.interpolation = RigidbodyInterpolation.Interpolate;
		// Хочемо тільки yaw, без завалів:
		rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
	}

	// ================== IDriveController / ISeatInputConsumer ==================

	/// <summary>
	/// Викликається з VehicleSeat.SetOccupied(...): сюди прилітає IInputService гравця.
	/// </summary>
	public void SetSeatInput(IInputService input)
	{
		_input = input;
		if (_input == null && requireOccupantForInput)
		{
			// Нема пасажира — обнуляємо команди
			_throttleCmd = 0f;
			_steerCmd = 0f;
		}
	}

	// Alias для сумісності з іншими кодами, якщо десь використовують SetInput
	public void SetInput(IInputService input) => SetSeatInput(input);

	/// <summary>
	/// Вимкнути/увімкнути драйв. При вимкненні повністю гасимо рух.
	/// </summary>
	public void SetDisabled(bool disabled)
	{
		_disabled = disabled;
		if (disabled)
			KillDrive();
	}

	/// <summary>
	/// Повністю глушить рух меха (викликається при виході з машини / вимкненні).
	/// </summary>
	public void KillDrive()
	{
		_throttleCmd = 0f;
		_steerCmd = 0f;
		_haveExternalCmd = false;

		if (rb != null)
		{
			// Гасимо тільки горизонтальну швидкість, залишаємо вертикаль (наприклад, якщо стрибає).
			Transform frame = driveFrame != null ? driveFrame : transform;
			Vector3 up = frame.up;

			Vector3 v = rb.velocity;
			Vector3 vert = Vector3.Project(v, up);
			rb.velocity = vert;
		}
	}

	/// <summary>
	/// Викликати ззовні (AI/мережа): команда напряму, минаючи IInputService.
	/// Діє один кадр FixedUpdate.
	/// </summary>
	public void SetCmd(float throttle, float steer)
	{
		_throttleCmd = Mathf.Clamp(throttle, -1f, 1f);
		_steerCmd = Mathf.Clamp(steer, -1f, 1f);
		_haveExternalCmd = true;
	}

	// ================== Update / FixedUpdate ==================

	private void Update()
	{
		// Увесь реальний інпут з сидіння/legacy читаємо в FixedUpdate.
	}

	private void FixedUpdate()
	{
		if (_disabled || rb == null) return;

		// 1) Вирішуємо, звідки беремо інпут на цей кадр
		if (_haveExternalCmd)
		{
			// Зовнішня команда вже виставила _throttleCmd/_steerCmd
		}
		else if (_input != null)
		{
			// Інпут з сидіння (IInputService від гравця)
			Vector2 mv = _input.GetMoveAxis(); // X = горизонталь (стрейф/поворот), Y = вперед/назад
			if (mv.sqrMagnitude > 1f)
				mv = mv.normalized;

			_throttleCmd = Mathf.Clamp(mv.y, -1f, 1f);
			_steerCmd = Mathf.Clamp(mv.x, -1f, 1f);
		}
		else if (!requireOccupantForInput && allowLegacyAxes)
		{
			// Фолбек: працюємо від старих осей, якщо це дозволено
			ReadInputLegacy();
		}
		else
		{
			// Ніхто не сидить / інпуту нема — стоїмо
			_throttleCmd = 0f;
			_steerCmd = 0f;
		}

		ApplyMovement();
		_haveExternalCmd = false; // зовнішня команда діє лише один FixedUpdate
	}

	// ================== Внутрішні методи ==================

	private void ReadInputLegacy()
	{
		if (!allowLegacyAxes) return;

		float v = Input.GetAxisRaw(verticalAxis);
		float h = Input.GetAxisRaw(horizontalAxis);

		Vector2 mv = new Vector2(h, v);
		if (mv.sqrMagnitude > 1f)
			mv.Normalize();

		_throttleCmd = mv.y;
		_steerCmd = mv.x;
	}

	private void ApplyMovement()
	{
		Transform frame = driveFrame != null ? driveFrame : transform;
		Vector3 up = frame.up;

		// Поточна швидкість
		Vector3 vel = rb.velocity;
		Vector3 planarVel = Vector3.ProjectOnPlane(vel, up);

		// === 1. Обчислюємо бажаний planar-вектор швидкості ===
		Vector3 basisFwd;
		Vector3 basisRight;

		if (useTurretRelativeMovement && turretYaw != null)
		{
			// Рухаємось у координатах башти (як людина відносно камери)
			Vector3 yawFwd = Vector3.ProjectOnPlane(turretYaw.forward, up);
			if (yawFwd.sqrMagnitude < 0.0001f)
				yawFwd = Vector3.ProjectOnPlane(frame.forward, up);

			yawFwd.Normalize();
			Vector3 yawRight = Vector3.Cross(up, yawFwd); // праворуч відносно башти

			basisFwd = yawFwd;
			basisRight = yawRight;
		}
		else
		{
			// Старий режим — рух відносно driveFrame.forward, steer = поворот корпусу
			Vector3 fwd = Vector3.ProjectOnPlane(frame.forward, up);
			if (fwd.sqrMagnitude < 0.0001f)
				fwd = Vector3.forward;

			fwd.Normalize();
			Vector3 right = Vector3.Cross(up, fwd);

			basisFwd = fwd;
			basisRight = right;
		}

		// Цільова planar-швидкість:
		// - throttle = вперед/назад
		// - steer   = у turret-режимі — стрейф, у звичному режимі тут буде переважно 0 (бо поворот окремо)
		Vector3 targetVel = basisFwd * (_throttleCmd * maxSpeed)
						  + basisRight * (_steerCmd * maxSpeed);

		// Різниця між бажаною і поточною
		Vector3 deltaV = targetVel - planarVel;

		// Вибираємо, чи це розгін, чи гальмування
		float sameDirDot = Vector3.Dot(targetVel, planarVel);
		float curAccel = (sameDirDot >= 0f ? accel : brakeAccel);

		float maxDeltaV = curAccel * Time.fixedDeltaTime;
		if (deltaV.magnitude > maxDeltaV)
			deltaV = deltaV.normalized * maxDeltaV;

		if (Time.fixedDeltaTime > 0f)
		{
			Vector3 accelVec = deltaV / Time.fixedDeltaTime;
			rb.AddForce(accelVec, ForceMode.Acceleration);
		}

		// === 2. Поворот корпуса (Rigidbody) у НЕ-turret-режимі ===
		if (!useTurretRelativeMovement)
		{
			if (Mathf.Abs(_steerCmd) > 0.001f)
			{
				float yawDelta = _steerCmd * turnSpeedDeg * Time.fixedDeltaTime;
				Quaternion dq = Quaternion.AngleAxis(yawDelta, up);
				rb.MoveRotation(dq * rb.rotation);
			}
		}

		// === 3. Вирівнювання нижньої частини (таза / BodyFrame) по КУРСУ РУХУ ===
		AlignBodyVisual(planarVel, up);
	}

	/// <summary>
	/// Плавно повертає bodyVisual (таз) по курсу ходьби:
	/// - якщо реально є швидкість — по вектору швидкості,
	/// - якщо швидкість мала, але є інпут і башта — фолбек по башті,
	/// - якщо стоїмо без інпуту — нічого не робимо.
	/// </summary>
	private void AlignBodyVisual(Vector3 planarVel, Vector3 up)
	{
		if (bodyVisual == null)
			return;

		Vector3 moveDir = Vector3.zero;
		float planarSpeed = planarVel.magnitude;

		// 1) Основний випадок — реальний курс руху
		if (planarSpeed >= alignMinPlanarSpeed)
		{
			moveDir = planarVel.normalized;
		}
		// 2) Коли ще не розігнався, але є інпут — фолбек по башті (для красивого старту)
		else if (useTurretRelativeMovement && turretYaw != null)
		{
			bool hasInput =
				Mathf.Abs(_throttleCmd) > 0.1f ||
				Mathf.Abs(_steerCmd) > 0.1f ||
				_haveExternalCmd;

			if (hasInput)
			{
				moveDir = Vector3.ProjectOnPlane(turretYaw.forward, up);
				if (moveDir.sqrMagnitude > 0.0001f)
					moveDir.Normalize();
			}
		}

		if (moveDir.sqrMagnitude < 0.0001f)
			return; // немає куди вирівнювати

		Quaternion targetRot = Quaternion.LookRotation(moveDir, up);

		bodyVisual.rotation = Quaternion.RotateTowards(
			bodyVisual.rotation,
			targetRot,
			bodyAlignSpeedDeg * Time.fixedDeltaTime);
	}
}
