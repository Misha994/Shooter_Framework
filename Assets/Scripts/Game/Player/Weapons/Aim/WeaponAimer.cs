using UnityEngine;
using Zenject;

public class WeaponAimer : MonoBehaviour
{
    [Header("Offsets (local)")]
    [SerializeField] Transform hipOffset;
    [SerializeField] Transform adsOffset;
    [SerializeField] Transform offsetsRoot;

    [Header("Timing")]
    [SerializeField, Min(0.01f)] float aimTime = 0.15f;

    [Inject(Optional = true)] IInputService _input;
    [Inject] SignalBus _bus;

    float _t;
    bool _isAiming;

    void OnEnable()
    {
        _t = 0f;
        Apply(0f);
        _bus?.Fire(new AimStateChangedSignal(false));
    }

    public void SetupFromConfig(WeaponConfig cfg)
    {
        if (cfg != null)
            aimTime = Mathf.Max(0.01f, cfg.aimTimeSeconds);
    }

    void Update()
    {
        bool wantAim = _input != null && _input.IsAimHeld();
        if (wantAim != _isAiming)
        {
            _isAiming = wantAim;
            _bus?.Fire(new AimStateChangedSignal(_isAiming));
        }

        float target = _isAiming ? 1f : 0f;
        _t = Mathf.MoveTowards(_t, target, Time.deltaTime / Mathf.Max(0.01f, aimTime));
        Apply(_t);
    }

    void Apply(float t)
    {
        if (!offsetsRoot || !hipOffset || !adsOffset) return;
        offsetsRoot.localPosition = Vector3.Lerp(hipOffset.localPosition, adsOffset.localPosition, t);
        offsetsRoot.localRotation = Quaternion.Slerp(hipOffset.localRotation, adsOffset.localRotation, t);
    }
}
