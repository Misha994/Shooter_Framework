public abstract class BasePlayerState : IPlayerState
{
    protected readonly PlayerMovementController Movement;
    protected readonly IInputService Input;
    protected readonly PlayerStateMachine StateMachine;

    protected BasePlayerState(PlayerMovementController movement, IInputService input, PlayerStateMachine stateMachine)
    {
        Movement = movement;
        Input = input;
        StateMachine = stateMachine;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public abstract void Update();
}
