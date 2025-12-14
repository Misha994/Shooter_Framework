using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.UI;
using Zenject;
using Game.Core;

public class ModalWindowView : UIViewBase
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button restartButton;
    [Inject] private SignalBus _signalBus;
    [Inject] private LocalizationConfig _localization;

	[Inject] private IUIService _uiService;
	public override UIViewId ViewId => UIViewId.GameOverModal;
    public override UIViewType ViewType => UIViewType.Modal;

    protected override void Awake()
    {
        base.Awake();
        if (closeButton != null)
            closeButton.onClick.AddListener(OnCloseButtonClicked);

        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestartClicked);
    }

    public override void Show(object data = null)
    {
        base.Show(data);
        if (data is string key && messageText != null)
            messageText.text = _localization.GetText(key, "Ukrainian");
    }

	private void OnCloseButtonClicked()
	{
		_uiService?.HideModal();
	}


	private void OnRestartClicked()
	{
		_signalBus.Fire(new SceneReloadRequestedSignal(GameScene.MainMenu));
	}
}
