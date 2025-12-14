// Assets/Scripts/Game/Player/Weapons/WeaponFactory.cs
using Combat.Core;
using UnityEngine;
using Zenject;

public class WeaponFactory : IWeaponFactory
{
	private readonly IAudioService _audio;
	private readonly DiContainer _container;

	public WeaponFactory(IAudioService audioService, DiContainer container)
	{
		_audio = audioService;
		_container = container;
	}

	public WeaponBase Create(WeaponConfig config, Transform parent)
	{
		var instance = _container.InstantiatePrefab(config.weaponPrefab, parent);
		var weapon = instance.GetComponent<WeaponBase>();

		var ammo = new MagazineAmmoProvider(config);
		var sound = new WeaponSoundPlayer(_audio, config.shootSfx, config.reloadSfx, instance.transform);

		var modeIndex = Mathf.Clamp(config.defaultFireModeIndex, 0, Mathf.Max(0, config.fireModes.Count - 1));
		var desc = config.fireModes[modeIndex];

		// нове: дістаємо falloff
		var falloff = config.ResolveFalloff(modeIndex);

		IFireHandler fireHandler = config.weaponType switch
		{
			WeaponType.Bullet => new ProjectileFireHandler(
				config.projectilePrefab,
				desc.projectileForce,
				config.bulletType,
				desc.damage,
				desc.penetration,
				config.weaponTag,
				config.damageType,
				falloff),

			WeaponType.Grenade => new ProjectileFireHandler(
				config.projectilePrefab,
				desc.projectileForce,
				config.bulletType,
				desc.damage,
				desc.penetration,
				config.weaponTag,
				DamageType.Explosive,
				falloff),

			WeaponType.Laser => new LaserFireHandler(
				desc.damage,
				desc.penetration,
				desc.raycastRange > 0f ? desc.raycastRange : config.defaultRaycastRange,
				config.weaponTag,
				config.damageType,
				config.bulletType,
				falloff),

			_ => throw new System.Exception("Unknown weapon type")
		};

		weapon.Initialize(config, ammo, fireHandler, sound);
		return weapon;
	}
}
