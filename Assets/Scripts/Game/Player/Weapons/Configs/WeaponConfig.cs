// Assets/Scripts/Game/Player/Weapons/WeaponConfig.cs
using UnityEngine;
using System.Collections.Generic;
using Combat.Core;

[CreateAssetMenu(fileName = "WeaponConfig", menuName = "FPS/Weapon Config")]
public class WeaponConfig : ScriptableObject
{
	[Header("Fire Modes")]
	public List<FireModeDescriptor> fireModes = new();
	public int defaultFireModeIndex = 0;

	[Header("General")]
	public string weaponName;
	public WeaponType weaponType;
	public GameObject weaponPrefab;

	[Header("Ammo")]
	public int magazineSize = 30;
	public int maxAmmo = 120;
	public float reloadTime = 1.5f;

	[Header("Bullet Settings (legacy)")]
	public BulletType bulletType; // сумісність/дефолт

	[Header("Visuals & Sounds")]
	public GameObject projectilePrefab;
	public GameObject casingPrefab;
	public AudioClip shootSfx;
	public AudioClip reloadSfx;
	public AnimationClip reloadAnimation;

	[Header("Casing Ejection")]
	public bool ejectCasing = true;
	public float casingEjectForce = 2f;
	public float casingRandomAngle = 8f;
	public float casingRandomSpin = 1.5f;
	public float casingLifetime = 4f;

	[Header("ADS (Aim Down Sights)")]
	public float aimSpreadMultiplier = 0.35f;
	public float aimTimeSeconds = 0.15f;
	public float hipFOV = 60f;
	public float adsFOV = 50f;
	public float fovLerpTime = 0.12f;
	public bool aimToggle = true;
	public float adsSensitivityMultiplier = 0.6f;

	[Header("Raycast Defaults")]
	public float defaultRaycastRange = 100f;

	[Header("Animations")]
	public WeaponAnimationSet animSet;

	[Header("Damage Tagging (defaults)")]
	public WeaponTag weaponTag = WeaponTag.Rifle;
	public DamageType damageType = DamageType.Kinetic;

	// ---------------------------
	// Travel Falloff (Distance/Time)
	// ---------------------------

	[System.Serializable]
	public struct FireModeFalloffOverride
	{
		[Tooltip("Індекс у списку fireModes")]
		public int modeIndex;

		[Tooltip("Увімкнути override для цього режиму")]
		public bool enable;

		[Tooltip("Налаштування спадання для цього режиму")]
		public TravelFalloff falloff;
	}

	[Header("Falloff: Profile / Local / Per-Mode")]
	[Tooltip("Опціональний профіль правил (APFSDS/HEAT/HE/Laser тощо)")]
	public AmmoFalloffProfile falloffProfile;

	[Tooltip("Локальний дефолт — застосовується якщо нема per-mode або профіль нічого не вирішив")]
	public TravelFalloff localFalloff = TravelFalloff.Disabled;

	[Tooltip("Per-mode overrides (мають найвищий пріоритет)")]
	public List<FireModeFalloffOverride> perModeFalloff = new();

	/// <summary>
	/// Розв'язати TravelFalloff з урахуванням пріоритету:
	/// 1) Per-Mode override (за індексом fire mode)
	/// 2) Local (якщо enable у структурі)
	/// 3) AmmoFalloffProfile (якщо заданий), інакше Disabled
	/// </summary>
	public TravelFalloff ResolveFalloff(int fireModeIndex)
	{
		// 1) Per-Mode
		if (perModeFalloff != null)
		{
			for (int i = 0; i < perModeFalloff.Count; i++)
			{
				var e = perModeFalloff[i];
				if (e.enable && e.modeIndex == fireModeIndex)
					return e.falloff.enable ? e.falloff : TravelFalloff.Disabled;
			}
		}

		// 2) Local
		if (localFalloff.enable)
			return localFalloff;

		// 3) Profile
		if (falloffProfile != null)
		{
			// профіль може матчити за DamageType/WeaponTag/BulletType
			var fromProfile = falloffProfile.Resolve(damageType, weaponTag, bulletType);
			return fromProfile.enable ? fromProfile : TravelFalloff.Disabled;
		}

		return TravelFalloff.Disabled;
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		// підчистити індекс за межами
		if (fireModes == null) fireModes = new List<FireModeDescriptor>();
		if (fireModes.Count == 0) defaultFireModeIndex = 0;
		else defaultFireModeIndex = Mathf.Clamp(defaultFireModeIndex, 0, fireModes.Count - 1);

		// нормалізація perModeFalloff: відсіяти негативні індекси
		if (perModeFalloff != null)
		{
			for (int i = perModeFalloff.Count - 1; i >= 0; i--)
			{
				if (perModeFalloff[i].modeIndex < 0)
					perModeFalloff.RemoveAt(i);
			}
		}
	}
#endif
}
