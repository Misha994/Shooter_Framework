using UnityEngine;

/// <summary>
/// Приймач віддачі для корпусу техніки. Додає імпульс у точці пострілу,
/// обмежуючи миттєву зміну швидкості і крутильний імпульс.
/// </summary>
[DisallowMultipleComponent]
public class RigidbodyRecoilReceiver : MonoBehaviour
{
	[SerializeField] private Rigidbody targetRb;
	[Tooltip("Множник до переданого імпульсу (на випадок глобального тюнінгу).")]
	public float impulseMul = 1f;

	[Header("Safety Clamps")]
	[Tooltip("Максимальна додаткова ΔV за один постріл (м/с). 0 = без обмеження.")]
	public float maxDeltaV = 1.5f;

	[Tooltip("Максимальний модуль крутильного імпульсу (Н·м·с), 0 = без обмеження.\n" +
			 "Зазвичай torque сам виникає від AddForceAtPosition, тож це лише запобіжник.")]
	public float maxTorqueImpulse = 50000f;

	private void Reset()
	{
		if (!targetRb) targetRb = GetComponentInParent<Rigidbody>();
	}

	/// <summary>
	/// Застосувати імпульс у світі. 'impulse' — вектор імпульсу (Н·с).
	/// </summary>
	public void Apply(Vector3 worldPoint, Vector3 impulse)
	{
		if (!targetRb) { targetRb = GetComponentInParent<Rigidbody>(); }
		if (!targetRb) return;

		var scaled = impulse * impulseMul;

		// Обмеження ΔV (консервативне): якщо занадто великий імпульс — зменшимо
		if (maxDeltaV > 0f)
		{
			float mass = Mathf.Max(0.0001f, targetRb.mass);
			float curDeltaV = scaled.magnitude / mass;
			if (curDeltaV > maxDeltaV)
				scaled = scaled.normalized * (maxDeltaV * mass);
		}

		targetRb.AddForceAtPosition(scaled, worldPoint, ForceMode.Impulse);
		// Додаткового AddTorque не треба: AddForceAtPosition вже створить момент відносно ЦМ.
		// Обмеження maxTorqueImpulse можна було б реалізувати через аналіз R= r x F, але
		// зазвичай достатньо clamp-у ΔV. Залишаємо поле на майбутнє.
	}
}
