// Assets/Scripts/Game/Player/Weapons/Falloff/AmmoFalloffProfile.cs
using System.Collections.Generic;
using UnityEngine;
using Combat.Core;

[CreateAssetMenu(fileName = "AmmoFalloffProfile", menuName = "FPS/Ammo Falloff Profile")]
public sealed class AmmoFalloffProfile : ScriptableObject
{
	[System.Serializable]
	public struct Rule
	{
		public bool matchDamageType; public DamageType damageType;
		public bool matchWeaponTag; public WeaponTag weaponTag;
		public bool matchBulletType; public BulletType bulletType;

		[Tooltip("TravelFalloff, який буде застосовано якщо всі увімкнені матчі співпали")]
		public TravelFalloff falloff;

		public bool Matches(DamageType dt, WeaponTag tag, BulletType bt)
		{
			if (matchDamageType && dt != damageType) return false;
			if (matchWeaponTag && tag != weaponTag) return false;
			if (matchBulletType && bt != bulletType) return false;
			return true;
		}
	}

	[Header("Default (якщо жодне правило не спрацює)")]
	public TravelFalloff defaultFalloff;

	[Header("Rules (перевіряються зверху-вниз)")]
	public List<Rule> rules = new();

	public TravelFalloff Resolve(DamageType dt, WeaponTag tag, BulletType bt)
	{
		for (int i = 0; i < (rules?.Count ?? 0); i++)
		{
			var r = rules[i];
			if (r.falloff.enable && r.Matches(dt, tag, bt))
				return r.falloff;
		}
		return defaultFalloff.enable ? defaultFalloff : TravelFalloff.Disabled;
	}
}
