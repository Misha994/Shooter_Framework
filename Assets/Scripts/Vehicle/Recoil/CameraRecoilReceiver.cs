using UnityEngine;

/// <summary>
/// Простий camera kick для місць техніки: разовий відхил по -X (pitch up),
/// із плавним поверненням. Може опційно враховувати швидкість rootBody.
/// </summary>
[DisallowMultipleComponent]
public class CameraRecoilReceiver : MonoBehaviour
{
	[Header("Refs")]
	[SerializeField] private Rigidbody rootBody;     // опційно (для майбутніх фільтрів)
	[SerializeField] private Transform cameraPivot;  // куди крутити локальний pitch

	[Header("Tuning")]
	[Tooltip("Швидкість повернення камери до нуля (deg/sec).")]
	[Min(0.01f)] public float returnSpeed = 10f;

	[Tooltip("Макс. кут kick, якщо накинути кілька разів підряд (deg). 0 = без ліміту.")]
	[Min(0f)] public float maxStackDegrees = 10f;

	private float _curKick; // у градусах (позитивне значення = дивимось трішки вниз, ніби ствол підкинуло вгору)

	private void Reset()
	{
		if (!cameraPivot) cameraPivot = transform;
		if (!rootBody) rootBody = GetComponentInParent<Rigidbody>();
	}

	/// <summary> Додати разовий kick у градусах (позитивне = віддати ствол вгору, камера вниз). </summary>
	public void Kick(float degrees)
	{
		_curKick += Mathf.Max(0f, degrees);
		if (maxStackDegrees > 0f)
			_curKick = Mathf.Min(_curKick, maxStackDegrees);
	}

	private void LateUpdate()
	{
		// Плавний релакс до нуля
		float back = returnSpeed * Time.deltaTime;
		_curKick = Mathf.MoveTowards(_curKick, 0f, back);

		if (cameraPivot != null)
		{
			// Повертаємо локальний X (pitch). Негативний кут — "задрати ствол": камеру трохи вниз.
			var e = cameraPivot.localEulerAngles;
			// Переведемо у signed (-180..180)
			float pitch = e.x;
			if (pitch > 180f) pitch -= 360f;

			pitch = -_curKick; // простий add: беремо тільки наш offset
			cameraPivot.localEulerAngles = new Vector3(pitch, e.y, e.z);
		}
	}
}
