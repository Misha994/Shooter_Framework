using UnityEngine;
using Zenject;
using Game.Core;
using Game.Core.Interfaces;

public class BootstrapInitializer : MonoBehaviour
{
    [Inject] private ISceneRouter _sceneRouter;

    private void Start()
    {
        if (_sceneRouter == null)
        {
            Debug.LogError("SceneRouter is null in BootstrapInitializer.");
            return;
        }

        // Єдиний стартовий перехід у MainMenu
        Debug.Log("[BootstrapInitializer] GoTo(MainMenu)");
        _sceneRouter.GoTo(GameScene.MainMenu);
    }
}
