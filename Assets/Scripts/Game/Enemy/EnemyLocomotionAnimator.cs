// Assets/Scripts/Game/Enemy/EnemyLocomotionAnimator.cs
using UnityEngine;
using UnityEngine.AI;
using Zenject;

/// <summary>
/// NPC: синхронізація навігації з Animator'ом у тій же схемі, що й у Player:
/// MoveX/MoveY (локальна проекція швидкості), Speed01 (0..1), IsGrounded/InAir,
/// IsSprinting, Aim01. Є fallback на "Speed" (int), якщо твій контролер старий.
/// Також самостійно повертає корпус у бік руху (agent.updateRotation = false).
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(+80)]
public class EnemyLocomotionAnimator : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator anim;        // признач "Infantryman"
    [SerializeField] private NavMeshAgent agent;   // auto
    [SerializeField] private Transform orientation; // локальна вісь для MoveX/MoveY (за замовчуванням = transform)

    [Inject(Optional = true)] private IInputService _input; // AIInputService з твого префаба

    [Header("Animator params (мають збігатись із контролером)")]
    [SerializeField] private string pMoveX = "MoveX";
    [SerializeField] private string pMoveY = "MoveY";
    [SerializeField] private string pSpeed01 = "Speed01";
    [SerializeField] private string pSpeedIntFallback = "Speed"; // якщо ще є старий int
    [SerializeField] private string pIsGrounded = "IsGrounded";
    [SerializeField] private string pInAir = "InAir";
    [SerializeField] private string pIsSprinting = "IsSprinting";
    [SerializeField] private string pAim01 = "Aim01";

    [Header("Tuning")]
    [Tooltip("Нормалізація Speed01 і MoveX/MoveY (м/с) — зазвичай твоя швидкість бігу.")]
    [SerializeField] private float runSpeed = 6f;
    [Tooltip("Швидкість демпфування напрямку (MoveX/MoveY). Більше — швидше реагує.")]
    [SerializeField, Range(1f, 20f)] private float dirLerp = 10f;
    [Tooltip("Швидкість демпфування модуля (Speed01).")]
    [SerializeField, Range(1f, 20f)] private float spdLerp = 10f;
    [Tooltip("Швидкість обертання корпуса (deg/s).")]
    [SerializeField] private float turnSpeedDegPerSec = 540f;
    [Tooltip("Порог «вважаємо стоїть» (м/с).")]
    [SerializeField] private float stopEps = 0.05f;

    // hashes
    int hMoveX, hMoveY, hSpeed01, hSpeedInt, hIsGrounded, hInAir, hIsSprinting, hAim01;
    bool hasMoveX, hasMoveY, hasSpeed01, hasSpeedInt, hasIsGrounded, hasInAir, hasIsSprinting, hasAim01;

    // state
    float _mx, _my, _spd01;

    void Reset()
    {
        anim = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        orientation = transform;
    }

    void Awake()
    {
        if (!anim) anim = GetComponentInChildren<Animator>();
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!orientation) orientation = transform;

        if (!anim) { Debug.LogError("[EnemyLocomotionAnimator] Animator not found", this); enabled = false; return; }
        if (!agent) { Debug.LogError("[EnemyLocomotionAnimator] NavMeshAgent not found", this); enabled = false; return; }

        // ми керуємо поворотом самі — краще для IK/риґів
        agent.updateRotation = false;

        CacheAnimatorParams();
    }

    void CacheAnimatorParams()
    {
        hasMoveX = Has(anim, pMoveX, AnimatorControllerParameterType.Float);
        hasMoveY = Has(anim, pMoveY, AnimatorControllerParameterType.Float);
        hasSpeed01 = Has(anim, pSpeed01, AnimatorControllerParameterType.Float);
        hasSpeedInt = Has(anim, pSpeedIntFallback, AnimatorControllerParameterType.Int);
        hasIsGrounded = Has(anim, pIsGrounded, AnimatorControllerParameterType.Bool);
        hasInAir = Has(anim, pInAir, AnimatorControllerParameterType.Bool);
        hasIsSprinting = Has(anim, pIsSprinting, AnimatorControllerParameterType.Bool);
        hasAim01 = Has(anim, pAim01, AnimatorControllerParameterType.Float);

        hMoveX = Animator.StringToHash(pMoveX);
        hMoveY = Animator.StringToHash(pMoveY);
        hSpeed01 = Animator.StringToHash(pSpeed01);
        hSpeedInt = Animator.StringToHash(pSpeedIntFallback);
        hIsGrounded = Animator.StringToHash(pIsGrounded);
        hInAir = Animator.StringToHash(pInAir);
        hIsSprinting = Animator.StringToHash(pIsSprinting);
        hAim01 = Animator.StringToHash(pAim01);

        if (!hasSpeed01 && !hasSpeedInt)
            Debug.LogWarning($"[EnemyLocomotionAnimator] Немає Float '{pSpeed01}' і Int '{pSpeedIntFallback}' у '{anim.name}'. Додай '{pSpeed01}' (рекомендовано).", anim);
    }

    void Update()
    {
        if (!agent || !anim) return;

        // 1) швидкість на площині
        Vector3 v = agent.velocity; v.y = 0f;
        float planar = v.magnitude;

        // 2) нормалізація
        float tSpeed = Mathf.Clamp01(planar / Mathf.Max(0.01f, runSpeed));

        // 3) напрямок у локалі orientation (щоб MoveX/MoveY працювали як у Player)
        Vector3 fwd = orientation.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 right = orientation.right; right.y = 0f; right.Normalize();

        float targetY = 0f, targetX = 0f;
        if (planar > 0.001f)
        {
            Vector3 dir = v.normalized;
            targetY = Mathf.Clamp(Vector3.Dot(dir, fwd), -1f, 1f);
            targetX = Mathf.Clamp(Vector3.Dot(dir, right), -1f, 1f);
        }

        // 4) згладження та запис у Animator
        _spd01 = Mathf.Lerp(_spd01, tSpeed, Time.deltaTime * spdLerp);
        if (hasSpeed01) anim.SetFloat(hSpeed01, _spd01);
        else if (hasSpeedInt) anim.SetInteger(hSpeedInt, Mathf.RoundToInt(_spd01 * 100f));

        _mx = Mathf.Lerp(_mx, targetX, Time.deltaTime * dirLerp);
        _my = Mathf.Lerp(_my, targetY, Time.deltaTime * dirLerp);
        if (hasMoveX) anim.SetFloat(hMoveX, _mx, 0.1f, Time.deltaTime);
        if (hasMoveY) anim.SetFloat(hMoveY, _my, 0.1f, Time.deltaTime);

        // 5) Ground/Air (у NavMeshAgent немає стрибків — вважаємо стоїть/на землі)
        bool grounded = true;
        if (hasIsGrounded) anim.SetBool(hIsGrounded, grounded);
        if (hasInAir) anim.SetBool(hInAir, !grounded);

        // 6) Sprint/Aim з AI (якщо AIInputService під’єднаний), інакше — вимкнено
        if (_input != null)
        {
            if (hasIsSprinting) anim.SetBool(hIsSprinting, _input.IsSprintHeld());
            if (hasAim01) anim.SetFloat(hAim01, _input.IsAimHeld() ? 1f : 0f);
        }

        // 7) Обертання корпуса за напрямком руху
        RotateTowardsVelocity(Time.deltaTime);
    }

    void RotateTowardsVelocity(float dt)
    {
        Vector3 vel = agent.velocity; vel.y = 0f;
        if (vel.magnitude <= stopEps) return;

        Quaternion target = Quaternion.LookRotation(vel.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, target, turnSpeedDegPerSec * dt);
    }

    static bool Has(Animator a, string name, AnimatorControllerParameterType type)
    {
        if (!a || string.IsNullOrEmpty(name)) return false;
        foreach (var p in a.parameters)
            if (p.type == type && p.name == name) return true;
        return false;
    }
}
