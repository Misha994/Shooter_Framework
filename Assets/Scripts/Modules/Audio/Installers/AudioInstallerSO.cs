using UnityEngine;
using Zenject;

[CreateAssetMenu(menuName = "Installers/AudioInstallerSO")]
public class AudioInstallerSO : ScriptableObjectInstaller<AudioInstallerSO>
{
    public SFXAudioPool sfxAudioPoolPrefab;

    public override void InstallBindings()
    {
        if (sfxAudioPoolPrefab == null)
        {
            Debug.LogError("[AudioInstallerSO] sfxAudioPoolPrefab is not assigned.");
            return;
        }

        var pool = Container.InstantiatePrefabForComponent<SFXAudioPool>(sfxAudioPoolPrefab);
        Object.DontDestroyOnLoad(pool.gameObject);
        Container.Bind<SFXAudioPool>().FromInstance(pool).AsSingle();

        Container.BindInterfacesAndSelfTo<AudioService>().AsSingle();

        Container.DeclareSignal<AudioPlaySignal>();
        Container.DeclareSignal<AudioPlay3DSignal>();
        Container.DeclareSignal<AudioFadeSignal>();
    }
}
