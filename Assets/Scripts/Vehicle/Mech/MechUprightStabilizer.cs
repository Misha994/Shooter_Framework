using UnityEngine;

/// <summary>
/// Плавно вирівнює мех так, щоб він стояв вертикально (up = Vector3.up),
/// але зберігає поточний yaw (обертання по Y).
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class MechUprightStabilizer : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Rigidbody rb;
	[SerializeField] private Transform body;
	// Можна залишити null – тоді використаємо transform

	[Header("Upright Settings")]
	[Tooltip("Наскільки швидко мех повертається у вертикальне положення.")]
	[SerializeField] private float uprightSpeed = 5f;

	[Tooltip("Максимальний допустимий кут завалу (для інформації, поки що не клампимо).")]
	[SerializeField] private float maxTiltAngle = 45f;

	private void Awake()
	{
		if (rb == null)
			rb = GetComponent<Rigidbody>();

		if (body == null)
			body = transform;
	}

	private void FixedUpdate()
	{
		if (rb == null || body == null)
			return;

		// Поточний forward, спроєктований на горизонтальну площину
		Vector3 forward = body.forward;
		forward = Vector3.ProjectOnPlane(forward, Vector3.up);

		if (forward.sqrMagnitude < 0.0001f)
		{
			// На всяк випадок – якщо forward раптом майже вертикальний
			forward = Vector3.forward;
		}

		forward.Normalize();

		// Обертання, яке хочемо: той самий yaw, але up = Vector3.up (без завалу)
		Quaternion targetRotation = Quaternion.LookRotation(forward, Vector3.up);

		// Плавно рухаємо Rigidbody до targetRotation
		Quaternion current = rb.rotation;
		Quaternion newRotation = Quaternion.Slerp(current, targetRotation, uprightSpeed * Time.fixedDeltaTime);

		rb.MoveRotation(newRotation);
	}
}
