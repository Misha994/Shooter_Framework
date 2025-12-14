using UnityEngine;

public class VehicleGunnerController : MonoBehaviour
{
	[Header("Refs")]
	[SerializeField] private VehicleTurretController turret;
	[SerializeField] private MonoBehaviour weaponBehaviour; // у інспекторі вкажи VehicleWeaponMount

	private IWeapon _weapon;
	private VehicleWeaponMount _weaponMount;
	private IInputService _input;

	[Header("Aim Speeds")]
	public float yawSpeed = 120f;    // deg/sec
	public float pitchSpeed = 75f;   // deg/sec
	public float minPitch = -10f;
	public float maxPitch = 25f;

	private void Awake()
	{
		// Підхопити VehicleWeaponMount без небезпечних кастів
		_weaponMount = weaponBehaviour as VehicleWeaponMount
					   ?? (weaponBehaviour != null ? (weaponBehaviour as Component)?.GetComponent<VehicleWeaponMount>() : null);

		if (_weaponMount != null)
		{
			var wb = _weaponMount.GetWeaponBaseUnsafe();
			if (wb != null) AttachWeaponFromBase(wb);
			_weaponMount.OnWeaponReady += AttachWeaponFromBase;
		}
		else
		{
			// fallback: якщо в полі поклали щось, що реалізує IWeapon
			if (weaponBehaviour is IWeapon w) _weapon = w;
		}

		// Дозволяємо башті приймати зовнішні дельти і синхронізуємо межі піча
		if (turret != null)
		{
			turret.allowExternalInput = true;
			minPitch = turret.minPitch;
			maxPitch = turret.maxPitch;
		}
	}

	private void OnDestroy()
	{
		if (_weaponMount != null)
			_weaponMount.OnWeaponReady -= AttachWeaponFromBase;
	}

	/// <summary>
	/// Викликається VehicleSeat.SetOccupied(...): проброс інпуту з сидіння у стрільця (і в башту).
	/// </summary>
	public void SetSeatInput(IInputService input)
	{
		_input = input;
		if (turret != null) turret.SetSeatInput(input);
	}

	private void AttachWeaponFromBase(WeaponBase wb)
	{
		_weapon = wb as IWeapon; // достатньо інтерфейсу для Fire/Reload
	}

	private void Update()
	{
		if (turret == null) return;

		// Якщо є локальний інпут — додаємо зовнішні дельти (дозволяє міксувати із самим турельним інпутом)
		if (_input != null)
		{
			Vector2 look = _input.GetLookDelta();

			float yawDelta = look.x * yawSpeed * Time.deltaTime;
			float pitchDelta = -look.y * pitchSpeed * Time.deltaTime; // інверсія для «мишкою вгору — гармата вгору»

			turret.ApplyExternalYaw(yawDelta);
			turret.ApplyExternalPitch(pitchDelta); // всередині turret є clamp до [minPitch..maxPitch]
		}

		// Вогонь утриманням
		if (_input != null && _weapon != null && _weapon.CanFire && _input.IsShootHeld())
			_weapon.Fire();

		// Перезарядка разовим натиском
		if (_input != null && _input.IsReloadPressed())
			_weapon?.Reload();

		// (необов’язково) можна обробити aim для зум-камери сидіння
		// bool aiming = _input?.IsAimHeld() ?? false;
		// ... перемкнути SeatCamera або змінити FOV
	}
}
