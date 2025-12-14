public class SceneReloadRequestedSignal
{
    public GameScene TargetScene { get; }

    public SceneReloadRequestedSignal(GameScene targetScene)
    {
        TargetScene = targetScene;
    }
}