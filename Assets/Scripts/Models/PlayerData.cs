using UnityEngine;

public class PlayerData
{
    public int Health { get; private set; }
    public int MaxHealth { get; private set; }

    public PlayerData(GameConfig config)
    {
        MaxHealth = Mathf.Max(1, config.MaxHealth);
        Health = MaxHealth;
    }

    public void TakeDamage(int damage)
    {
        Health = Mathf.Max(0, Health - Mathf.Max(0, damage));
    }

    public void Heal(int amount)
    {
        Health = Mathf.Min(MaxHealth, Health + Mathf.Max(0, amount));
    }
}
