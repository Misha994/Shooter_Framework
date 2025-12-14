using UnityEngine;

public class IKFootSolver : MonoBehaviour
{
	[Header("Setup")]
	[SerializeField] private LayerMask terrainLayer;
	[SerializeField] private Transform body;
	[SerializeField] private IKFootSolver otherFoot;

	[Header("Step Settings")]
	[SerializeField] private float speed = 4f;
	[SerializeField] private float stepDistance = 2.5f;
	[SerializeField] private float stepLength = 2.5f;
	[SerializeField] private float stepHeight = 1f;
	[SerializeField] private Vector3 footOffset = new Vector3(0f, 0.2f, 0f);
	[SerializeField] private bool drawDebug = false;

	[Header("Step Conditions")]
	[SerializeField] private float minBodySpeedForStep = 0.1f;       // мінімальна лінійна швидкість для кроку
	[SerializeField] private float minStepInterval = 0.15f;          // мінімальний інтервал між кроками
	[SerializeField] private float minAngularSpeedForStep = 60f;     // мінімальна кутова швидкість (deg/s) для кроку при повороті на місці

	// Рантайм-поля
	private Vector3 oldPosition;
	private Vector3 currentPosition;
	private Vector3 newPosition;

	private Vector3 oldNormal = Vector3.up;
	private Vector3 currentNormal = Vector3.up;
	private Vector3 newNormal = Vector3.up;

	private float lerp = 1f;
	private bool _isStepping = false;
	private float _timeSinceLastStep = 0f;

	private Vector3 _prevBodyPos;
	private Quaternion _prevBodyRot;

	// Локальний зсув ноги по X відносно тіла (аналог footSpacing)
	private float _footSpacingLocalX;

	// Публічні гетери для тіла / контролера
	public Vector3 CurrentPosition => currentPosition;
	public Vector3 CurrentNormal => currentNormal;
	public bool IsStepping => _isStepping;

	private void Start()
	{
		if (body == null && transform.parent != null)
			body = transform.parent;

		currentPosition = newPosition = oldPosition = transform.position;
		currentNormal = newNormal = oldNormal = transform.up;

		if (body != null)
		{
			_prevBodyPos = body.position;
			_prevBodyRot = body.rotation;

			// Запамʼятовуємо, наскільки нога відставлена вбік від тіла
			Vector3 local = body.InverseTransformPoint(transform.position);
			_footSpacingLocalX = local.x;
		}
		else
		{
			_prevBodyPos = transform.position;
			_prevBodyRot = transform.rotation;
			_footSpacingLocalX = transform.localPosition.x;
		}

		lerp = 1f;
		_isStepping = false;
	}

	private void Update()
	{
		_timeSinceLastStep += Time.deltaTime;

		if (body == null)
			return;

		// -------- ШВИДКІСТЬ ТІЛА (лінійна) --------
		Vector3 bodyDelta = body.position - _prevBodyPos;
		float bodySpeed = bodyDelta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
		_prevBodyPos = body.position;

		// -------- КУТОВА ШВИДКІСТЬ (Y-поворот) --------
		float angularSpeed = 0f;
		{
			Quaternion deltaRot = body.rotation * Quaternion.Inverse(_prevBodyRot);
			deltaRot.ToAngleAxis(out float angleDegrees, out Vector3 axis);

			angleDegrees = Mathf.Abs(angleDegrees);
			if (angleDegrees > 180f) angleDegrees = 360f - angleDegrees;

			angularSpeed = angleDegrees / Mathf.Max(Time.deltaTime, 0.0001f);
			_prevBodyRot = body.rotation;
		}

		// Чи вважаємо це «поворотом на місці» (мало руху, але велика кутова швидкість)
		bool isTurningInPlace =
			bodySpeed < minBodySpeedForStep &&
			angularSpeed > minAngularSpeedForStep;

		// -------- Анімація кроку (дуга) --------
		float t = Mathf.Clamp01(lerp);
		Vector3 horizontal = Vector3.Lerp(oldPosition, newPosition, t);
		float heightArc = Mathf.Sin(t * Mathf.PI) * stepHeight;

		currentPosition = horizontal + Vector3.up * heightArc;
		currentNormal = Vector3.Slerp(oldNormal, newNormal, t);

		if (lerp < 1f)
		{
			lerp += Time.deltaTime * speed;
			if (lerp >= 1f)
			{
				lerp = 1f;
				_isStepping = false;
				oldPosition = newPosition;
				oldNormal = newNormal;
			}
		}

		// -------- Умова для старту нового кроку --------
		bool canStepThisFoot =
			!_isStepping &&
			_timeSinceLastStep > minStepInterval &&
			(bodySpeed > minBodySpeedForStep || isTurningInPlace);

		bool otherIsMoving = otherFoot != null && otherFoot._isStepping;
		bool otherNotReadyYet = otherFoot != null && otherFoot.lerp < 0.5f;

		if (canStepThisFoot && !otherIsMoving && !otherNotReadyYet)
		{
			TryStartStep(isTurningInPlace);
		}

		if (drawDebug)
		{
			Debug.DrawLine(transform.position + Vector3.up * 0.1f, currentPosition, Color.yellow);
			Debug.DrawRay(currentPosition, currentNormal, Color.cyan);
		}

		// Оновлюємо реальний трансформ ступні
		transform.position = currentPosition;
		transform.up = currentNormal;
	}

	private void TryStartStep(bool isTurningInPlace)
	{
		if (body == null)
			return;

		// Аналог старого:
		// Ray ray = new Ray(body.position + (body.right * footSpacing), Vector3.down);
		Vector3 sideOffset = body.right * _footSpacingLocalX;
		Vector3 rayOrigin = body.position + sideOffset + Vector3.up * 3f;
		Vector3 rayDir = Vector3.down;
		float rayDist = 10f;

		if (!Physics.Raycast(rayOrigin, rayDir, out RaycastHit hit, rayDist, terrainLayer, QueryTriggerInteraction.Ignore))
			return;

		// Наскільки поточна ціль ноги відрізняється від бажаного місця
		float distToTarget = Vector3.Distance(newPosition, hit.point);
		if (distToTarget <= stepDistance)
			return;

		// -------- Напрямок кроку --------
		int direction = 0;

		if (!isTurningInPlace)
		{
			// При нормальній ходьбі – логіка як у старому скрипті
			float hitZ = body.InverseTransformPoint(hit.point).z;
			float currentZ = body.InverseTransformPoint(newPosition).z;
			direction = hitZ > currentZ ? 1 : -1;
		}
		else
		{
			// При повороті на місці – крок без «вперед/назад»,
			// просто переставляємо ногу у нове положення відносно тіла.
			direction = 0;
		}

		Vector3 targetPos = hit.point + (body.forward * stepLength * direction) + footOffset;
		Vector3 targetNormal = hit.normal;

		// Підготовка до кроку
		oldPosition = currentPosition;
		oldNormal = currentNormal;

		newPosition = targetPos;
		newNormal = targetNormal;

		lerp = 0f;
		_isStepping = true;
		_timeSinceLastStep = 0f;
	}

	private void OnDrawGizmosSelected()
	{
		if (!drawDebug) return;
		Gizmos.color = Color.red;
		Gizmos.DrawSphere(newPosition, 0.05f);
	}
}
