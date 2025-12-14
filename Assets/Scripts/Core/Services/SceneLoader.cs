using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;
using Game.Core.Interfaces;

public class SceneLoader : ISceneLoader
{
    private readonly SignalBus _signalBus;

    [Inject]
    public SceneLoader(SignalBus signalBus)
    {
        _signalBus = signalBus;
    }

    public void LoadScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] sceneName is null or empty.");
            return;
        }

        // Правильна перевірка на наявність сцени в Build Settings
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneLoader] Scene '{sceneName}' is not in Build Settings.");
            return;
        }

        Debug.Log($"[SceneLoader] Loading scene '{sceneName}'...");

        // Гарантовано шлемо сигнал ПІСЛЯ фактичного завантаження
        SceneManager.sceneLoaded += OnSceneLoadedInternal;
        SceneManager.LoadScene(sceneName);
    }

    private void OnSceneLoadedInternal(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoadedInternal;
        Debug.Log($"[SceneLoader] Scene loaded: {scene.name} (mode: {mode})");

        // Може бути null лише якщо DI зламаний, але підстрахуємось
        _signalBus?.Fire(new SceneLoadedSignal(scene.name));
    }
}
