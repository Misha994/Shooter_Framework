using UnityEngine;

[CreateAssetMenu(menuName = "Game/FactionRules")]
public class FactionRules : ScriptableObject
{
    public bool friendlyFireEnabled = false;

    public bool AreHostile(FactionId a, FactionId b)
        => a != b && a != FactionId.Neutral && b != FactionId.Neutral;
}
