using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.UI;
using Zenject;
using Game.Core;

public class InfoPopupView : UIViewBase
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button closeButton;
    [Inject] private IUIService _uiService;
    [Inject] private LocalizationConfig _localization;

    public override UIViewId ViewId => UIViewId.InfoPopup;
    public override UIViewType ViewType => UIViewType.Popup;

    protected override void Awake()
    {
        base.Awake();
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);
    }

    public override void Show(object data = null)
    {
        base.Show(data);
        if (data is string key && messageText != null)
            messageText.text = _localization.GetText(key, "Ukrainian");
    }

    private void OnCloseButtonClicked()
    {
        _uiService.HidePopup();
    }
}
