using UnityEngine;
using Zenject;
using System.Collections.Generic;

/// <summary>
/// Інвентар зброї гравця:
/// - створення/підбір, перемикання, монтування у WeaponMount
/// - склеювання з IK, FOV/ADS, Recoil, Animator Override, RigWeightBlender
/// - безпечний унсабскрайб/сабскрайб подій зброї
/// Нічого не ламає, якщо якісь залежності не призначені (null-safe).
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(WeaponController))]
public class PlayerWeaponInventory : MonoBehaviour, IWeaponPickupHandler, IHumanoidWeaponInventory
{
    [Header("Setup")]
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private int maxWeapons = 2;

    [Header("Glue (автозв'язуються в Awake, але можна задати в інспекторі)")]
    [SerializeField] private WeaponMount _mount;
    [SerializeField] private WeaponIKLinker _ik;
    [SerializeField] private CameraFOVAimControllerCinemachine _fovCm;
    [SerializeField] private RecoilKick _recoil;
    [SerializeField] private Animator _anim;
    [SerializeField] private RigWeightBlender _rigBlend;

    // Runtime
    private readonly List<WeaponBase> _weapons = new();
    private int _currentIndex = 0;

    private AnimatorOverrideController _aoc;
    private WeaponController _weaponController;
    private IWeaponFactory _weaponFactory;
    private IAudioService _audioService;

    public event System.Action<WeaponBase, WeaponBase> WeaponChanged;

    // ===== Zenject =====
    [Inject]
    public void Construct(IWeaponFactory weaponFactory, IAudioService audioService)
    {
        _weaponFactory = weaponFactory;
        _audioService = audioService;
    }

    // ===== Unity =====
    private void Awake()
    {
        _weaponController ??= GetComponent<WeaponController>();

        // weaponHolder автопошук/створення
        if (!weaponHolder)
        {
            var holder = transform.Find("WeaponHolder ");
            if (holder) weaponHolder = holder;
            else
            {
                var go = new GameObject("WeaponHolder ");
                go.transform.SetParent(transform, false);
                weaponHolder = go.transform;
            }
        }

        _mount ??= weaponHolder.GetComponent<WeaponMount>();
        if (!_mount) _mount = weaponHolder.gameObject.AddComponent<WeaponMount>();

        _ik ??= GetComponentInParent<WeaponIKLinker>(true);
        _fovCm ??= GetComponentInParent<CameraFOVAimControllerCinemachine>(true);
        _recoil ??= GetComponentInChildren<RecoilKick>(true);
        _rigBlend ??= GetComponentInChildren<RigWeightBlender>(true);
        _anim ??= GetComponentInChildren<Animator>(true);

        // Готуємо AOC один раз, але тільки якщо є Animator
        if (_anim && _anim.runtimeAnimatorController && _aoc == null)
        {
            _aoc = new AnimatorOverrideController(_anim.runtimeAnimatorController);
            _anim.runtimeAnimatorController = _aoc;
        }
    }

    private void OnDestroy()
    {
        // Зняти підписки з поточної зброї (і на всяк випадок — з усіх, що лишилися)
        for (int i = 0; i < _weapons.Count; i++)
            UnsubscribeWeaponEvents(_weapons[i]);
    }

    // ===== Публічні властивості/API =====
    public int Count => _weapons.Count;
    public int CurrentIndex => _currentIndex;
    public WeaponBase Current => GetCurrentWeapon();

    public WeaponBase GetCurrentWeapon() => _weapons.Count > 0 ? _weapons[_currentIndex] : null;

    /// <summary> Перемкнути зброю за індексом. </summary>
    public void SwitchWeapon(int index)
    {
        if (index < 0 || index >= _weapons.Count || index == _currentIndex) return;

        var prev = GetCurrentWeapon();
        if (prev != null) { prev.CancelFiringState(); UnsubscribeWeaponEvents(prev); }

        _currentIndex = index;
        ApplyActiveWeapon();

        var next = GetCurrentWeapon();
        AfterSwitch(prev, next); // підписки, IK, FOV, анімації, сигнали
        _weaponController?.RefreshCurrentWeapon();
    }

    /// <summary> Зручність: наступна зброя. </summary>
    public void SwitchNext()
    {
        if (_weapons.Count <= 1) return;
        int next = (_currentIndex + 1) % _weapons.Count;
        SwitchWeapon(next);
    }

    /// <summary> Зручність: попередня зброя. </summary>
    public void SwitchPrev()
    {
        if (_weapons.Count <= 1) return;
        int next = (_currentIndex - 1 + _weapons.Count) % _weapons.Count;
        SwitchWeapon(next);
    }

    // ===== Підбір/створення =====
    public void TryPickupWeapon(WeaponPickup pickup)
    {
        if (pickup == null || pickup.WeaponConfig == null || _weaponFactory == null) return;

        var config = pickup.WeaponConfig;
        AddOrReplaceCurrent(config);
        Destroy(pickup.gameObject);
    }

    /// <summary>
    /// Додати зброю з конфіга. Якщо слотів повно — замінює поточну.
    /// Викликає повний AfterSwitch/Refresh.
    /// </summary>
    public void AddOrReplaceCurrent(WeaponConfig config)
    {
        if (config == null || _weaponFactory == null) return;

        var prev = GetCurrentWeapon();

        if (_weapons.Count >= maxWeapons)
        {
            // Замінюємо поточну
            if (prev != null)
            {
                prev.CancelFiringState();
                UnsubscribeWeaponEvents(prev);
                Destroy(prev.gameObject);
            }

            var created = _weaponFactory.Create(config, weaponHolder);
            _weapons[_currentIndex] = created;
        }
        else
        {
            var created = _weaponFactory.Create(config, weaponHolder);
            _weapons.Add(created);
            _currentIndex = _weapons.Count - 1;
        }

        ApplyActiveWeapon();

        var next = GetCurrentWeapon();
        AfterSwitch(prev, next);
        _weaponController?.RefreshCurrentWeapon();
    }

    // ===== Внутрішня логіка монтування/склеювання =====
    /// <summary>
    /// Активує поточну зброю, решту деактивує. Монтує у WeaponMount.
    /// Нічого не чіпає, якщо список порожній.
    /// </summary>
    private void ApplyActiveWeapon()
    {
        // Деактивуємо всі (залишаємо їх під holder — Mount сам перепризначить)
        for (int i = 0; i < _weapons.Count; i++)
        {
            var w = _weapons[i];
            if (!w) continue;
            w.gameObject.SetActive(false);
            if (w.transform.parent != weaponHolder)
                w.transform.SetParent(weaponHolder, false);
        }

        var current = GetCurrentWeapon();
        _mount?.Mount(current);             // всередині — перевішування під сокет
        if (current) current.gameObject.SetActive(true);
    }

    /// <summary>
    /// Після зміни зброї: IK, FOV/ADS, Recoil підписки, Animator прапори/оверрайди, RigBlend, евенти.
    /// </summary>
    private void AfterSwitch(WeaponBase prev, WeaponBase next)
    {
        // IK рук → до таргетів зброї (підтримка різних сигнатур через SendMessage)
        if (_ik)
        {
            if (next != null)
            {
                // Прямий твій API
                _ik.SendMessage("LinkTo", next, SendMessageOptions.DontRequireReceiver);

                // Фолбек, якщо у твого Linker’а інший API
                var points = next.GetComponent<WeaponIKPoints>();
                if (points) _ik.SendMessage("Link", points, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                _ik.SendMessage("Unlink", SendMessageOptions.DontRequireReceiver);
            }
        }

        // FOV/ADS + таймінг WeaponAimer з конфіга
        if (next != null)
        {
            var cfg = next.Config;
            _fovCm?.SetupFromConfig(cfg);
            next.GetComponent<WeaponAimer>()?.SetupFromConfig(cfg);
        }

        // Recoil: перепідписатися
        if (_recoil != null)
        {
            if (prev != null) prev.Fired -= _recoil.OnFired;
            if (next != null) next.Fired += _recoil.OnFired;
        }

        // Animator: прапори
        if (_anim != null)
        {
            if (HasBool(_anim, "HasWeapon"))
                _anim.SetBool("HasWeapon", next != null);

            // Якщо поруч є AnimLayerAimer — повідомимо його (не обов'язково)
            var aimer = GetComponentInParent<AnimLayerAimer>();
            if (aimer) aimer.SetHasWeapon(next != null);
        }

        // Ріґ-блендер: знає, чи є зброя
        _rigBlend?.SetHasWeapon(next != null);

        // Підміна кліпів (override)
        TryApplyAnimationSet(next ? next.Config : null);

        // HUD/інші підписники
        WeaponChanged?.Invoke(prev, next);
    }

    private void TryApplyAnimationSet(WeaponConfig cfg)
    {
        if (!_anim) return;

        // Створимо AOC, якщо його ще немає (можливо Animator призначили пізніше)
        if (_aoc == null && _anim.runtimeAnimatorController)
        {
            _aoc = new AnimatorOverrideController(_anim.runtimeAnimatorController);
            _anim.runtimeAnimatorController = _aoc;
        }

        if (_aoc == null) return;
        if (cfg == null || cfg.animSet == null) { /* немає сету — лишаємо базові */ return; }

        var set = cfg.animSet;
        var map = new (string placeholder, AnimationClip clip)[]
        {
            ("PL_IDLE",           set.idle),
            ("PL_WALK_FWD",       set.walkFwd),
            ("PL_WALK_BACK",      set.walkBack),
            ("PL_STRAFE_L",       set.strafeL),
            ("PL_STRAFE_R",       set.strafeR),
            ("PL_RUN_FWD",        set.runFwd),
            ("PL_RUN_BACK",       set.runBack),
            ("PL_RUN_STRAFE_L",   set.runStrafeL),
            ("PL_RUN_STRAFE_R",   set.runStrafeR),
            ("PL_RELOAD",         set.reload),
            ("PL_ADS_POSE",       set.adsPoseLoop),
            ("PL_FIRE_ADD",       set.fireAdditive),
            ("PL_HANDPOSE_ADD",   set.handPoseAdditive),
        };

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(_aoc.overridesCount);
        _aoc.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            var orig = overrides[i].Key;
            if (!orig) continue;

            for (int j = 0; j < map.Length; j++)
            {
                if (orig.name == map[j].placeholder)
                {
                    var replacement = map[j].clip ? map[j].clip : orig;
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, replacement);
                    break;
                }
            }
        }

        _aoc.ApplyOverrides(overrides);
    }

    // ===== Допоміжні =====
    private void UnsubscribeWeaponEvents(WeaponBase w)
    {
        if (!w) return;
        if (_recoil != null) w.Fired -= _recoil.OnFired;
        // Тут можна розширити: ReloadStarted/Ended → _rigBlend.SetReloading(...)
    }

    private static bool HasBool(Animator a, string name)
    {
        if (!a) return false;
        foreach (var p in a.parameters)
            if (p.type == AnimatorControllerParameterType.Bool && p.name == name) return true;
        return false;
    }
}
