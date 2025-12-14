using UnityEngine;
using Zenject;

public class GlobalAudioListenerInstaller : MonoInstaller
{
    [SerializeField] private GameObject audioListenerPrefab;

    public override void InstallBindings()
    {
        if (GlobalAudioListener.Exists)
        {
            Debug.Log("[GlobalAudioListenerInstaller] Already present, skipping instantiation.");
            return;
        }

        if (audioListenerPrefab == null)
        {
            Debug.LogError("AudioListenerPrefab is not assigned in GlobalAudioListenerInstaller.");
            return;
        }

        var instance = Container.InstantiatePrefab(audioListenerPrefab);
        instance.name = "GlobalAudioListener";
    }
}
