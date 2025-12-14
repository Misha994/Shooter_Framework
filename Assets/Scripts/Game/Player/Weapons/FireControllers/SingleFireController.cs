public class SingleFireController : IWeaponFireController
{
    private readonly IWeapon weapon;

    public SingleFireController(IWeapon weapon)
    {
        this.weapon = weapon;
    }

    public void OnFirePressed()
    {
        if (weapon != null && weapon.CanFire)
            weapon.Fire();
    }

    public void OnFireReleased() { }

    // ⬇⬇⬇ ВАЖЛИВО: реалізуємо reload
    public void OnReloadPressed()
    {
        if (weapon != null)
            weapon.Reload();
    }

    public void Update() { }
}
