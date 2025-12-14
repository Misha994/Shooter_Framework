public class SprintHandler
{
    private float _walkSpeed;
    private float _sprintSpeed;

    public SprintHandler(float walkSpeed, float sprintSpeed)
    {
        _walkSpeed = walkSpeed;
        _sprintSpeed = sprintSpeed;
    }

    public float GetSpeed(bool isSprinting)
    {
        return isSprinting ? _sprintSpeed : _walkSpeed;
    }
}
