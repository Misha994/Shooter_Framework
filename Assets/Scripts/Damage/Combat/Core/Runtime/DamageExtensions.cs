// Assets/Combat/Runtime/DamageExtensions.cs
using UnityEngine;
using Combat.Core;

public static class DamageExtensions
{
	/// <summary>Спроба надіслати урон у колайдер/його батьків.</summary>
	public static bool TryDealDamage(this RaycastHit hit, in DamagePayload payload)
	{
		var r = hit.collider.GetComponentInParent<IDamageReceiver>();
		if (r == null) return false;
		r.TakeDamage(in payload);
		return true;
	}

	/// <summary>Скорочення для лего-сумісних викликів.</summary>
	public static bool TryDealDamage(this RaycastHit hit, float damage, float penetration,
									 DamageType type, WeaponTag tag, Transform attacker = null)
	{
		var p = new DamagePayload(
			damage, penetration, type, tag,
			hit.point, hit.normal, BodyRegion.Torso,
			attackerId: 0, victimId: 0, attacker: attacker
		);
		return hit.TryDealDamage(in p);
	}
}
