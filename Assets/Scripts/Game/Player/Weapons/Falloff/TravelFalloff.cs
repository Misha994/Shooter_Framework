// Assets/Scripts/Game/Player/Weapons/Falloff/TravelFalloff.cs
using UnityEngine;

namespace Combat.Core
{
	/// <summary>
	/// ”н≥версальний опис спаданн€ демеджу/пенетрац≥њ за в≥дстанню ≥/або часом польоту.
	///  омпонуютьс€ незалежно: Distance * Time.
	/// </summary>
	[System.Serializable]
	public struct TravelFalloff
	{
		public bool enable;

		[Header("Distance Falloff")]
		public bool useDistance;
		[Min(0f)] public float distanceStart;   // м, з цього м≥сц€ починаЇмо спаданн€
		[Min(0f)] public float distanceEnd;     // м, на ц≥й дистанц≥њ дос€гаЇмо м≥н≥муму
		public AnimationCurve damageMulByDistance;   // вх≥д [0..1] Ч нормал≥зована дистанц≥€
		public AnimationCurve penMulByDistance;      // вх≥д [0..1] Ч нормал≥зована дистанц≥€

		[Header("Time Falloff")]
		public bool useTime;
		[Min(0f)] public float timeStart;       // с
		[Min(0f)] public float timeEnd;         // с
		public AnimationCurve damageMulByTime;       // вх≥д [0..1] Ч нормал≥зований час
		public AnimationCurve penMulByTime;          // вх≥д [0..1] Ч нормал≥зований час

		[Header("Clamps & Hitscan")]
		[Range(0f, 1f)] public float minDamageMul;   // нижн€ межа
		[Range(0f, 1f)] public float minPenMul;      // нижн€ межа
		[Min(0f)] public float hitscanMuzzleSpeed;   // м/с дл€ оц≥нки "часу польоту" у х≥тскан-зброњ

		public static TravelFalloff Disabled => new TravelFalloff
		{
			enable = false,
			useDistance = false,
			useTime = false,
			minDamageMul = 1f,
			minPenMul = 1f,
			hitscanMuzzleSpeed = 0f
		};

		private static bool HasCurve(AnimationCurve c)
			=> c != null && c.keys != null && c.keys.Length > 0;

		public void Evaluate(float distanceMeters, float timeSeconds, out float dmgMul, out float penMul)
		{
			if (!enable)
			{
				dmgMul = 1f; penMul = 1f; return;
			}

			float dMul = 1f, pMul = 1f;

			if (useDistance)
			{
				float d0 = Mathf.Max(0f, distanceStart);
				float d1 = Mathf.Max(d0 + 0.0001f, distanceEnd <= 0f ? d0 : distanceEnd);
				float t = distanceMeters <= d0 ? 0f : (distanceMeters >= d1 ? 1f : Mathf.InverseLerp(d0, d1, distanceMeters));

				// якщо кривоњ немаЇ Ч дефолт: л≥н≥йно в≥д 1 до 0
				float dm = HasCurve(damageMulByDistance) ? damageMulByDistance.Evaluate(t) : (1f - t);
				float pm = HasCurve(penMulByDistance) ? penMulByDistance.Evaluate(t) : (1f - t);

				dMul *= Mathf.Clamp01(dm);
				pMul *= Mathf.Clamp01(pm);
			}

			if (useTime)
			{
				float t0 = Mathf.Max(0f, timeStart);
				float t1 = Mathf.Max(t0 + 0.0001f, timeEnd <= 0f ? t0 : timeEnd);
				float tt = timeSeconds <= t0 ? 0f : (timeSeconds >= t1 ? 1f : Mathf.InverseLerp(t0, t1, timeSeconds));

				float dm = HasCurve(damageMulByTime) ? damageMulByTime.Evaluate(tt) : (1f - tt);
				float pm = HasCurve(penMulByTime) ? penMulByTime.Evaluate(tt) : (1f - tt);

				dMul *= Mathf.Clamp01(dm);
				pMul *= Mathf.Clamp01(pm);
			}

			dMul = Mathf.Clamp(dMul, Mathf.Clamp01(minDamageMul), 1f);
			pMul = Mathf.Clamp(pMul, Mathf.Clamp01(minPenMul), 1f);

			dmgMul = dMul;
			penMul = pMul;
		}
	}
}
