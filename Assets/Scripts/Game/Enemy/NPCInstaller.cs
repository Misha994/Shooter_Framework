// Assets/Scripts/Game/Enemy/NPCInstaller.cs
using Fogoyote.BFLike.Combat.Weapons.Factory;
using UnityEngine;
using Zenject;

[DisallowMultipleComponent]
public class NPCInstaller : MonoInstaller
{
    [SerializeField] private GameConfig gameConfig;
    [SerializeField] private WeaponCatalog weaponCatalog;
    [SerializeField] private AIInputService aiInputService;

    public override void InstallBindings()
    {
        if (gameConfig != null)
            Container.Bind<GameConfig>().FromInstance(gameConfig).IfNotBound();

        if (weaponCatalog != null)
            Container.Bind<WeaponCatalog>().FromInstance(weaponCatalog).IfNotBound();

        if (aiInputService != null)
            Container.BindInterfacesAndSelfTo<AIInputService>().FromInstance(aiInputService).AsSingle();

        // Локальні компоненти
        Container.BindInterfacesAndSelfTo<NPCWeaponInventory>().FromComponentInHierarchy().AsSingle();
        Container.BindInterfacesAndSelfTo<WeaponController>().FromComponentInHierarchy().AsSingle();

        // НЕ намагаємось шукати фабрику/аудіо в ієрархії,
        // якщо в тебе є глобальні сервіси — вони мають биндитись у ProjectContext/SceneContext.
        // Якщо ж потрібно локально, розкоментуй і додай відповідні компонент-реалізації на Enemy_New:
        // Container.BindInterfacesTo<WeaponFactoryMono>().FromComponentInHierarchy().AsSingle().IfNotBound();
        // Container.BindInterfacesTo<AudioServiceMono>().FromComponentInHierarchy().AsSingle().IfNotBound();
    }

    public override void Start()
    {
        // Видати дефолтну зброю, якщо інвентар порожній
        var inv = Container.Resolve<NPCWeaponInventory>();
        if (inv != null && inv.Count == 0 && weaponCatalog != null)
        {
            var cfg = weaponCatalog.GetDefault();
            if (cfg != null)
            {
                inv.AddOrReplaceCurrent(cfg);
            }
            else
            {
                Debug.LogWarning($"[NPCInstaller] WeaponCatalog has no default on {name}");
            }
        }

        // Увімкнути автофайр для AI
        //var wc = Container.Resolve<WeaponController>();
        //if (wc != null)
        //    wc.EnableAutoFireForAI(true);
    }
}
