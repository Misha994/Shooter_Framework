using UnityEngine;

/// <summary>
/// Спеціальний PooledObject для кулі:
/// - чистить трейл;
/// - скидає velocity/ angularVelocity (якщо тіло не kinematic);
/// - дергає ResetStateOnSpawn() у Projectile.
/// </summary>
[RequireComponent(typeof(Projectile))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(TrailRenderer))]
public class PooledProjectile : PooledObject
{
	private Projectile _projectile;
	private Rigidbody _rb;
	private TrailRenderer _trail;

	private void Awake()
	{
		_projectile = GetComponent<Projectile>();
		_rb = GetComponent<Rigidbody>();
		_trail = GetComponent<TrailRenderer>();
	}

	public override void OnSpawned()
	{
		// Скидання фізики (тільки якщо не kinematic)
		if (_rb != null && !_rb.isKinematic)
		{
			_rb.velocity = Vector3.zero;
			_rb.angularVelocity = Vector3.zero;
		}

		// Скидання трейлу
		if (_trail != null)
		{
			_trail.Clear();
			_trail.emitting = true;
		}

		// Скидання внутрішнього стейту Projectile
		_projectile?.ResetStateOnSpawn();
	}

	public override void OnDespawned()
	{
		if (_rb != null && !_rb.isKinematic)
		{
			_rb.velocity = Vector3.zero;
			_rb.angularVelocity = Vector3.zero;
		}

		if (_trail != null)
		{
			_trail.emitting = false;
			_trail.Clear();
		}
	}
}
