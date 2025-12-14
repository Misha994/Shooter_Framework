using UnityEngine;

public class MagazineAmmoProvider : IAmmoProvider
{
    private int currentAmmo;
    private int reserveAmmo;
    private WeaponConfig config;

    public MagazineAmmoProvider(WeaponConfig cfg)
    {
        config = cfg;
        currentAmmo = cfg.magazineSize;
        reserveAmmo = cfg.maxAmmo;

        Debug.Log($"[AmmoProvider] Initialized. Current: {currentAmmo}, Reserve: {reserveAmmo}");
    }

    public bool CanShoot
    {
        get
        {
            bool can = currentAmmo > 0;
            if (!can) Debug.LogWarning("[AmmoProvider] CanShoot == false (no bullets)");
            return can;
        }
    }

    public bool TryReload()
    {
        if (reserveAmmo <= 0 || currentAmmo == config.magazineSize)
            return false;

        int needed = config.magazineSize - currentAmmo;
        int toReload = Mathf.Min(needed, reserveAmmo);
        currentAmmo += toReload;
        reserveAmmo -= toReload;

        Debug.Log($"[AmmoProvider] Reloaded {toReload} bullets. Current: {currentAmmo}, Reserve: {reserveAmmo}");
        return true;
    }

    public void Consume()
    {
        if (currentAmmo > 0)
        {
            currentAmmo--;
            Debug.Log($"[AmmoProvider] Ammo consumed. Remaining: {currentAmmo}");
        }
        else
        {
            Debug.LogWarning("[AmmoProvider] Tried to consume but no ammo left!");
        }
    }

    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
}
