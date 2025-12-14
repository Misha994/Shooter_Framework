using UnityEngine;

/// <summary>
/// Базовий клас усієї вогнепальної зброї (і піхоти, і техніки).
/// Реалізує IWeapon, тримає посилання на конфіги/сокети/провайдери,
/// має подію Fired для інтеграцій (віддача, кік камери тощо).
/// Конкретну логіку стрільби реалізують нащадки (короткі/довгі черги, танкова гармата і т.д.).
/// </summary>
public abstract class WeaponBase : MonoBehaviour, IWeapon
{
	[Header("Sockets")]
	[SerializeField] protected Transform muzzle;
	[SerializeField] protected Transform casingEjectPoint;

	// Основні залежності, які зазвичай сетяться фабрикою
	protected WeaponConfig config;
	protected IAmmoProvider ammo;
	protected IFireHandler fireHandler;
	protected IWeaponSoundPlayer soundPlayer;

	// Внутрішні стейти
	protected bool isFiring;
	protected bool isReloading;

	// Режими вогню
	protected int currentFireModeIndex = 0;
	protected FireModeDescriptor currentDescriptor;

	// ADS (прицілювання)
	protected bool isAiming;
	public bool IsAiming => isAiming;
	public virtual void SetAim(bool aiming) { isAiming = aiming; }

	// Для авто-режимів (коли кнопка утримується ззовні, напр. ІІ або тригер місця)
	public bool IsFireHeldExternally { get; set; }
	public virtual bool IsFireButtonHeld() => IsFireHeldExternally;

	// ==== IWeapon API ====
	public virtual bool CanFire => ammo != null && ammo.CanShoot && !isFiring && !isReloading;
	public virtual float FireRate => currentDescriptor != null ? currentDescriptor.fireRate : 0.2f;
	public virtual WeaponFireMode FireMode => currentDescriptor != null ? currentDescriptor.mode : WeaponFireMode.Single;

	public Transform Muzzle => muzzle;
	public Transform CasingPoint => casingEjectPoint;
	public WeaponConfig Config => config;
	public IAmmoProvider Ammo => ammo;
	public IFireHandler FireHandler => fireHandler;
	public IWeaponSoundPlayer SoundPlayer => soundPlayer;

	/// <summary>
	/// Подія «режим вогню змінено».
	/// Слухачі (контролер, HUD) мають перебудувати свій стан.
	/// </summary>
	public event System.Action<FireModeDescriptor> FireModeChanged;

	/// <summary> Виклич при зміні режиму вогню. </summary>
	protected void RaiseFireModeChanged() => FireModeChanged?.Invoke(currentDescriptor);

	/// <summary>
	/// Ініціалізація зброї (викликається фабрикою). Передаємо конфіг і провайдери.
	/// </summary>
	public virtual void Initialize(WeaponConfig cfg, IAmmoProvider ammoProvider, IFireHandler fire, IWeaponSoundPlayer sound)
	{
		config = cfg;
		ammo = ammoProvider;
		fireHandler = fire;
		soundPlayer = sound;

		currentFireModeIndex = Mathf.Clamp(cfg.defaultFireModeIndex, 0, cfg.fireModes.Count > 0 ? cfg.fireModes.Count - 1 : 0);
		currentDescriptor = (cfg.fireModes.Count > 0) ? cfg.fireModes[currentFireModeIndex] : new FireModeDescriptor();
	}

	// Конкретна реалізація стрільби/перезарядки — у нащадках
	public abstract void Fire();
	public abstract void Reload();

	/// <summary> Перемкнути режим вогню, якщо доступно. </summary>
	public virtual bool TrySwitchFireMode()
	{
		if (config == null || config.fireModes == null || config.fireModes.Count <= 1)
			return false;

		currentFireModeIndex = (currentFireModeIndex + 1) % config.fireModes.Count;
		currentDescriptor = config.fireModes[currentFireModeIndex];

		// ВАЖЛИВО: повідомляємо слухачів, що режим змінився
		RaiseFireModeChanged();
		return true;
	}

	/// <summary> Обробка викидання гільз (пул або Instantiate). </summary>
	public virtual void HandleCasing()
	{
		if (config == null || !config.ejectCasing || config.casingPrefab == null || casingEjectPoint == null)
			return;

		GameObject casing = PoolManager.Instance
			? PoolManager.Instance.Spawn(config.casingPrefab, casingEjectPoint.position, casingEjectPoint.rotation)
			: Instantiate(config.casingPrefab, casingEjectPoint.position, casingEjectPoint.rotation);

		if (casing.TryGetComponent<Rigidbody>(out var rb))
		{
			rb.velocity = Vector3.zero;
			rb.angularVelocity = Vector3.zero;

			float force = Mathf.Max(0f, config.casingEjectForce);

			// невеликий конус розльоту
			Vector3 dir = casingEjectPoint.forward;
			if (config.casingRandomAngle > 0f)
			{
				float yaw = Random.Range(-config.casingRandomAngle, config.casingRandomAngle);
				float pitch = Random.Range(-config.casingRandomAngle, config.casingRandomAngle);
				dir = Quaternion.Euler(pitch, yaw, 0f) * dir;
			}
			rb.AddForce(dir * force, ForceMode.Impulse);

			float spin = Mathf.Abs(config.casingRandomSpin);
			if (spin > 0f)
				rb.AddTorque(Random.insideUnitSphere * spin, ForceMode.Impulse);
		}

		var auto = casing.GetComponent<CasingAutoReturn>();
		if (!auto) auto = casing.AddComponent<CasingAutoReturn>();
		auto.Begin(config.casingPrefab, Mathf.Max(0.1f, config.casingLifetime));
	}

	/// <summary>
	/// Повертає активний розкид з урахуванням ADS (якщо прицілюємось — зменшуємо).
	/// </summary>
	protected float GetCurrentSpread(float baseSpread)
	{
		if (isAiming)
			return baseSpread * (config != null ? config.aimSpreadMultiplier : 0.35f);
		return baseSpread;
	}

	// ==== Подія пострілу для інтеграцій (віддача/камера/ефекти) ====
	/// <summary> Викликається кожного разу, коли зброя успішно здійснила постріл. </summary>
	public event System.Action Fired;

	/// <summary>
	/// Виклич це відразу після того, як ШОТ стався (після SpawnProjectile/ConsumeAmmo/звук тощо).
	/// Рекомендується робити це у нащадку в кінці Fire()/корутини пострілу.
	/// </summary>
	protected void RaiseFired() => Fired?.Invoke();

	// ==== Життєвий цикл монтування/холстера ====
	/// <summary> Коли зброю дістали/оснастили (викликає твій WeaponMount/інвентар). </summary>
	public virtual void OnEquipped()
	{
		CancelFiringState();
		SetAim(false);
		gameObject.SetActive(true);
	}

	/// <summary> Коли зброю сховали/переклали у слот (викликає твій WeaponMount/інвентар). </summary>
	public virtual void OnHolstered()
	{
		CancelFiringState();
		SetAim(false);
		// не обов'язково вимикати тут GO — цим займається Mount/інвентар
	}

	/// <summary> Перепризначити сокети (викликає VehicleWeaponMount). </summary>
	public void BindSockets(Transform newMuzzle, Transform newCasing)
	{
		if (newMuzzle != null) muzzle = newMuzzle;
		if (newCasing != null) casingEjectPoint = newCasing;
	}

	/// <summary> Скидання корутин/флагів на випадок свічу/переривання. </summary>
	public virtual void CancelFiringState()
	{
		isFiring = false;
		isReloading = false;
		StopAllCoroutines();
	}
}
