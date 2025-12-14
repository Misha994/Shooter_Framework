using Game.Core.Interfaces;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

/// <summary>
/// Installer який вмикається на Player prefab (GameObjectContext). 
/// ВАЖЛИВО: цей Installer має жити саме на префабі гравця (а не в SceneContext / ProjectContext),
/// щоб кожен гравець мав свій scoped binding для IInputService.
/// </summary>
public class PlayerInstaller : MonoInstaller
{
    [Header("Configs")]
    [SerializeField] private GameConfig _gameConfig;

    [System.Serializable]
    public class GrenadeEntry
    {
        public GameObject prefab;
        public int startCount = 3;
    }

    [Header("Grenades (per-player loadout)")]
    [SerializeField] private List<GrenadeEntry> grenadeLoadout = new();

    public override void InstallBindings()
    {
        // -------------------------------
        // INPUT SERVICE (per-player)
        // -------------------------------
        // Рекомендований (простіший) варіант:
        // - зробити PlayerInputReader (PC) та MobileInputService (mobile) як компоненти на префабі гравця
        // - тоді FromComponentInHierarchy() знайде правильний компонент в межах цього префабу (subcontainer)
        //
        // Це гарантує: кожен префаб гравця має свій IInputService інстанс, немає глобальних конфліктів.
#if UNITY_ANDROID || UNITY_IOS
        // Для мобайлу: переконайся, що MobileInputService знаходиться на префабі
        Container.Bind<IInputService>().FromComponentInHierarchy().AsSingle();
#else
        // Для ПК: PlayerInputReader має бути компонентом на префабі
        Container.Bind<IInputService>().FromComponentInHierarchy().AsSingle();
#endif

        // Якщо ти НЕ хочеш робити MobileInputService як компонент (тобто це клас без MonoBehaviour),
        // можна використати FromMethod + Resolve MobileInputView:
        //
        // Container.Bind<MobileInputView>().FromComponentInHierarchy().AsSingle();
        // Container.Bind<IInputService>().FromMethod(ctx => new MobileInputService(ctx.Resolve<MobileInputView>())).AsSingle();

        // -------------------------------
        // Config
        // -------------------------------
        Container.Bind<GameConfig>().FromInstance(_gameConfig).AsSingle();

        // -------------------------------
        // Player components (bound from this prefab's hierarchy)
        // -------------------------------
        Container.Bind<Player>().FromComponentOnRoot().AsSingle();

        Container.Bind<PlayerMovementController>().FromComponentInHierarchy().AsSingle();
        Container.Bind<PlayerStateMachine>().AsSingle();

        Container.Bind<PlayerInput>().FromComponentInHierarchy().AsSingle();
        Container.Bind<MovementComponent>().FromComponentInHierarchy().AsSingle();
        Container.Bind<JumpComponent>().FromComponentInHierarchy().AsSingle();
        Container.Bind<GravityComponent>().FromComponentInHierarchy().AsSingle();
        Container.Bind<AirControlComponent>().FromComponentInHierarchy().AsSingle();

        // === Weapons ===
        Container.Bind<PlayerWeaponInventory>().FromComponentInHierarchy().AsSingle();
        Container.Bind<WeaponController>().FromComponentInHierarchy().AsSingle();
        Container.Bind<IWeaponFactory>().To<WeaponFactory>().AsSingle(); // витягує з ProjectContext звук / інші сервіси


        // === Grenades ===
        var prefabs = new List<GameObject>(grenadeLoadout.Count);
        var counts = new List<int>(grenadeLoadout.Count);
        foreach (var e in grenadeLoadout)
        {
            if (e.prefab == null)
            {
                Debug.LogError("[PlayerInstaller] Grenade prefab is null in loadout, skipping.");
                continue;
            }
            prefabs.Add(e.prefab);
            counts.Add(Mathf.Max(0, e.startCount));
        }
        Container.Bind<GrenadeInventory>().AsSingle().WithArguments(prefabs, counts, 0);
        Container.Bind<PlayerGrenadeHandler>().FromComponentInHierarchy().AsSingle();

        // === Movement helpers ===
        Container.Bind<JumpHandler>().AsSingle().WithArguments(_gameConfig.JumpForce, 0.2f, 0.2f);
        Container.Bind<SprintHandler>().AsSingle().WithArguments(_gameConfig.MoveSpeed, _gameConfig.SprintSpeed);

        Container.Bind<ICharacterAnimator>().To<PlayerAnimController>().FromComponentInHierarchy().AsSingle();

        // === ADS / Aiming ===
        Container.DeclareSignal<AimStateChangedSignal>();
        Container.Bind<WeaponAimer>().FromComponentInHierarchy().AsTransient();
    }
}
