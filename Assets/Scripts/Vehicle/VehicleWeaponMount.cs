using System;
using UnityEngine;
using Zenject;

[DefaultExecutionOrder(-100)]
[DisallowMultipleComponent]
public class VehicleWeaponMount : MonoBehaviour
{
	[Header("Config")]
	public WeaponConfig weaponConfig;

	[Header("Mount points")]
	public Transform mountParent;
	public Transform muzzle;

	[Tooltip("ќпц≥йно. якщо не задано Ч г≥льзи не викидаютьс€.")]
	public Transform casingEjectPoint;

	/// <summary>”н≥версальний ≥нтерфейс зброњ.</summary>
	public IWeapon Weapon { get; private set; }
	public event Action<WeaponBase> OnWeaponReady;

	private WeaponBase _weaponInstance;
	[Inject] private IWeaponFactory _factory;

#if UNITY_EDITOR
	private void OnValidate() { AutoWireMuzzleIfMissing(createIfMissing: false); }
	private void Reset() { AutoWireMuzzleIfMissing(createIfMissing: false); }
#endif

	private void Awake()
	{
		// п≥дхоплюЇмо лише muzzle; casing Ќ≈ створюЇмо навмисно
		AutoWireMuzzleIfMissing(createIfMissing: true);

		if (!ValidateInputs())
		{
			enabled = false;
			return;
		}

		_weaponInstance = _factory.Create(weaponConfig, mountParent);
		if (_weaponInstance == null)
		{
			Debug.LogError("[VehicleWeaponMount] Factory returned null WeaponBase.", this);
			enabled = false;
			return;
		}

		// casingEjectPoint ћќ∆≈ бути null Ч це означаЇ Ђбез г≥льзї
		_weaponInstance.BindSockets(muzzle, casingEjectPoint);

		Weapon = _weaponInstance;
		SafeRaiseReady(_weaponInstance);

#if UNITY_EDITOR
		Debug.Log($"[VehicleWeaponMount] Ready on '{name}'. weapon='{SafeName(_weaponInstance)}', muzzle='{SafeName(muzzle)}', casing='{SafeName(casingEjectPoint)}'", this);
#endif
	}

	private bool ValidateInputs()
	{
		if (weaponConfig == null)
		{
			Debug.LogError("[VehicleWeaponMount] Missing 'weaponConfig'.", this);
			return false;
		}
		if (mountParent == null)
		{
			Debug.LogError("[VehicleWeaponMount] Missing 'mountParent'.", this);
			return false;
		}
		if (muzzle == null)
		{
			Debug.LogError("[VehicleWeaponMount] Missing 'muzzle'.", this);
			return false;
		}
		return true; // casing може бути null
	}

	private void AutoWireMuzzleIfMissing(bool createIfMissing)
	{
		if (muzzle != null) return;

		muzzle = transform.Find("FirePoint");
		if (muzzle == null && createIfMissing)
		{
			var go = new GameObject("FirePoint");
			go.transform.SetParent(transform, false);
			go.transform.localPosition = Vector3.forward * 1f;
			go.transform.localRotation = Quaternion.identity;
			muzzle = go.transform;
		}
	}

	public WeaponBase GetWeaponBaseUnsafe() => _weaponInstance;

	public bool CanFire => Weapon != null && Weapon.CanFire;
	public void Fire() => Weapon?.Fire();
	public void Reload() => Weapon?.Reload();

	public void RebindSockets(Transform newMuzzle, Transform newCasingOrNull)
	{
		if (_weaponInstance == null) return;
		_weaponInstance.BindSockets(newMuzzle, newCasingOrNull); // newCasingOrNull може бути null
#if UNITY_EDITOR
		Debug.Log($"[VehicleWeaponMount] RebindSockets => muzzle='{SafeName(newMuzzle)}', casing='{SafeName(newCasingOrNull)}'", this);
#endif
	}

	private void SafeRaiseReady(WeaponBase wb)
	{
		try { OnWeaponReady?.Invoke(wb); }
		catch (Exception e) { Debug.LogException(e, this); }
	}

#if UNITY_EDITOR
	private static string SafeName(UnityEngine.Object o) => o == null ? "(null)" : o.name;

	private void OnDrawGizmosSelected()
	{
		if (muzzle != null)
		{
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireSphere(muzzle.position, 0.04f);
			Gizmos.DrawLine(muzzle.position, muzzle.position + muzzle.forward * 0.25f);
		}

		if (casingEjectPoint != null)
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(casingEjectPoint.position, 0.035f);
			Gizmos.DrawLine(casingEjectPoint.position, casingEjectPoint.position + casingEjectPoint.forward * 0.2f);
		}
	}
#endif
}
