using UnityEngine;
using Zenject;
using UnityEngine.InputSystem;
using Game.Core.Interfaces;

public class GameSceneInstaller : MonoInstaller
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform playerSpawnPoint;

    public override void InstallBindings()
    {
        Container.Bind<Player>().FromSubContainerResolve()
            .ByNewContextPrefab(playerPrefab)
            .UnderTransform(playerSpawnPoint)
            .AsSingle()
            .NonLazy();

        Container.Bind<ICameraTargetProvider>()
    .FromResolveGetter<Player>(p => p.GetComponentInChildren<PlayerCameraTarget>(true))
    .AsSingle();
    }
}
