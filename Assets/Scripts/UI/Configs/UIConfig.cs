using UnityEngine;
using Game.UI;

[CreateAssetMenu(fileName = "UIConfig", menuName = "Configs/UIConfig")]
public class UIConfig : ScriptableObject
{
    [System.Serializable]
    public struct ViewConfig
    {
        public UIViewId ViewId;
        public UIViewType ViewType;
        public GameObject Prefab;
    }

    [SerializeField] private ViewConfig[] views;
    public ViewConfig[] Views => views;
}