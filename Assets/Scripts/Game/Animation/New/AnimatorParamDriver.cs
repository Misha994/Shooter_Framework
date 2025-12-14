using UnityEngine;
using Zenject;

[RequireComponent(typeof(Animator))]
public class AnimatorParamDriver : MonoBehaviour
{
    [SerializeField] private CharacterController cc;  // Player’ів
    [SerializeField] private Transform cam;           // PlayerCamera (yaw reference)
    [SerializeField] private float runSpeed = 6f;     // підігнай під свій GameConfig
    [SerializeField] private float damp = 10f;        // демпф MoveX/Y

    [Inject(Optional = true)] private IInputService _input;
    [Inject(Optional = true)] private SignalBus _bus;

    private Animator _anim;
    private bool _isAiming;
    private Vector2 _moveSmoothed;

    private static readonly int HashMoveX = Animator.StringToHash("MoveX");
    private static readonly int HashMoveY = Animator.StringToHash("MoveY");
    private static readonly int HashSpeed01 = Animator.StringToHash("Speed01");
    private static readonly int HashIsAiming = Animator.StringToHash("IsAiming");
    private static readonly int HashIsSprinting = Animator.StringToHash("IsSprinting");
    private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int HashInAir = Animator.StringToHash("InAir");

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        if (!cc) cc = GetComponentInParent<CharacterController>();
        if (!cam && Camera.main) cam = Camera.main.transform;
    }

    private void OnEnable()
    {
        _bus?.Subscribe<AimStateChangedSignal>(OnAimChanged);
    }
    private void OnDisable()
    {
        _bus?.Unsubscribe<AimStateChangedSignal>(OnAimChanged);
    }
    private void OnAimChanged(AimStateChangedSignal s) => _isAiming = s.IsAiming;

    private void Update()
    {
        // 1) Ground/Air
        bool grounded = cc ? cc.isGrounded : true;
        _anim.SetBool(HashIsGrounded, grounded);
        _anim.SetBool(HashInAir, !grounded);

        // 2) Aim/Sprint
        _anim.SetBool(HashIsAiming, _isAiming);
        bool sprint = _input != null && _input.IsSprintHeld();
        _anim.SetBool(HashIsSprinting, sprint);

        // 3) MoveX/MoveY у локалі персонажа (за напрямком камери)
        Vector3 velo = cc ? cc.velocity : Vector3.zero;
        Vector3 fwd = cam ? cam.forward : transform.forward;
        Vector3 right = cam ? cam.right : transform.right;
        fwd.y = right.y = 0f; fwd.Normalize(); right.Normalize();

        float vx = Vector3.Dot(velo, right);
        float vy = Vector3.Dot(velo, fwd);

        // нормуємо до швидкості бігу → отримаємо ~[-1..1]
        Vector2 move = new Vector2(vx, vy) / Mathf.Max(0.01f, runSpeed);

        // демпф (щоб не смикалось)
        _moveSmoothed = Vector2.MoveTowards(_moveSmoothed, move, damp * Time.deltaTime);

        _anim.SetFloat(HashMoveX, _moveSmoothed.x);
        _anim.SetFloat(HashMoveY, _moveSmoothed.y);

        // 4) Speed01 = 0..1 (для 1D walk→run)
        float horizontalSpeed = new Vector2(velo.x, velo.z).magnitude;
        float speed01 = Mathf.InverseLerp(0f, runSpeed, horizontalSpeed);
        _anim.SetFloat(HashSpeed01, speed01);
    }
}
