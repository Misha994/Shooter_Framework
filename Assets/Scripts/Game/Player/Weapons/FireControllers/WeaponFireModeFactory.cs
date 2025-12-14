public static class WeaponFireModeFactory
{
    public static IWeaponFireController Create(WeaponFireMode mode, IWeapon weapon)
    {
        return mode switch
        {
            WeaponFireMode.Single => new SingleFireController(weapon),
            WeaponFireMode.Auto => new AutoFireController(weapon),
            WeaponFireMode.Burst => new SingleFireController(weapon), // Burst виконується всередині WeaponMono.Fire() як серія пострілів
            _ => new SingleFireController(weapon),
        };
    }
}
