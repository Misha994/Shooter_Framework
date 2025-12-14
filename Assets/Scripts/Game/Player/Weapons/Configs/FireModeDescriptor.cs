using UnityEngine;

[System.Serializable]
public class FireModeDescriptor
{
    public string modeName = "BurstAuto";
    public WeaponFireMode mode = WeaponFireMode.Auto;

    public float damage = 20f;
    public float penetration = 2f;

    [Header("Projectile")]
    public float projectileForce = 800f;

    [Header("Cadence & Spread")]
    public float fireRate = 0.5f;
    public float spread = 1.5f;
    public int bulletsPerShot = 3;
    public float delayBetweenBullets = 0.1f;

    [Header("Mode flags")]
    public bool isAutoBurst = true;
    public bool isShotgunStyle = false;

    [Header("Raycast (for Laser)")]
    [Tooltip("ƒальн≥сть рейкасту дл€ лазера. якщо <= 0 Ч використовуЇтьс€ WeaponConfig.defaultRaycastRange.")]
    public float raycastRange = -1f;
}
