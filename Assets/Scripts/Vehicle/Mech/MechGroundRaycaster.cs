using UnityEngine;

public class MechGroundRaycaster : MonoBehaviour
{
	[Header("Посилання")]
	public Rigidbody rb;
	public Transform probe;

	[Header("Ґрунт")]
	public LayerMask groundMask;
	public float bodyHeight = 1f;
	public float rayLength = 4f;

	[Header("Вертикальні обмеження")]
	[Tooltip("Наскільки високо можна 'підстрибнути' корпусом за кадр при сході на уступ")]
	public float maxStepUp = 0.8f;

	[Tooltip("Наскільки низько можна опуститись за кадр при падінні з уступу")]
	public float maxStepDown = 1.5f;

	[Tooltip("Швидкість, з якою позиція тіла наближається до цільової висоти")]
	public float snapSpeed = 2f;

	[Header("Нахил по нормалі ґрунту")]
	public bool alignToGroundNormal = false;
	public float alignSpeed = 10f;
	[Tooltip("Максимальний кут нахилу корпусу від вертикалі (градуси)")]
	public float maxTiltAngle = 15f;
	[Tooltip("Максимальний кут схилу, на якому взагалі дозволяється стояти (градуси)")]
	public float maxSlopeAngle = 40f;

	public bool drawDebug = true;

	[SerializeField, HideInInspector] private bool _hasGround = false;
	[SerializeField, HideInInspector] private RaycastHit _lastHit;

	void Awake()
	{
		if (rb == null)
			rb = GetComponent<Rigidbody>();

		if (probe == null)
			probe = transform;
	}

	void FixedUpdate()
	{
		if (rb == null)
			return;

		Vector3 origin = probe.position + Vector3.up * bodyHeight;
		LayerMask mask = groundMask.value == 0
			? Physics.DefaultRaycastLayers
			: groundMask;

		if (Physics.Raycast(origin, Vector3.down, out var hit, rayLength, mask, QueryTriggerInteraction.Ignore))
		{
			_hasGround = true;
			_lastHit = hit;

			// Перевіряємо крутизну схилу
			float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
			if (slopeAngle > maxSlopeAngle)
			{
				// Вважаємо, що стояти тут не можна
				if (drawDebug)
					Debug.DrawLine(origin, hit.point, Color.red);
				return;
			}

			// Цільова висота тіла
			float targetY = hit.point.y + bodyHeight;
			Vector3 pos = rb.position;

			// Обмеження step up / down
			float maxUp = pos.y + maxStepUp;
			float maxDown = pos.y - maxStepDown;
			targetY = Mathf.Clamp(targetY, maxDown, maxUp);

			float newY = Mathf.Lerp(pos.y, targetY, Time.fixedDeltaTime * snapSpeed);
			rb.MovePosition(new Vector3(pos.x, newY, pos.z));

			if (alignToGroundNormal)
			{
				AlignToGround(hit.normal);
			}

			if (drawDebug)
			{
				Debug.DrawLine(origin, hit.point, Color.green);
				Debug.DrawRay(hit.point, hit.normal, Color.cyan);
			}
		}
		else
		{
			_hasGround = false;
			if (drawDebug)
				Debug.DrawRay(origin, Vector3.down * rayLength, Color.yellow);
		}
	}

	void AlignToGround(Vector3 groundNormal)
	{
		Quaternion currentRot = rb.rotation;

		// Базовий forward у площині XZ
		Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
		if (forward.sqrMagnitude < 0.0001f)
			forward = Vector3.forward;

		// Цільовий rotation по нормалі ґрунту
		Quaternion targetRot = Quaternion.LookRotation(
			Vector3.ProjectOnPlane(forward, groundNormal).normalized,
			groundNormal
		);

		// Обмежуємо tilt від вертикалі
		float angleFromUp = Vector3.Angle(targetRot * Vector3.up, Vector3.up);
		if (angleFromUp > maxTiltAngle)
		{
			float t = maxTiltAngle / angleFromUp;
			targetRot = Quaternion.Slerp(Quaternion.identity, targetRot, t);
			targetRot = Quaternion.RotateTowards(Quaternion.identity, targetRot, maxTiltAngle);
		}

		Quaternion newRot = Quaternion.Slerp(currentRot, targetRot, Time.fixedDeltaTime * alignSpeed);
		rb.MoveRotation(newRot);
	}
}
