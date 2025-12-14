public interface IWeapon
{
    bool CanFire { get; }
    float FireRate { get; }             // секунди між викликами Fire() для авто
    WeaponFireMode FireMode { get; }    // Single / Auto / Burst (Burst обробляється всередині Fire())

    WeaponConfig Config { get; }

    void Initialize(WeaponConfig cfg, IAmmoProvider ammo, IFireHandler handler, IWeaponSoundPlayer sound);
    void Fire();    // ОДНОКРАТНА дія: одиночний постріл / залп / черга (залежно від дескриптора)
    void Reload();
}
