using UnityEngine;

public class FallingState : BasePlayerState
{
    public FallingState(PlayerMovementController movement, IInputService input, PlayerStateMachine stateMachine)
        : base(movement, input, stateMachine)
    { }

    public override void Update()
    {
        Vector2 move = Input.GetMoveAxis();
        bool isSprinting = Input.IsSprintHeld();

        Movement.Move(move, isSprinting);
        Movement.ApplyGravity();
        Movement.MoveCharacter();

        if (Movement.IsGrounded)
        {
            StateMachine.SwitchState(StateMachine.GroundedState);
        }
    }
}
