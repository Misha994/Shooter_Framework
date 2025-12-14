using UnityEngine;
using Zenject;

public class AudioInstaller : MonoInstaller
{
    [SerializeField] private SFXAudioPool sfxAudioPoolPrefab;

    public override void InstallBindings()
    {
        if (sfxAudioPoolPrefab == null)
        {
            Debug.LogError("[AudioInstaller] sfxAudioPoolPrefab is not assigned.");
            return;
        }

        // Інстансимо пул і зберігаємо як синглтон
        var pool = Container.InstantiatePrefabForComponent<SFXAudioPool>(sfxAudioPoolPrefab);
        Object.DontDestroyOnLoad(pool.gameObject);
        Container.Bind<SFXAudioPool>().FromInstance(pool).AsSingle();

        // Сервіс
        Container.BindInterfacesAndSelfTo<AudioService>().AsSingle();

        // Сигнали (єдина точка декларації АУДІО-сигналів)
        Container.DeclareSignal<AudioPlaySignal>();
        Container.DeclareSignal<AudioPlay3DSignal>();
        Container.DeclareSignal<AudioFadeSignal>();
    }
}
