// Assets/Scripts/Game/Player/Weapons/Shooting/LaserFireHandler.cs
using Combat.Core;
using UnityEngine;

/// <summary>
/// Лазерний постріл з підтримкою мульти-пеністрації.
/// Один промінь може пройти через декілька колайдерів, поки вистачає пенетрації.
/// Працює аналогічно Projectile/Armor, щоб ArmorDBG показував ті ж кути / товщину.
/// </summary>
public class LaserFireHandler : IFireHandler
{
	private readonly float baseDamage;
	private readonly float basePenetrationMM;
	private readonly float maxRange;
	private readonly WeaponTag weaponTag;
	private readonly DamageType damageType;
	private readonly BulletType bulletType; // можна використати для ефектів, якщо треба
	private readonly TravelFalloff falloff;

	public LaserFireHandler(
		float damage,
		float penetrationMM,
		float maxRange,
		WeaponTag weaponTag,
		DamageType damageType,
		BulletType bulletType,
		TravelFalloff falloff)
	{
		this.baseDamage = damage;
		this.basePenetrationMM = Mathf.Max(0f, penetrationMM);
		this.maxRange = maxRange;
		this.weaponTag = weaponTag;
		this.damageType = damageType;
		this.bulletType = bulletType;
		this.falloff = falloff;
	}

	public void ExecuteFire(Transform muzzle, Vector3 direction)
	{
		if (!muzzle)
		{
			Debug.LogError("[LaserFireHandler] Muzzle is null.");
			return;
		}

		// Стартові дані пострілу
		Vector3 origin = muzzle.position;
		Vector3 dirN = direction.sqrMagnitude > 0.0001f ? direction.normalized : muzzle.forward;

		float remainingRange = maxRange;
		float remainingPen = basePenetrationMM;

		float shotTime = Time.time;
		Vector3 spawnPos = origin;

		// Коллайдери власника, щоб не влучати в себе
		Collider[] ownerCols = null;
		var root = muzzle.root;
		if (root != null)
			ownerCols = root.GetComponentsInChildren<Collider>(true);

		const int maxHits = 16; // запобіжник

		for (int i = 0; i < maxHits; i++)
		{
			if (remainingPen <= 0f || remainingRange <= 0.01f)
				break;

			int mask = Physics.DefaultRaycastLayers; // при бажанні можна брати з конфіга

			if (!Physics.Raycast(
					origin,
					dirN,
					out RaycastHit hit,
					remainingRange,
					mask,
					QueryTriggerInteraction.Collide))
			{
				// Більше немає перетинів вздовж променя.
				break;
			}

			// --- Ігнор власника ---
			if (ownerCols != null && hit.collider != null)
			{
				bool selfHit = false;
				for (int k = 0; k < ownerCols.Length; k++)
				{
					if (ownerCols[k] && hit.collider == ownerCols[k])
					{
						selfHit = true;
						break;
					}
				}
				if (selfHit)
				{
					// Пересуваємо origin трохи далі й продовжуємо.
					float used = hit.distance + 0.01f;
					origin = hit.point + dirN * 0.01f;
					remainingRange -= used;
					continue;
				}
			}

			// --- Falloff по дистанції/часу ---
			float totalDist = Vector3.Distance(spawnPos, hit.point);
			float tof = Time.time - shotTime;

			falloff.Evaluate(totalDist, tof, out float dmgMul, out float penMul);

			float dmg = baseDamage * dmgMul;
			float penForThisHit = remainingPen * penMul;

			// --- BodyRegion (голова/корпус/і т.д.) ---
			var region = BodyRegion.Default;
			var hr = hit.collider.GetComponentInParent<HitboxRegion>();
			if (hr != null) region = hr.region;

			// --- Репорт напряму в систему броні / ArmorDBG ---
			Vector3 backstepped = ImpactAngleUtil.Backstep(hit.point, dirN, 0.02f);
			HitDirectionSensor.TryReportRayHit(hit.collider, backstepped, dirN);

			// Debug-гізмо
			Debug.DrawLine(backstepped, backstepped + dirN * 1.5f, Color.magenta, 0.5f);
			Debug.DrawRay(backstepped, hit.normal * 0.7f, Color.cyan, 0.5f);

			// --- Payload у систему урону ---
			var payload = new DamagePayload(
				dmg,
				penForThisHit,
				damageType,
				weaponTag,
				backstepped,
				hit.normal,
				region,
				attackerId: root ? root.GetInstanceID() : 0,
				victimId: hit.collider.gameObject.GetInstanceID(),
				attacker: root
			);

			var targetGo = hit.collider.gameObject;

			// Якщо хочеш, щоб лазер теж користувався BulletType.OnHit (ефекти, спавн шрамів)
			if (bulletType != null)
			{
				bulletType.OnHit(targetGo, in payload);
			}
			else
			{
				var recv = targetGo.GetComponentInParent<IDamageReceiver>();
				if (recv != null) recv.TakeDamage(in payload);
				else targetGo.GetComponentInParent<DamageReceiver>()?.TakeDamage(in payload);
			}

			// --- Взаємодія з матеріалом броні / пенетрація ---
			var mat = hit.collider.GetComponentInParent<PenetrationMaterial>();
			float fakeSpeed = 1f;  // для лазера можна тримати 1.0, але API та ж сама
			Vector3 newDir = dirN;

			if (mat != null)
			{
				// Матеріал зʼїдає частину пенетрації і, за бажання, змінює напрямок (рефракція/рикошет).
				mat.ApplyMaterialLoss(ref remainingPen, ref fakeSpeed, ref newDir);
				dirN = newDir;
			}
			else
			{
				// Немає PenetrationMaterial → вважаємо що тут промінь помер.
				break;
			}

			if (remainingPen <= 0.01f)
				break;

			// --- Продовжуємо після цього хіта ---
			float consumed = hit.distance + 0.01f;
			origin = hit.point + dirN * 0.01f;
			remainingRange -= consumed;
		}
	}
}
