// Assets/Scripts/Damage/PenetrationMaterial.cs
using UnityEngine;

/// <summary>
/// Простий опис матеріалу для пенетрації:
/// - товщина в мм,
/// - скільки пенетрації/швидкості з’їдає,
/// - невеликий випадковий дефлект напрямку.
/// Вішай на бронепластини, стіни, корпуси, health-об'єкти.
/// </summary>
[DisallowMultipleComponent]
public class PenetrationMaterial : MonoBehaviour
{
	[Header("Geometry / Thickness")]
	[Tooltip("Еквівалентна товщина в мм (умовна або RHA).")]
	public float thicknessMM = 50f;

	[Header("Energy loss")]
	[Tooltip("Яку частку пенетрації з’їдає 1 мм (0–1). Напр. 0.02 = 2% на мм.")]
	[Range(0f, 1f)]
	public float penetrationLossPerMM = 0.02f;

	[Tooltip("Множник швидкості втрат (1 = повністю як пенетрація, 0.5 = повільніше).")]
	[Range(0f, 2f)]
	public float speedLossFactor = 1f;

	[Header("Deflection")]
	[Tooltip("Максимальний випадковий відхил по куту при проході, в градусах.")]
	[Range(0f, 45f)]
	public float maxDeflectionDeg = 5f;

	/// <summary>
	/// Застосувати втрати до залишкової пенетрації, швидкості й напрямку.
	/// </summary>
	public void ApplyMaterialLoss(
		ref float remainingPenMM,
		ref float speed,
		ref Vector3 dirN)
	{
		float t = Mathf.Max(0f, thicknessMM);
		if (t <= 0.01f || remainingPenMM <= 0f)
			return;

		// Скільки пенетрації зʼїдає матеріал.
		float penLoss = t * penetrationLossPerMM;
		remainingPenMM = Mathf.Max(0f, remainingPenMM - penLoss);

		// Швидкість — пропорційно (дуже груба модель, але працює).
		float lossFrac = Mathf.Clamp01(penLoss * speedLossFactor);
		speed *= (1f - lossFrac);

		// Випадковий невеликий дефлект.
		if (maxDeflectionDeg > 0.1f && dirN.sqrMagnitude > 0.0001f)
		{
			dirN.Normalize();
			float angle = Random.Range(-maxDeflectionDeg, maxDeflectionDeg);

			// Обираємо хоч якусь вісь, не паралельну напрямку.
			Vector3 axis = Vector3.Cross(dirN, Vector3.up);
			if (axis.sqrMagnitude < 0.0001f)
				axis = Vector3.Cross(dirN, Vector3.right);

			axis.Normalize();
			dirN = Quaternion.AngleAxis(angle, axis) * dirN;
			dirN.Normalize();
		}
	}
}
