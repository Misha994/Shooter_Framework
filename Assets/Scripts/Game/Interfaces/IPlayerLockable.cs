namespace Game.Core.Interfaces
{
    /// <summary>
    /// ќпц≥ональний ≥нтерфейс, €кий дозвол€Ї "заблокувати" / "розблокувати" управл≥нн€ гравц€.
    /// ¬икористовуЇтьс€ VehicleSeatManager дл€ безпечного приховуванн€ гравц€.
    /// –еал≥зац≥€ на гравц≥ повинна в≥дключати/включати компоненти контролю руху.
    /// </summary>
    public interface IPlayerLockable
    {
        /// <summary>ЅлокуЇ або розблоковуЇ управл≥нн€ гравцем.</summary>
        void SetLocked(bool locked);
    }
}
