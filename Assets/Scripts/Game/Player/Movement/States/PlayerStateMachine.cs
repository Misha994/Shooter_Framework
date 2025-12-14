public class PlayerStateMachine
{
    public BasePlayerState GroundedState { get; private set; }
    public BasePlayerState JumpingState { get; private set; }
    public BasePlayerState FallingState { get; private set; }
    public BasePlayerState AirborneState { get; private set; }

    private BasePlayerState _currentState;

    public PlayerStateMachine(PlayerMovementController movement, IInputService input)
    {
        GroundedState = new GroundedState(movement, input, this);
        JumpingState = new JumpingState(movement, input, this);
        FallingState = new FallingState(movement, input, this);
        AirborneState = new AirborneState(movement, input, this);

        _currentState = GroundedState;
    }

    public void SwitchState(BasePlayerState newState)
    {
        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }

    public void Update()
    {
        _currentState?.Update();
    }
}
