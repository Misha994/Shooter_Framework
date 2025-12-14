// Assets/Scripts/Game/Player/Weapons/Shooting/ProjectileFireHandler.cs
using Combat.Core;
using UnityEngine;

/// <summary>
/// Хендлер пострілу фізичними снарядами (пуля, граната тощо).
/// Спавнить префаб, ініціалізує Projectile і штовхає його Rigidbody.
/// </summary>
public class ProjectileFireHandler : IFireHandler
{
	private readonly GameObject projectilePrefab;
	private readonly float force;
	private readonly BulletType bulletType;
	private readonly float baseDamage;
	private readonly float basePenetrationMM;
	private readonly WeaponTag weaponTag;
	private readonly DamageType damageType;
	private readonly TravelFalloff falloff;

	public ProjectileFireHandler(
		GameObject prefab,
		float launchForce,
		BulletType type,
		float damage,
		float penetrationMM,
		WeaponTag weaponTag,
		DamageType damageType,
		TravelFalloff falloff)
	{
		projectilePrefab = prefab;
		force = launchForce;
		bulletType = type;
		baseDamage = damage;
		basePenetrationMM = Mathf.Max(0f, penetrationMM);
		this.weaponTag = weaponTag;
		this.damageType = damageType;
		this.falloff = falloff;
	}

	public void ExecuteFire(Transform muzzle, Vector3 direction)
	{
		// 1) Перевірка префаба
		if (projectilePrefab == null)
		{
			Debug.LogError("[ProjectileFireHandler] Projectile prefab is NULL!");
			return;
		}

		// 2) Перевірка дула
		if (muzzle == null)
		{
			Debug.LogError("[ProjectileFireHandler] Muzzle is NULL, cannot spawn projectile. Check weapon setup!");
			return;
		}

		// 3) Нормалізуємо напрямок
		Vector3 dirN = direction.sqrMagnitude > 0.0001f
			? direction.normalized
			: muzzle.forward;

		Vector3 pos = muzzle.position;
		Quaternion rot = Quaternion.LookRotation(dirN);

		// 4) Спавнимо снаряд
		GameObject proj;
		if (PoolManager.Instance != null)
		{
			proj = PoolManager.Instance.Spawn(projectilePrefab, pos, rot);
		}
		else
		{
			proj = Object.Instantiate(projectilePrefab, pos, rot);
		}

		if (proj == null)
		{
			Debug.LogError("[ProjectileFireHandler] Failed to spawn projectile instance.");
			return;
		}

		// Пул вже виставляє позицію/ротацію, цей рядок більше не потрібен:
		// proj.transform.SetPositionAndRotation(pos, rot);

		// 5) Коллайдери власника, щоб не бити себе
		Collider[] ownerCols = null;
		var root = muzzle.root;
		if (root != null)
			ownerCols = root.GetComponentsInChildren<Collider>(true);

		// 6) Якщо є наш Projectile — ініціалізуємо всі параметри
		if (proj.TryGetComponent<Projectile>(out var projectile))
		{
			// Можна додатково викликати ResetStateOnSpawn, якщо хочеш жорстко гарантувати порядок
			// projectile.ResetStateOnSpawn();

			projectile.Init(
				bulletType,
				baseDamage,
				basePenetrationMM,
				dirN,
				force,
				weaponTag,
				damageType,
				ownerColliders: ownerCols,
				attacker: root,
				falloff: falloff
			);
		}
		else
		{
			// 7) Fallback: просто штовхаємо Rigidbody
			if (proj.TryGetComponent<Rigidbody>(out var rb))
			{
				rb.velocity = Vector3.zero;
				rb.angularVelocity = Vector3.zero;
				rb.AddForce(dirN * force, ForceMode.Impulse);
			}
			else
			{
				Debug.LogWarning("[ProjectileFireHandler] Projectile has no 'Projectile' and no Rigidbody. Nothing to move.");
			}

			Object.Destroy(proj, 5f);
			Debug.LogWarning("[ProjectileFireHandler] Projectile has no 'Projectile' script. Consider adding it for pooling & penetration logic.");
		}
	}
}
