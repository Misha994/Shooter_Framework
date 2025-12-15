using UnityEngine;
using Zenject;

/// <summary>
/// Драйвер параметрів Animator для гравця.
/// Єдиний клас, який пише:
///  - IsGrounded / InAir
///  - MoveX / MoveY (простір камери, згладжено)
///  - Speed01 (0..1 для walk→run)
///  - IsAiming / IsSprinting
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimatorParamDriver : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private CharacterController cc;  // контролер гравця
	[SerializeField] private Transform cam;           // PlayerCamera (yaw-орієнтир)

	[Header("Movement")]
	[Tooltip("Максимальна швидкість бігу (має збігатись з sprintSpeed у MovementComponent).")]
	[SerializeField] private float runSpeed = 6f;

	[Tooltip("Наскільки швидко MoveX/MoveY/Speed01 наздоганяють реальну швидкість. Більше = різкіше.")]
	[SerializeField] private float damp = 10f;

	[Header("Debug")]
	[SerializeField] private bool drawDebugVectors = false;

	[Inject(Optional = true)] private IInputService _input;
	[Inject(Optional = true)] private SignalBus _bus;

	private Animator _anim;

	// Стан із сигналу (WeaponController / AimAdapter)
	private bool _isAimingFromSignal;

	// Згладжений рух у просторі камери
	private Vector2 _moveSmoothed;

	// === Animator hashes ======================================================
	private static readonly int HashMoveX = Animator.StringToHash("MoveX");
	private static readonly int HashMoveY = Animator.StringToHash("MoveY");
	private static readonly int HashSpeed01 = Animator.StringToHash("Speed01");
	private static readonly int HashIsAiming = Animator.StringToHash("IsAiming");
	private static readonly int HashIsSprinting = Animator.StringToHash("IsSprinting");
	private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
	private static readonly int HashInAir = Animator.StringToHash("InAir");

	// ==========================================================================

	private void Awake()
	{
		_anim = GetComponent<Animator>();

		if (!cc)
			cc = GetComponentInParent<CharacterController>();

		if (!cam && Camera.main)
			cam = Camera.main.transform;
	}

	private void OnEnable()
	{
		_bus?.Subscribe<AimStateChangedSignal>(OnAimChanged);
	}

	private void OnDisable()
	{
		_bus?.TryUnsubscribe<AimStateChangedSignal>(OnAimChanged);
	}

	private void OnAimChanged(AimStateChangedSignal s)
	{
		_isAimingFromSignal = s.IsAiming;
	}

	private void Update()
	{
		UpdateGrounded();
		UpdateAimAndSprint();
		UpdateLocomotion();
	}

	// === Ground / Air =========================================================
	private void UpdateGrounded()
	{
		bool grounded = cc ? cc.isGrounded : true;
		_anim.SetBool(HashIsGrounded, grounded);
		_anim.SetBool(HashInAir, !grounded);
	}

	// === Aim / Sprint =========================================================
	private void UpdateAimAndSprint()
	{
		bool isAiming;
		bool isSprinting;

		// Якщо немає SignalBus – напряму читаємо інпут.
		if (_bus == null && _input != null)
		{
			isAiming = _input.IsAimHeld();
			isSprinting = _input.IsSprintHeld();
		}
		else
		{
			// Aim – із сигналу, Sprint – з інпуту (якщо є).
			isAiming = _isAimingFromSignal;
			isSprinting = _input != null && _input.IsSprintHeld();
		}

		_anim.SetBool(HashIsAiming, isAiming);
		_anim.SetBool(HashIsSprinting, isSprinting);
	}

	// === Locomotion (MoveX / MoveY / Speed01) =================================
	private void UpdateLocomotion()
	{
		Vector3 velo = cc ? cc.velocity : Vector3.zero;

		// Осі в просторі камери (проєкція на XZ)
		Vector3 fwd = cam ? cam.forward : transform.forward;
		Vector3 right = cam ? cam.right : transform.right;

		fwd.y = 0f;
		right.y = 0f;

		if (fwd.sqrMagnitude < 1e-6f)
			fwd = transform.forward;

		fwd.Normalize();
		right.Normalize();

		// Розклад швидкості по осях камери
		float vx = Vector3.Dot(velo, right);
		float vy = Vector3.Dot(velo, fwd);

		// Нормуємо до runSpeed → [-1..1] для BlendTree
		float denom = Mathf.Max(0.01f, runSpeed);
		Vector2 moveTarget = new Vector2(vx, vy) / denom;

		if (moveTarget.sqrMagnitude > 1f)
			moveTarget = moveTarget.normalized;

		// FPS-незалежне експоненційне згладжування
		float k = Mathf.Max(0f, damp);
		float lerp = (k <= 0f) ? 1f : 1f - Mathf.Exp(-k * Time.deltaTime);

		_moveSmoothed = Vector2.Lerp(_moveSmoothed, moveTarget, lerp);

		_anim.SetFloat(HashMoveX, _moveSmoothed.x);
		_anim.SetFloat(HashMoveY, _moveSmoothed.y);

		// Speed01 – окремо від скалярної горизонтальної швидкості
		float horizontalSpeed = new Vector2(velo.x, velo.z).magnitude;
		float speedTarget = Mathf.InverseLerp(0f, runSpeed, horizontalSpeed);

		float prevSpeed = _anim.GetFloat(HashSpeed01);
		float speedSmoothed = Mathf.Lerp(prevSpeed, speedTarget, lerp);
		_anim.SetFloat(HashSpeed01, speedSmoothed);

		if (drawDebugVectors)
			DrawDebug(velo, fwd, right);
	}

	private void DrawDebug(Vector3 velo, Vector3 fwd, Vector3 right)
	{
		Vector3 origin = transform.position + Vector3.up * 0.1f;
		Debug.DrawRay(origin, velo, Color.red);             // реальна швидкість CC
		Debug.DrawRay(origin, fwd * runSpeed, Color.green); // forward (камера)
		Debug.DrawRay(origin, right * runSpeed, Color.blue);// right (камера)
	}
}
