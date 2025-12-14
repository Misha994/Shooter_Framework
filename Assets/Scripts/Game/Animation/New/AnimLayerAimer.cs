using UnityEngine;
using Zenject;

[DefaultExecutionOrder(90)]
public class AnimLayerAimer : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator anim;
    [SerializeField] private string aimParam = "Aim01";
    [SerializeField] private int upperBodyLayer = 1; // шар із маскою верхнього тіла

    [Header("Blend")]
    [SerializeField] private float aimLerpSpeed = 10f;   // швидкість виходу в ADS
    [SerializeField] private float hipLerpSpeed = 10f;   // швидкість повернення в HIP

    [Header("State")]
    [SerializeField] private bool hasWeapon = true;
    [SerializeField] private bool disableUpperBodyWhileSprinting = true;

    [Inject(Optional = true)] private IInputService _input;
    [Inject(Optional = true)] private SignalBus _bus;

    private float _aim01;
    private bool _isAiming;
    private bool _isSprinting;

    private void Reset()
    {
        anim = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        _bus?.Subscribe<AimStateChangedSignal>(OnAimChanged);
    }

    private void OnDisable()
    {
        _bus?.TryUnsubscribe<AimStateChangedSignal>(OnAimChanged);
    }

    public void SetHasWeapon(bool v) => hasWeapon = v;

    private void OnAimChanged(AimStateChangedSignal s) => _isAiming = s.IsAiming;

    private void Update()
    {
        // Якщо немає сигналів — читаємо інпут напряму (дозволяє працювати без SignaBus)
        if (_input != null)
        {
            _isAiming = _input.IsAimHeld();
            _isSprinting = _input.IsSprintHeld();
        }

        float target = _isAiming ? 1f : 0f;
        float speed = _isAiming ? aimLerpSpeed : hipLerpSpeed;
        _aim01 = Mathf.MoveTowards(_aim01, target, speed * Time.deltaTime);

        if (anim)
        {
            anim.SetFloat(aimParam, _aim01);
            // Вага шару: є зброя → шар активний. Якщо хочеш опускати руки під час спринту — обнулимо шар.
            float layerW = (hasWeapon && (!disableUpperBodyWhileSprinting || !_isSprinting)) ? 1f : 0f;
            if (upperBodyLayer >= 0 && upperBodyLayer < anim.layerCount)
                anim.SetLayerWeight(upperBodyLayer, layerW);
        }
    }
}
