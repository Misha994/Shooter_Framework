using UnityEngine;
using Combat.Core;

/// <summary>
/// Фізична куля з підтримкою пулу, трейлу та простим хіт-сканом між кадрами.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class Projectile : MonoBehaviour
{
	[Header("Lifetime & Collisions")]
	[SerializeField] private float maxLifeSeconds = 8f;
	[SerializeField] private LayerMask hitMask = ~0;
	[SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;

	// Кеші
	private Rigidbody _rb;
	private TrailRenderer _trail;

	// Бойові параметри (сетяться через Init)
	private BulletType _bulletType;
	private float _baseDamage;
	private float _basePenetrationMM;
	private WeaponTag _weaponTag;
	private DamageType _damageType;
	private Transform _attacker;
	private Collider[] _ownerCols;
	private TravelFalloff _falloff;

	// Стейт польоту
	private Vector3 _spawnPos;
	private float _spawnTime;
	private bool _initialized;

	private Vector3 _prevPos;
	private bool _hasPrevPos;
	private Vector3 _lastVel;
	private float _remainingPenMM;
	private float _currentSpeed;

	private void Awake()
	{
		_rb = GetComponent<Rigidbody>();
		_trail = GetComponent<TrailRenderer>();
	}

	/// <summary>
	/// Викликається PooledProjectile / PooledObject при спавні з пулу.
	/// Скидає стейт, але ще НЕ знає напрямку/швидкості.
	/// </summary>
	public void ResetStateOnSpawn()
	{
		_initialized = false;
		_hasPrevPos = false;

		_prevPos = transform.position;
		_lastVel = Vector3.zero;
		_currentSpeed = 0f;
		_remainingPenMM = 0f;

		if (_rb != null && !_rb.isKinematic)
		{
			_rb.velocity = Vector3.zero;
			_rb.angularVelocity = Vector3.zero;
			_rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			_rb.interpolation = RigidbodyInterpolation.Interpolate;
		}

		if (_trail != null)
		{
			_trail.Clear();
			_trail.emitting = true;
		}
	}

	/// <summary>
	/// Повна ініціалізація кулі при пострілі.
	/// ВАЖЛИВО: викликається ПІСЛЯ того, як пул виставив позицію в точку дула.
	/// </summary>
	public void Init(
		BulletType bulletType,
		float baseDamage,
		float basePenetrationMM,
		Vector3 shootDir,
		float impulse,
		WeaponTag weaponTag,
		DamageType damageType,
		Collider[] ownerColliders,
		Transform attacker,
		TravelFalloff falloff)
	{
		_bulletType = bulletType;
		_baseDamage = baseDamage;
		_basePenetrationMM = Mathf.Max(0f, basePenetrationMM);
		_weaponTag = weaponTag;
		_damageType = damageType;
		_attacker = attacker;
		_ownerCols = ownerColliders;
		_falloff = falloff;

		_spawnPos = transform.position;
		_spawnTime = Time.time;
		_remainingPenMM = _basePenetrationMM;

		Vector3 dirN = shootDir.sqrMagnitude > 0.0001f ? shootDir.normalized : transform.forward;

		if (_rb != null && !_rb.isKinematic)
		{
			_rb.velocity = Vector3.zero;
			_rb.angularVelocity = Vector3.zero;

			_rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
			_rb.interpolation = RigidbodyInterpolation.Interpolate;

			_rb.AddForce(dirN * impulse, ForceMode.Impulse);

			_lastVel = _rb.velocity;
		}
		else
		{
			float speed = impulse;
			_lastVel = dirN * speed;
		}

		_currentSpeed = _lastVel.magnitude;

		_prevPos = transform.position;
		_hasPrevPos = false; // перший крок не рейкастимо

		_initialized = true;
	}

	private void OnEnable()
	{
		_hasPrevPos = false;

		if (_trail != null)
		{
			_trail.Clear();
			_trail.emitting = true;
		}
	}

	private void OnDisable()
	{
		if (_trail != null)
		{
			_trail.emitting = false;
			_trail.Clear();
		}

		_initialized = false;
		_hasPrevPos = false;
	}

	private void FixedUpdate()
	{
		if (!_initialized)
			return;

		// 1) Перевірка часу життя
		if (Time.time - _spawnTime >= maxLifeSeconds)
		{
			SelfDestruct();
			return;
		}

		Vector3 currentPos = transform.position;

		// 2) Оновлюємо швидкість
		if (_rb != null && !_rb.isKinematic)
		{
			_lastVel = _rb.velocity;
			_currentSpeed = _rb.velocity.magnitude;
			currentPos = transform.position;
		}
		else
		{
			Vector3 delta = _lastVel * Time.fixedDeltaTime;
			currentPos = transform.position + delta;
		}

		// 3) Хіт-скан між попередньою та поточною позицією
		if (_hasPrevPos)
		{
			Vector3 seg = currentPos - _prevPos;
			float dist = seg.magnitude;

			if (dist > 0.0001f)
			{
				Ray ray = new Ray(_prevPos, seg.normalized);

				if (Physics.SphereCast(ray, 0.02f, out var hit, dist, hitMask, triggerInteraction))
				{
					if (!IsOwnerCollider(hit.collider))
					{
						OnHit(hit);
						return;
					}
				}
			}
		}

		// 4) Реальне переміщення, якщо рухаємо вручну
		if (_rb == null || _rb.isKinematic)
			transform.position = currentPos;

		_prevPos = transform.position;
		_hasPrevPos = true;
	}

	/// <summary>
	/// Ігноруємо власні коллайдери (стрілець).
	/// </summary>
	private bool IsOwnerCollider(Collider col)
	{
		if (_ownerCols == null) return false;

		for (int i = 0; i < _ownerCols.Length; i++)
		{
			if (_ownerCols[i] == null) continue;
			if (_ownerCols[i] == col) return true;
		}

		return false;
	}

	/// <summary>
	/// Обробка попадання: формуємо DamagePayload як у LaserFireHandler і шлемо в BulletType/Armor.
	/// </summary>
	private void OnHit(RaycastHit hit)
	{
		// --- Falloff по дистанції/часу ---
		float totalDist = Vector3.Distance(_spawnPos, hit.point);
		float tof = Time.time - _spawnTime;

		_falloff.Evaluate(totalDist, tof, out float dmgMul, out float penMul);

		float dmg = _baseDamage * dmgMul;
		float penForThisHit = _remainingPenMM * penMul;

		// --- BodyRegion (голова/корпус і т.д.) ---
		var region = BodyRegion.Default;
		var hr = hit.collider.GetComponentInParent<HitboxRegion>();
		if (hr != null)
			region = hr.region;

		// --- Напрямок для HitDirectionSensor / ArmorDBG ---
		Vector3 dir = _lastVel.sqrMagnitude > 1e-6f
			? _lastVel.normalized
			: (hit.point - _prevPos).normalized;

		if (dir.sqrMagnitude < 1e-6f)
			dir = -hit.normal;

		Vector3 backstepped = ImpactAngleUtil.Backstep(hit.point, dir, 0.02f);
		HitDirectionSensor.TryReportRayHit(hit.collider, backstepped, dir);

		// Дебаг лінії – як у лазера, тільки інший колір
		Debug.DrawLine(backstepped, backstepped + dir * 1.5f, Color.yellow, 0.5f);
		Debug.DrawRay(backstepped, hit.normal * 0.7f, Color.red, 0.5f);

		// --- Payload у систему урону ---
		var payload = new DamagePayload(
			dmg,
			penForThisHit,
			_damageType,
			_weaponTag,
			backstepped,
			hit.normal,
			region,
			attackerId: _attacker ? _attacker.GetInstanceID() : 0,
			victimId: hit.collider.gameObject.GetInstanceID(),
			attacker: _attacker
		);

		var targetGo = hit.collider.gameObject;

		// Якщо є BulletType – даємо йому можливість відпрацювати (ефекти/мульти-каски і т.д.)
		if (_bulletType != null)
		{
			_bulletType.OnHit(targetGo, in payload);
		}
		else
		{
			// Прямий виклик в DamageReceiver
			var recv = targetGo.GetComponentInParent<IDamageReceiver>();
			if (recv != null)
				recv.TakeDamage(in payload);
			else
				targetGo.GetComponentInParent<DamageReceiver>()?.TakeDamage(in payload);
		}

		// Поки що – одне попадання → смерть кулі.
		SelfDestruct();
	}

	private void SelfDestruct()
	{
		if (!_initialized) return;
		_initialized = false;

		if (TryGetComponent<PooledObject>(out var po))
		{
			po.ReturnToPool();
		}
		else if (PoolManager.Instance != null)
		{
			PoolManager.Instance.Despawn(gameObject);
		}
		else
		{
			Destroy(gameObject);
		}
	}
}
