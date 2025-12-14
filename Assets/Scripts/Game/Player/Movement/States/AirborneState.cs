using UnityEngine;

public class AirborneState : BasePlayerState
{
    public AirborneState(PlayerMovementController movement, IInputService input, PlayerStateMachine stateMachine)
        : base(movement, input, stateMachine) { }

    public override void Update()
    {
        Movement.ApplyGravity();

        Vector2 input = Input.GetMoveAxis();
        Movement.ApplyAirControl(input);

        Movement.MoveCharacter();

        if (Movement.VerticalVelocity <= 0)
        {
            StateMachine.SwitchState(StateMachine.FallingState);
        }
    }
}
