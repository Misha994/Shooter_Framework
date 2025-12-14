using UnityEngine;
using TMPro;
using Game.UI;

public class GameUIView : UIViewBase
{
    [SerializeField] private TMP_Text healthText;

    public override UIViewId ViewId => UIViewId.GameUI;
    public override UIViewType ViewType => UIViewType.Window;

    public void UpdateHealth(int health)
    {
        if (healthText != null)
        {
            healthText.text = $"Health: {health}";
        }
        else
        {
            Debug.LogError("[GameUIView] healthText is not assigned.");
        }
    }
}
