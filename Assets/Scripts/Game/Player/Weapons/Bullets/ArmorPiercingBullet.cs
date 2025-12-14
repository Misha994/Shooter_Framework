// Assets/Combat/Bullets/ArmorPiercingBullet.cs
using UnityEngine;

namespace Combat.Core
{
	[CreateAssetMenu(menuName = "Bullets/Armor Piercing")]
	public class ArmorPiercingBullet : BulletType
	{
		// інтерпретуємо як міліметри, а не [0..1]
		[Min(0f)] public float extraPenetration = 0f;

		protected override DamagePayload Preprocess(in DamagePayload p)
		{
			// додаємо мм до поточної пенетрації (без нормалізації)
			var pen = Mathf.Max(0f, p.Penetration + extraPenetration);

			return new DamagePayload(
				p.Damage,
				pen,
				p.Type,
				p.WeaponTag,
				p.HitPoint,
				p.HitNormal,
				p.Region,
				p.AttackerId,
				p.VictimId
			);
		}
	}
}
