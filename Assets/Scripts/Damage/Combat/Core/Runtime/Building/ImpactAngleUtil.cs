// Assets/Combat/Core/Runtime/ImpactAngleUtil.cs
using UnityEngine;

namespace Combat.Core
{
	public static class ImpactAngleUtil
	{
		public static bool TryGetIncomingDir(in DamagePayload p, Vector3 hitPoint,
											 float probeRadius, float maxAge,
											 out Vector3 incoming)
		{
			incoming = default;

			// 1) —в≥жа под≥€ б≥л€ точки (сенсор ставить кул€/лазер)
			var s = HitDirectionSensor.FindBestAround(hitPoint, probeRadius, maxAge);
			if (s != null && s.TryGetRecent(out var dir, maxAge))
			{
				incoming = dir; return true;
			}

			// 2) ‘олбек Ч напр€мок в≥д атакера
			if (p.Attacker)
			{
				var v = hitPoint - p.Attacker.position;
				if (v.sqrMagnitude > 1e-6f) { incoming = v.normalized; return true; }
			}
			return false;
		}

		/// 0∞ = удар перпендикул€рно до плити (найважче пробити).
		public static float GuessImpactAngleDeg(in DamagePayload p, Vector3 hitPoint, Vector3 hitNormal,
												float defaultAngleDeg = 0f,
												float probeRadius = 0.2f, float maxAge = 0.5f)
		{
			if (TryGetIncomingDir(in p, hitPoint, probeRadius, maxAge, out var inc))
				return Mathf.Clamp(Vector3.Angle(hitNormal, -inc), 0f, 89f);
			return Mathf.Clamp(defaultAngleDeg, 0f, 89f);
		}

		public static Vector3 Backstep(Vector3 hitPoint, Vector3 dir, float back = 0.02f)
			=> hitPoint - dir.normalized * Mathf.Max(0f, back);
	}
}
