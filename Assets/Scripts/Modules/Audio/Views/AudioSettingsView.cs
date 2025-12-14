using UnityEngine;
using UnityEngine.UI;
using Zenject;
using Game.UI;

public class AudioSettingsView : UIViewBase
{
    [Inject] private IAudioService _audioService;

    [SerializeField] private Slider musicSlider;
    [SerializeField] private Slider sfxSlider;

    public override UIViewId ViewId => UIViewId.AudioSettings;
    public override UIViewType ViewType => UIViewType.Modal;

    protected override void Awake()
    {
        base.Awake();

        if (musicSlider == null)
            Debug.LogError("[AudioSettingsView] musicSlider is not assigned.");
        if (sfxSlider == null)
            Debug.LogError("[AudioSettingsView] sfxSlider is not assigned.");

        // Підписуємось тільки якщо все є
        if (musicSlider != null)
            musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
    }

    private void OnDestroy()
    {
        if (musicSlider != null)
            musicSlider.onValueChanged.RemoveListener(OnMusicChanged);
        if (sfxSlider != null)
            sfxSlider.onValueChanged.RemoveListener(OnSfxChanged);
    }

    public override void Show(object data = null)
    {
        base.Show(data);

        if (_audioService == null)
        {
            Debug.LogError("[AudioSettingsView] IAudioService is not bound.");
            return;
        }

        if (musicSlider != null)
            musicSlider.value = _audioService.GetVolume(AudioChannelType.Music);

        if (sfxSlider != null)
            sfxSlider.value = _audioService.GetVolume(AudioChannelType.SFX);
    }

    private void OnMusicChanged(float value)
    {
        if (_audioService == null) return;
        _audioService.SetVolume(AudioChannelType.Music, value);
    }

    private void OnSfxChanged(float value)
    {
        if (_audioService == null) return;
        _audioService.SetVolume(AudioChannelType.SFX, value);
    }
}
