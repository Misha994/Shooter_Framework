// IDriveController.cs
public interface IDriveController
{
    void SetSeatInput(IInputService input);
    void SetDisabled(bool disabled);
    void KillDrive();
    bool IsActive { get; }
}
