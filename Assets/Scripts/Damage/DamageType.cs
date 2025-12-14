namespace Combat.Core
{
    // Додані значення, які зустрічаються у твоїх префабах / коді
    public enum DamageType
    {
        Kinetic = 0,
        Explosive = 1,
        Energy = 2,
        Fire = 3,
        Corrosive = 4,
        Electric = 5,
        True = 6,
        Melee = 7,     // використовується у VehicleCannonWeapon в інспекторі
        Laser = 8      // про запас (LaserFireHandler)
    }
}
