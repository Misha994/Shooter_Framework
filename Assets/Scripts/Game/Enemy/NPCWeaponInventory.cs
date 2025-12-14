using UnityEngine;
using Zenject;
using System.Collections.Generic;

[DisallowMultipleComponent]
[RequireComponent(typeof(WeaponController))]
public class NPCWeaponInventory : MonoBehaviour, IHumanoidWeaponInventory, IWeaponInventoryEvents, IWeaponPickupHandler
{
    [Header("Setup")]
    [SerializeField] private Transform weaponHolder;
    [SerializeField] private int maxWeapons = 2;

    [Header("Loadout")]
    [SerializeField] private List<WeaponConfig> initialLoadout = new();
    [SerializeField] private bool spawnOnStart = true;

    [Header("Glue (опційні)")]
    [SerializeField] private WeaponMount _mount;
    [SerializeField] private WeaponIKLinker _ik;
    [SerializeField] private RecoilKick _recoil;
    [SerializeField] private Animator _anim;
    [SerializeField] private RigWeightBlender _rigBlend;

    private readonly List<WeaponBase> _weapons = new();
    private int _currentIndex = 0;

    private AnimatorOverrideController _aoc;
    private WeaponController _weaponController;
    private IWeaponFactory _weaponFactory;
    private IAudioService _audioService;

    public event System.Action<WeaponBase, WeaponBase> WeaponChanged;

    [Inject]
    public void Construct([InjectOptional] IWeaponFactory weaponFactory,
                          [InjectOptional] IAudioService audioService)
    {
        _weaponFactory = weaponFactory;
        _audioService = audioService;
    }

    private void Awake()
    {
        _weaponController = GetComponent<WeaponController>();

        if (!weaponHolder)
        {
            var socket = transform.Find("WeaponSocket");
            if (socket) weaponHolder = socket;
            else
            {
                var holder = transform.Find("WeaponHolder ");
                if (!holder)
                {
                    var go = new GameObject("WeaponHolder ");
                    go.transform.SetParent(transform, false);
                    holder = go.transform;
                }
                weaponHolder = holder;
            }
        }

        _mount = weaponHolder.GetComponent<WeaponMount>() ?? weaponHolder.gameObject.AddComponent<WeaponMount>();

        _ik ??= GetComponentInChildren<WeaponIKLinker>(true);
        _recoil ??= GetComponentInChildren<RecoilKick>(true);
        _rigBlend ??= GetComponentInChildren<RigWeightBlender>(true);
        _anim ??= GetComponentInChildren<Animator>(true);

        if (_anim && _anim.runtimeAnimatorController && _aoc == null)
        {
            _aoc = new AnimatorOverrideController(_anim.runtimeAnimatorController);
            _anim.runtimeAnimatorController = _aoc;
        }
    }

    private void Start()
    {
        if (!spawnOnStart || _weaponFactory == null) return;

        bool createdAny = false;
        for (int i = 0; i < initialLoadout.Count; i++)
        {
            var cfg = initialLoadout[i];
            if (!cfg) continue;

            if (_weapons.Count >= maxWeapons)
                AddOrReplaceCurrent(cfg);
            else
            {
                var w = _weaponFactory.Create(cfg, weaponHolder); // <-- сигнатура твоєї фабрики
                _weapons.Add(w);
                _currentIndex = _weapons.Count - 1;
            }
            createdAny = true;
        }

        if (createdAny)
        {
            ApplyActiveWeapon();
            var cur = GetCurrentWeapon();
            AfterSwitch(null, cur);
            _weaponController?.RefreshCurrentWeapon();
        }
    }

    private void OnDestroy()
    {
        for (int i = 0; i < _weapons.Count; i++)
            UnsubscribeWeaponEvents(_weapons[i]);
    }

    public int Count => _weapons.Count;
    public int CurrentIndex => _currentIndex;
    public WeaponBase Current => GetCurrentWeapon();
    public WeaponBase GetCurrentWeapon() => _weapons.Count > 0 ? _weapons[_currentIndex] : null;

    public void SwitchWeapon(int index)
    {
        if (index < 0 || index >= _weapons.Count || index == _currentIndex) return;

        var prev = GetCurrentWeapon();
        if (prev != null) { prev.CancelFiringState(); UnsubscribeWeaponEvents(prev); }

        _currentIndex = index;
        ApplyActiveWeapon();

        var next = GetCurrentWeapon();
        AfterSwitch(prev, next);
        _weaponController?.RefreshCurrentWeapon();
    }

    public void SwitchNext()
    {
        if (_weapons.Count <= 1) return;
        int next = (_currentIndex + 1) % _weapons.Count;
        SwitchWeapon(next);
    }

    public void SwitchPrev()
    {
        if (_weapons.Count <= 1) return;
        int next = (_currentIndex - 1 + _weapons.Count) % _weapons.Count;
        SwitchWeapon(next);
    }

    public void TryPickupWeapon(WeaponPickup pickup)
    {
        if (pickup == null || pickup.WeaponConfig == null || _weaponFactory == null) return;
        AddOrReplaceCurrent(pickup.WeaponConfig);
        Destroy(pickup.gameObject);
    }

    public void AddOrReplaceCurrent(WeaponConfig config)
    {
        if (config == null || _weaponFactory == null) return;

        var prev = GetCurrentWeapon();

        if (_weapons.Count >= maxWeapons)
        {
            if (prev != null)
            {
                prev.CancelFiringState();
                UnsubscribeWeaponEvents(prev);
                prev.gameObject.SetActive(false);
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

    private void ApplyActiveWeapon()
    {
        for (int i = 0; i < _weapons.Count; i++)
        {
            var w = _weapons[i];
            if (!w) continue;
            w.gameObject.SetActive(false);
            if (w.transform.parent != weaponHolder)
                w.transform.SetParent(weaponHolder, false);
        }

        var current = GetCurrentWeapon();
        _mount?.Mount(current);
        if (current) current.gameObject.SetActive(true);
    }

    private void AfterSwitch(WeaponBase prev, WeaponBase next)
    {
        if (_ik)
        {
            if (next != null)
            {
                _ik.SendMessage("LinkTo", next, SendMessageOptions.DontRequireReceiver);
                var points = next.GetComponent<WeaponIKPoints>();
                if (points) _ik.SendMessage("Link", points, SendMessageOptions.DontRequireReceiver);
            }
            else
            {
                _ik.SendMessage("Unlink", SendMessageOptions.DontRequireReceiver);
            }
        }

        if (_recoil != null)
        {
            if (prev != null) prev.Fired -= _recoil.OnFired;
            if (next != null) next.Fired += _recoil.OnFired;
        }

        if (_anim != null)
        {
            if (HasBool(_anim, "HasWeapon"))
                _anim.SetBool("HasWeapon", next != null);

            var aimer = GetComponentInParent<AnimLayerAimer>();
            if (aimer) aimer.SetHasWeapon(next != null);
        }

        _rigBlend?.SetHasWeapon(next != null);
        TryApplyAnimationSet(next ? next.Config : null);

        WeaponChanged?.Invoke(prev, next);
    }

    private void TryApplyAnimationSet(WeaponConfig cfg)
    {
        if (!_anim) return;

        if (_aoc == null && _anim.runtimeAnimatorController)
        {
            _aoc = new AnimatorOverrideController(_anim.runtimeAnimatorController);
            _anim.runtimeAnimatorController = _aoc;
        }

        if (_aoc == null || cfg == null || cfg.animSet == null) return;

        var set = cfg.animSet;
        var map = new (string key, AnimationClip clip)[]
        {
            ("PL_IDLE", set.idle),
            ("PL_WALK_FWD", set.walkFwd),
            ("PL_WALK_BACK", set.walkBack),
            ("PL_STRAFE_L", set.strafeL),
            ("PL_STRAFE_R", set.strafeR),
            ("PL_RUN_FWD", set.runFwd),
            ("PL_RUN_BACK", set.runBack),
            ("PL_RUN_STRAFE_L", set.runStrafeL),
            ("PL_RUN_STRAFE_R", set.runStrafeR),
            ("PL_RELOAD", set.reload),
            ("PL_ADS_POSE", set.adsPoseLoop),
            ("PL_FIRE_ADD", set.fireAdditive),
            ("PL_HANDPOSE_ADD", set.handPoseAdditive),
        };

        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>(_aoc.overridesCount);
        _aoc.GetOverrides(overrides);

        for (int i = 0; i < overrides.Count; i++)
        {
            var orig = overrides[i].Key;
            if (!orig) continue;

            for (int j = 0; j < map.Length; j++)
            {
                if (orig.name == map[j].key)
                {
                    var replacement = map[j].clip ? map[j].clip : orig;
                    overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(orig, replacement);
                    break;
                }
            }
        }

        _aoc.ApplyOverrides(overrides);
    }

    private void UnsubscribeWeaponEvents(WeaponBase w)
    {
        if (!w) return;
        if (_recoil != null) w.Fired -= _recoil.OnFired;
    }

    private static bool HasBool(Animator a, string name)
    {
        if (!a) return false;
        foreach (var p in a.parameters)
            if (p.type == AnimatorControllerParameterType.Bool && p.name == name) return true;
        return false;
    }
}
