public interface IAmmoProvider
{
    bool CanShoot { get; }
    bool TryReload();
    void Consume();
    int CurrentAmmo { get; }
}