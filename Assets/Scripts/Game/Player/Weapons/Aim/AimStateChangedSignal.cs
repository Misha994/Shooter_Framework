// Signals/AimStateChangedSignal.cs
public struct AimStateChangedSignal
{
    public bool IsAiming { get; }
    public AimStateChangedSignal(bool isAiming) => IsAiming = isAiming;
}
