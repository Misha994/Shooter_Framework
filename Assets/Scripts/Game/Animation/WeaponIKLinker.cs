using UnityEngine;
using UnityEngine.Animations.Rigging;
using Zenject;

public class WeaponIKLinker : MonoBehaviour
{
    [Header("Player IK Targets")]
    [SerializeField] private Transform rightTarget;
    [SerializeField] private Transform rightHint;
    [SerializeField] private Transform leftTarget;
    [SerializeField] private Transform leftHint;

    [Header("Rigs")]
    [SerializeField] private Rig handsRig;

    [Header("Lerp")]
    [SerializeField] private float aimLerpSpeed = 12f;   // для t (hip↔ADS)
    [SerializeField] private float posLerp = 18f;
    [SerializeField] private float rotLerp = 22f;

    [Inject(Optional = true)] private IInputService _input;
    [Inject(Optional = true)] private SignalBus _bus;

    private WeaponIKPoints _points;
    private float _t;   // 0=hip 1=ADS
    private float _tVel;

    public void Link(WeaponIKPoints points)
    {
        _points = points;
        // одразу піджени позиції щоб не було стрибка
        if (_points != null)
        {
            SnapTo(_points.rightHipTarget, rightTarget);
            SnapTo(_points.rightHipHint, rightHint);
            SnapTo(_points.leftHipTarget,  leftTarget);
            SnapTo(_points.leftHipHint,   leftHint);
        }
    }

    public void Unlink() => _points = null;

    private void SnapTo(Transform src, Transform dst)
    {
        if (!src || !dst) return;
        dst.position = src.position; dst.rotation = src.rotation;
    }

    void Update()
    {
        if (!_points || !handsRig) return;

        // Обчислюємо t: інпут → Animator ("Aim01") → фолбек 0
        float targetT = 0f;
        if (_input != null) targetT = _input.IsAimHeld() ? 1f : 0f;
        else
        {
            var rb = GetComponentInParent<RigWeightBlender>();
            if (rb) { /* якщо є публічний геттер — використай його; інакше фолбек нижче */ }
            var anim = GetComponentInParent<Animator>();
            if (anim) targetT = anim.HasFloat("Aim01") ? anim.GetFloat("Aim01") : 0f;
        }

        _t = Mathf.MoveTowards(_t, targetT, aimLerpSpeed * Time.deltaTime);

        // Лерпимо кожну точку між hip і ADS
        ApplyLerp(_points.rightHipTarget, _points.rightAdsTarget, rightTarget, true);
        ApplyLerp(_points.rightHipHint,   _points.rightAdsHint,   rightHint,   false);
        ApplyLerp(_points.leftHipTarget,  _points.leftAdsTarget,  leftTarget,  true);
        ApplyLerp(_points.leftHipHint,    _points.leftAdsHint,    leftHint,    false);
    }

    private void ApplyLerp(Transform hip, Transform ads, Transform dst, bool withRotation)
    {
        if (!hip || !ads || !dst) return;

        Vector3 p = Vector3.Lerp(hip.position, ads.position, _t);
        dst.position = Vector3.Lerp(dst.position, p, posLerp * Time.deltaTime);

        if (withRotation)
        {
            Quaternion r = Quaternion.Slerp(hip.rotation, ads.rotation, _t);
            dst.rotation = Quaternion.Slerp(dst.rotation, r, rotLerp * Time.deltaTime);
        }
    }
}

static class AnimatorExt
{
    public static bool HasFloat(this Animator a, string name)
    {
        foreach (var p in a.parameters)
            if (p.type == AnimatorControllerParameterType.Float && p.name == name) return true;
        return false;
    }
}
