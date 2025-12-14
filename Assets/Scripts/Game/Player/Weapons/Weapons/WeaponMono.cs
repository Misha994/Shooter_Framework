using System.Collections;
using UnityEngine;

public class WeaponMono : WeaponBase
{
	private Coroutine _fireRoutine;
	private Coroutine _reloadRoutine;

	private Animator _anim;
	private RecoilKick _recoil;
	private RigWeightBlender _rigBlend; // щоб притиснути/підняти руки на релоді

	private static readonly int HashFire = Animator.StringToHash("Fire");
	private static readonly int HashReload = Animator.StringToHash("Reload");

	private float _nextFireTime; // глобальний кулдаун

	private void Awake()
	{
		_anim = GetComponentInParent<Animator>();
		_recoil = GetComponentInParent<RecoilKick>();
		_rigBlend = GetComponentInParent<RigWeightBlender>();
	}

	// ==== overrides життєвих хуків (нове) ====
	public override void OnEquipped()
	{
		base.OnEquipped();
		// За потреби — скинути анімаційні тригери/стани
		if (_anim)
		{
			_anim.ResetTrigger(HashFire);
			_anim.ResetTrigger(HashReload);
		}
		_rigBlend?.SetReloading(false);
	}

	public override void OnHolstered()
	{
		base.OnHolstered();
		_rigBlend?.SetReloading(false);
	}

	// === IWeapon ===

	public override bool CanFire =>
		ammo != null &&
		ammo.CanShoot &&
		!isFiring &&
		!isReloading &&
		Time.time >= _nextFireTime;

	public override void Fire()
	{
		if (!CanFire) return;
		if (_fireRoutine == null)
			_fireRoutine = StartCoroutine(FireBurstOnce());
	}

	public override void Reload()
	{
		if (isReloading || ammo == null) return;

		if (ammo is MagazineAmmoProvider m)
		{
			if (m.CurrentAmmo == Config.magazineSize || m.ReserveAmmo <= 0)
				return;
		}

		if (_fireRoutine != null)
		{
			StopCoroutine(_fireRoutine);
			_fireRoutine = null;
			isFiring = false;
		}

		if (_reloadRoutine == null)
			_reloadRoutine = StartCoroutine(ReloadRoutine());
	}

	private void OnDisable()
	{
		CancelFiringState();
		_rigBlend?.SetReloading(false);
	}

	private IEnumerator FireBurstOnce()
	{
		var desc = currentDescriptor;
		isFiring = true;

		int pellets = Mathf.Max(1, desc.bulletsPerShot);
		bool shotgunStyle = desc.isShotgunStyle;
		bool ejected = false;

		for (int i = 0; i < pellets; i++)
		{
			if (!ammo.CanShoot || isReloading)
			{
				Debug.LogWarning($"[WeaponMono] FireBurstOnce break at pellet={i}. CanShoot={ammo.CanShoot}, isReloading={isReloading}");
				break;
			}

			float spreadNow = GetCurrentSpread(desc.spread);
			Vector3 dir = Quaternion.Euler(
				Random.Range(-spreadNow, spreadNow),
				Random.Range(-spreadNow, spreadNow),
				0) * Muzzle.forward;

			// ДІАГНОСТИКА: ми дійшли до виклику ExecuteFire
			Debug.Log($"[WeaponMono] ExecuteFire pellet={i}, muzzlePos={Muzzle.position}");

			fireHandler.ExecuteFire(Muzzle, dir);

			Debug.Log($"[WeaponMono] After ExecuteFire pellet={i}");

			soundPlayer?.PlayShootSound();
			ammo.Consume();
			Debug.Log($"[WeaponMono] After Consume pellet={i}");

			if (!shotgunStyle || !ejected)
			{
				HandleCasing();
				ejected = true;
			}

			_recoil?.OnFired();
			_anim?.SetTrigger(HashFire);
			RaiseFired();

			if (desc.delayBetweenBullets > 0 && i < pellets - 1)
				yield return new WaitForSeconds(desc.delayBetweenBullets);
		}

		_nextFireTime = Time.time + Mathf.Max(0.01f, desc.fireRate);

		isFiring = false;
		_fireRoutine = null;
	}

	private IEnumerator ReloadRoutine()
	{
		isReloading = true;

		_rigBlend?.SetReloading(true);
		_anim?.SetTrigger(HashReload);
		soundPlayer?.PlayReloadSound();

		float time = Mathf.Max(0.05f, config != null ? config.reloadTime : 1.5f);
		yield return new WaitForSeconds(time);

		ammo.TryReload();

		_rigBlend?.SetReloading(false);
		isReloading = false;
		_reloadRoutine = null;

		_nextFireTime = Mathf.Max(_nextFireTime, Time.time + 0.03f);
	}
}
