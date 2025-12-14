using UnityEngine;

/// <summary>
/// Під’єднує подію пострілу зброї на техніці до фізичної віддачі танка і kick'у камер місць.
/// Ставиться на той самий GO, де й VehicleWeaponMount (або вище в ієрархії).
/// </summary>
[DisallowMultipleComponent]
public class VehicleWeaponRecoilBinder : MonoBehaviour
{
	[Header("Refs")]
	[SerializeField] private VehicleWeaponMount mount;              // де спавниться зброя
	[SerializeField] private RigidbodyRecoilReceiver rbRecoil;      // віддача на корпус
	[SerializeField] private CameraRecoilReceiver[] cameraRecoil;   // всі камери місць (можна залишити пустим — знайдеться автоматично)

	[Header("Recoil Tuning")]
	[Tooltip("Імпульс уздовж -muzzle.forward (Н·с). Це базове значення; фактичний імпульс = baseImpulse * ImpulseScale.")]
	[Min(0f)] public float baseImpulse = 2000f;

	[Tooltip("Додатковий множник — можна керувати ззовні (наприклад, тип снаряду).")]
	[Min(0f)] public float impulseScale = 1f;

	[Tooltip("Kick для камер місць (в градусах по -X), разовий імпульс.")]
	[Min(0f)] public float cameraKickDegrees = 2.5f;

	[Tooltip("Якщо true — дублюємо AddForceAtPosition безпосередньо у Rigidbody, якщо немає rbRecoil.")]
	public bool fallbackDirectPhysics = true;

	// runtime
	private WeaponBase _weapon;

	private void Reset()
	{
		if (!mount) mount = GetComponentInChildren<VehicleWeaponMount>(true);
		if (!rbRecoil) rbRecoil = GetComponentInParent<RigidbodyRecoilReceiver>();
	}

	private void Awake()
	{
		if (!mount) mount = GetComponentInChildren<VehicleWeaponMount>(true);
		if (cameraRecoil == null || cameraRecoil.Length == 0)
			cameraRecoil = GetComponentsInChildren<CameraRecoilReceiver>(true);
	}

	private void OnEnable()
	{
		if (mount != null) mount.OnWeaponReady += OnWeaponReady;
		HookCurrentWeaponIfAny();
	}

	private void OnDisable()
	{
		if (mount != null) mount.OnWeaponReady -= OnWeaponReady;
		UnhookWeapon();
	}

	private void OnWeaponReady(WeaponBase w)
	{
		UnhookWeapon();
		_weapon = w;
		if (_weapon != null)
			_weapon.Fired += OnWeaponFired;
	}

	private void HookCurrentWeaponIfAny()
	{
		if (_weapon != null) return;
		if (mount == null) return;

		// Використовуємо наданий методом інстанс WeaponBase
		var wb = mount.GetWeaponBaseUnsafe();
		if (wb != null)
		{
			_weapon = wb;
			_weapon.Fired += OnWeaponFired;
		}
	}

	private void UnhookWeapon()
	{
		if (_weapon != null)
		{
			_weapon.Fired -= OnWeaponFired;
			_weapon = null;
		}
	}

	private void OnWeaponFired()
	{
		if (_weapon == null) return;

		var muzzle = _weapon.Muzzle ? _weapon.Muzzle : _weapon.transform;
		var dir = -(muzzle.forward); // віддача у зворотньому напрямку пострілу
		var point = muzzle.position;
		var impulse = Mathf.Max(0f, baseImpulse * impulseScale);

		// 1) Фізична віддача корпусу
		if (rbRecoil != null && rbRecoil.enabled)
		{
			rbRecoil.Apply(point, dir * impulse);
		}
		else if (fallbackDirectPhysics)
		{
			var rb = GetComponentInParent<Rigidbody>();
			if (rb != null)
				rb.AddForceAtPosition(dir * impulse, point, ForceMode.Impulse);
		}

		// 2) Kick у всі знайдені камери місць (можна вказати вручну в масиві)
		if (cameraRecoil == null || cameraRecoil.Length == 0)
			cameraRecoil = GetComponentsInChildren<CameraRecoilReceiver>(true);

		if (cameraRecoil != null)
		{
			for (int i = 0; i < cameraRecoil.Length; i++)
			{
				var cr = cameraRecoil[i];
				if (cr != null && cr.isActiveAndEnabled)
					cr.Kick(cameraKickDegrees);
			}
		}
	}
}
