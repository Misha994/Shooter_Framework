public struct PlayerHealthChangedSignal
{
    public int Health { get; private set; }

    public PlayerHealthChangedSignal(int health)
    {
        Health = health;
    }
}