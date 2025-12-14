using Game.Core;
using Game.Core.Interfaces;
using UnityEngine;
using Zenject;

public class SceneRouter : ISceneRouter
{
    private readonly ISceneLoader _sceneLoader;

    [Inject]
    public SceneRouter(ISceneLoader sceneLoader)
    {
        _sceneLoader = sceneLoader;
        if (_sceneLoader == null)
        {
            Debug.LogError("SceneLoader is null in SceneRouter.");
        }
    }

    public void GoTo(GameScene scene)
    {
        Debug.Log($"SceneRouter: Navigating to {scene}");
        switch (scene)
        {
            case GameScene.MainMenu:
                _sceneLoader.LoadScene("MainMenu");
                break;
            case GameScene.Game:
                _sceneLoader.LoadScene("Game");
                break;
            default:
                Debug.LogError($"Unknown scene: {scene}");
                break;
        }
    }
}
