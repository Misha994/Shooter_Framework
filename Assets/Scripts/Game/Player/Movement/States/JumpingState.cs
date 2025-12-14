using UnityEngine;

public class JumpingState : BasePlayerState
{
    public JumpingState(PlayerMovementController movement, IInputService input, PlayerStateMachine stateMachine)
        : base(movement, input, stateMachine)
    { }

    public override void Update()
    {
        Movement.ApplyGravity();

        Vector2 input = Input.GetMoveAxis();
        Movement.ApplyAirControl(input); // Allow air control

        Movement.MoveCharacter();

        if (Movement.VerticalVelocity <= 0)
        {
            StateMachine.SwitchState(StateMachine.FallingState);
        }
    }
}
