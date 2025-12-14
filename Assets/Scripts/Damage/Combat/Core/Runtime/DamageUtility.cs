// Assets/Combat/Runtime/DamageUtility.cs
using UnityEngine;
using Combat.Core;

public static class DamageUtility
{
	/// <summary>Простий AoE з лінією видимості й лінійним спадом.</summary>
	public static int ApplyRadialDamage(Vector3 origin, float radius, float baseDamage,
										float penetration, DamageType type, WeaponTag tag,
										LayerMask mask, Transform attacker = null,
										AnimationCurve falloff = null, float losHead = 1.2f)
	{
		int hits = 0;
		var cols = Physics.OverlapSphere(origin, radius, mask, QueryTriggerInteraction.Collide);
		for (int i = 0; i < cols.Length; i++)
		{
			var col = cols[i];
			var recv = col.GetComponentInParent<IDamageReceiver>();
			if (recv == null) continue;

			Vector3 p = col.ClosestPoint(origin);
			float d01 = Mathf.Clamp01(Vector3.Distance(origin, p) / Mathf.Max(0.001f, radius));
			float mul = falloff != null ? Mathf.Max(0f, falloff.Evaluate(d01)) : (1f - d01);

			if (mul <= 0f) continue;

			// Лінія видимості (спрощено)
			Vector3 a = origin + Vector3.up * losHead;
			Vector3 b = p + Vector3.up * 0.2f;
			if (Physics.Linecast(a, b, out var hit, Physics.AllLayers, QueryTriggerInteraction.Ignore))
			{
				// якщо вдарились у щось НЕ з ієрархії жертви — можна зменшити вдвічі або відкинути
				if (hit.collider.GetComponentInParent<IDamageReceiver>() != recv)
					mul *= 0.5f;
			}

			float dmg = baseDamage * mul;
			if (dmg <= 0f) continue;

			var payload = new DamagePayload(dmg, penetration, type, tag, p, (p - origin).normalized,
											BodyRegion.Torso, 0, 0, attacker);
			recv.TakeDamage(in payload);
			hits++;
		}
		return hits;
	}
}
