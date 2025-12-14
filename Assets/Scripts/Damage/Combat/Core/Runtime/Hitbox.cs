// Assets/Combat/Runtime/Hitbox.cs
using UnityEngine;

namespace Combat.Core
{
	[DisallowMultipleComponent]
	public sealed class Hitbox : MonoBehaviour, IDamageReceiver
	{
		[SerializeField] private BodyRegion region = BodyRegion.Torso;
		[SerializeField] private DamageReceiver root; // кореневий приймач на персонажі/техніці

		private void Reset()
		{
			if (!root) root = GetComponentInParent<DamageReceiver>();
		}

		public void TakeDamage(in DamagePayload p)
		{
			if (!root) return;
			var routed = new DamagePayload(
				p.Damage, p.Penetration, p.Type, p.WeaponTag,
				p.HitPoint, p.HitNormal,
				region, p.AttackerId, p.VictimId, p.Attacker
			);
			root.TakeDamage(in routed);
		}
	}
}
