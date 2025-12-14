using Zenject;
using UnityEngine;
using Game.Core;
using Game.UI;
using UnityEngine.UI;
using Game.Core.Interfaces;

public class ProjectContextInstaller : MonoInstaller
{
    [Header("Configs")]
    [SerializeField] private GameConfig gameConfig;
    [SerializeField] private UIConfig uiConfig;
    [SerializeField] private LocalizationConfig localizationConfig;
    [SerializeField] private AudioConfig audioConfig;

    [Header("UI Roots (optional prefabs)")]
    [SerializeField] private GameObject windowsRootPrefab;
    [SerializeField] private GameObject modalsRootPrefab;
    [SerializeField] private GameObject popupsRootPrefab;

    [Header("Audio")]
    [SerializeField] private SFXAudioPool sfxAudioPoolPrefab;

    public override void InstallBindings()
    {
        // ===== Validation =====
        if (gameConfig == null) Debug.LogError("[ProjectContextInstaller] GameConfig is not assigned.");
        if (uiConfig == null) Debug.LogError("[ProjectContextInstaller] UIConfig is not assigned.");
        if (localizationConfig == null) Debug.LogError("[ProjectContextInstaller] LocalizationConfig is not assigned.");
        if (audioConfig == null) Debug.LogError("[ProjectContextInstaller] AudioConfig is not assigned.");
        if (windowsRootPrefab == null) Debug.LogWarning("[ProjectContextInstaller] WindowsRootPrefab is not assigned, creating default.");
        if (modalsRootPrefab == null) Debug.LogWarning("[ProjectContextInstaller] ModalsRootPrefab is not assigned, creating default.");
        if (popupsRootPrefab == null) Debug.LogWarning("[ProjectContextInstaller] PopupsRootPrefab is not assigned, creating default.");
        if (sfxAudioPoolPrefab == null) Debug.LogError("[ProjectContextInstaller] sfxAudioPoolPrefab is not assigned.");

        // ===== UI Roots =====
        Transform windowsRoot = CreateOrInstantiateRoot(windowsRootPrefab, "WindowsRoot", 0);
        Transform modalsRoot = CreateOrInstantiateRoot(modalsRootPrefab, "ModalsRoot", 10);
        Transform popupsRoot = CreateOrInstantiateRoot(popupsRootPrefab, "PopupsRoot", 20);

        Object.DontDestroyOnLoad(windowsRoot.gameObject);
        Object.DontDestroyOnLoad(modalsRoot.gameObject);
        Object.DontDestroyOnLoad(popupsRoot.gameObject);

        Container.Bind<Transform>().WithId("WindowsRoot").FromInstance(windowsRoot).AsCached();
        Container.Bind<Transform>().WithId("ModalsRoot").FromInstance(modalsRoot).AsCached();
        Container.Bind<Transform>().WithId("PopupsRoot").FromInstance(popupsRoot).AsCached();

        // ===== Scene services =====
        Container.BindInterfacesAndSelfTo<SceneRouter>().AsSingle();
        Container.BindInterfacesAndSelfTo<SceneLoader>().AsSingle();

        // ===== Configs (data) =====
        Container.Bind<GameConfig>().FromInstance(gameConfig).AsSingle();
        Container.Bind<UIConfig>().FromInstance(uiConfig).AsSingle();
        Container.Bind<LocalizationConfig>().FromInstance(localizationConfig).AsSingle();
        Container.Bind<AudioConfig>().FromInstance(audioConfig).AsSingle();

        // ===== Signals =====
        SignalBusInstaller.Install(Container);
        Container.DeclareSignal<GameStartedSignal>();
        Container.DeclareSignal<PlayerDiedSignal>();
        Container.DeclareSignal<PlayerHealthChangedSignal>();
        Container.DeclareSignal<SceneLoadedSignal>().OptionalSubscriber();
        Container.DeclareSignal<SceneReloadRequestedSignal>();
        Container.DeclareSignal<AimStateChangedSignal>();
        // Аудіо-сигнали тут же — щоб не було рознесено по сценах
        Container.DeclareSignal<AudioPlaySignal>();
        Container.DeclareSignal<AudioPlay3DSignal>();
        Container.DeclareSignal<AudioFadeSignal>();

        // ===== Game/UI services =====
        Container.BindInterfacesAndSelfTo<GameService>().AsSingle();
        Container.BindInterfacesAndSelfTo<UIFactory>().AsSingle();
        Container.BindInterfacesAndSelfTo<UIService>().AsSingle().NonLazy();

        // ===== Audio (єдина точка правди) =====
        // Інстансимо пул і біндимо як синглтон у root-контейнері
        var pool = Container.InstantiatePrefabForComponent<SFXAudioPool>(sfxAudioPoolPrefab);
        Object.DontDestroyOnLoad(pool.gameObject);
        Container.Bind<SFXAudioPool>().FromInstance(pool).AsSingle();
        // Сервіс + IAudioService
        Container.BindInterfacesAndSelfTo<AudioService>().AsSingle();

        // ===== Bootstrap =====
        Container.Bind<BootstrapInitializer>()
                 .FromNewComponentOnNewGameObject()
                 .WithGameObjectName("BootstrapInitializer")
                 .AsSingle().NonLazy();

        // ===== DOTween init =====
        DG.Tweening.DOTween.Init();

        Container.Bind<IWeaponFactory>().To<WeaponFactory>().AsSingle().IfNotBound();

    }

    private Transform CreateOrInstantiateRoot(GameObject prefab, string name, int sortingOrder)
    {
        if (prefab == null)
        {
            var go = new GameObject(name);
            var canvas = go.AddComponent<Canvas>();
            var scaler = go.AddComponent<CanvasScaler>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            scaler.referenceResolution = new Vector2(1920, 1080);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            return go.transform;
        }
        else
        {
            var t = Object.Instantiate(prefab).transform;
            t.name = name;
            return t;
        }
    }
}
