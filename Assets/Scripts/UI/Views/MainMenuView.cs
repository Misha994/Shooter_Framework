using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Game.UI;
using Zenject;
using Game.Core;

public class MainMenuView : UIViewBase
{
    [Header("UI")]
    [SerializeField] private Button playButton;
    [SerializeField] private TMP_Text titleText;      // ← TMP варіант
    [SerializeField] private TMP_Text playLabelText;  // ← якщо хочеш локалізувати текст кнопки

    [Inject] private SignalBus _signalBus;
    [Inject] private LocalizationConfig _localization;

    public override UIViewId ViewId => UIViewId.MainMenu;
    public override UIViewType ViewType => UIViewType.Window;

    protected override void Awake()
    {
        base.Awake();

        if (playButton != null)
            playButton.onClick.AddListener(OnPlayButtonClicked);
        else
            Debug.LogError("[MainMenuView] playButton is not assigned");
    }

    public override void Show(object data = null)
    {
        base.Show(data);

        // Якщо прийшов ключ, локалізуємо заголовок
        if (data is string key && titleText != null)
            titleText.text = _localization.GetText(key, "Ukrainian");

        // Якщо треба — локалізуй підпис кнопки (вкажи свій ключ)
        if (playLabelText != null)
            playLabelText.text = _localization.GetText("PlayButton", "Ukrainian");
    }

    private void OnPlayButtonClicked()
    {
        _signalBus.Fire<GameStartedSignal>();
        //_signalBus.Fire(new AudioPlaySignal(AudioClipId.MainTheme, AudioChannelType.Music, true));
    }
}
