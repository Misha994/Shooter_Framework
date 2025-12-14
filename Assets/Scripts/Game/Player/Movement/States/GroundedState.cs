using UnityEngine;

public class GroundedState : BasePlayerState
{
    public GroundedState(PlayerMovementController movement, IInputService input, PlayerStateMachine stateMachine)
        : base(movement, input, stateMachine)
    { }

    public override void Update()
    {
        Vector2 move = Input.GetMoveAxis();
        bool isSprinting = Input.IsSprintHeld();

        Movement.Move(move, isSprinting);
        Movement.ApplyGravity();
        Movement.MoveCharacter();

        if (Input.IsJumpPressed())
        {
            //Movement.ApplyJumpMomentum(move, isSprinting); // Apply momentum for jump
            Movement.Jump();
            StateMachine.SwitchState(StateMachine.JumpingState);
        }
        else if (!Movement.IsGrounded)
        {
            StateMachine.SwitchState(StateMachine.FallingState);
        }
    }
}
