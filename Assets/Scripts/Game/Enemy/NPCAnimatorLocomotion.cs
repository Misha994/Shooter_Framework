// Assets/_Project/Code/Runtime/AI/Locomotion/NPCAnimatorLocomotion.cs
using Fogoyote.BFLike.AI;
using Fogoyote.BFLike.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class NPCAnimatorLocomotion : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Animator anim;             // Infantryman
    [SerializeField] private NavMeshAgent agent;
    [SerializeField] private AIInputService input;      // щоб читати Aim/Sprint
    [SerializeField] private NavToIntentAdapter nav;    // щоб читати бажаний рух/дос€гненн€ ц≥л≥

    [Header("Animator params (п≥длаштуй п≥д св≥й контролер)")]
    [SerializeField] private string speedParam = "Speed";          // float 0..1
    [SerializeField] private string sprintBool = "IsSprinting";    // bool
    [SerializeField] private string aimFloat = "Aim01";            // float 0..1

    [Header("Tuning")]
    [SerializeField] private float turnSpeed = 9f;     // швидк≥сть довороту, €кщо немаЇ Уturn-in-placeФ у кл≥пах
    [SerializeField] private float maxMoveSpeed = 2.2f; // оч≥кувана швидк≥сть б≥гу з ан≥мац≥й (м/с) дл€ нормал≥зац≥њ в Speed
    [SerializeField] private float speedLerp = 12f;    // згладжуванн€ параметра Speed

    private CharacterController cc;                    // опц≥йно: €кщо хочеш п≥дстраховку без root motion
    private float _speed01;                            // згладжене значенн€ в ан≥матор

    void Reset()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponentInChildren<Animator>();
        input = GetComponent<AIInputService>();
        nav = GetComponent<NavToIntentAdapter>();
        cc = GetComponent<CharacterController>();
    }

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!anim) anim = GetComponentInChildren<Animator>(true);
        if (!input) input = GetComponent<AIInputService>();
        if (!nav) nav = GetComponent<NavToIntentAdapter>();
        cc = GetComponent<CharacterController>();

        // ƒуже важливо: агент лише рахуЇ шл€х Ч рухаЇмо сам≥
        agent.updatePosition = false;
        agent.updateRotation = false;

        if (anim) anim.applyRootMotion = true;
    }

    void Update()
    {
        if (!anim || !agent || !nav) return;

        // 1) «читуЇмо бажаний вектор руху з нав≥гац≥њ
        Vector3 desired = nav.DesiredMoveWorld;
        desired.y = 0f;

        // —тавимо швидк≥сть у BlendTree (0..1) Ч нормал≥зуЇмо на оч≥кувану швидк≥сть з кл≥п≥в
        float desiredSpeed = Mathf.Clamp01(desired.magnitude);
        _speed01 = Mathf.Lerp(_speed01, desiredSpeed, 1f - Mathf.Exp(-speedLerp * Time.deltaTime));

        if (!string.IsNullOrEmpty(speedParam)) anim.SetFloat(speedParam, _speed01);
        if (!string.IsNullOrEmpty(sprintBool) && input != null) anim.SetBool(sprintBool, input.IsSprintHeld());
        if (!string.IsNullOrEmpty(aimFloat) && input != null) anim.SetFloat(aimFloat, input.IsAimHeld() ? 1f : 0f);

        // 2) ѕоворот корпусу:
        //    - €кщо Ї Target (AIInputService уже крутить в б≥к ц≥л≥), можна залишити €к Ї
        //    - €кщо Target немаЇ Ч докручуЇмос€ в б≥к руху (допомога, €кщо немаЇ turn-in-place)
        if (desired.sqrMagnitude > 0.001f && !HasTarget())
        {
            var targetRot = Quaternion.LookRotation(desired);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, turnSpeed * Time.deltaTime);
        }

        // 3) —инхрон≥зувати agent.nextPosition з нашим Transform (щоб агент не Ђгубивї шл€х)
        agent.nextPosition = transform.position;

        // 4) ‘олбек: €кщо ≥з €коњсь причини root motion вимкнено Ч мТ€ко рухаЇмо сам≥
        if (!anim.applyRootMotion && cc)
        {
            var move = desired * maxMoveSpeed * Time.deltaTime;
            cc.Move(move);
        }
    }

    void OnAnimatorMove()
    {
        // –еальний рух Ч зв≥дси, €кщо ув≥мкнений root motion
        if (!anim || !anim.applyRootMotion) return;

        Vector3 delta = anim.deltaPosition;
        Quaternion dRot = anim.deltaRotation;

        // –ух Ч через CharacterController, щоб була повага до кол≥з≥й
        if (cc)
        {
            cc.Move(delta);
            transform.rotation = dRot * transform.rotation;
        }
        else
        {
            transform.position += delta;
            transform.rotation = dRot * transform.rotation;
        }

        // “римай агент синхрон≥зованим
        if (agent) agent.nextPosition = transform.position;
    }

    private bool HasTarget()
    {
        // ѕростий спос≥б д≥знатись, чи Ї ц≥ль у планувальника
        // якщо хочеш точн≥ший Ч збережи TryPeekIntent(Target!=null) у AIInputService та прочитай тут.
        var planner = nav ? nav.GetComponent<AITacticalPlanner>() : null;
        if (planner != null && planner.TryPeekIntent(out var i))
            return i.Target != null;
        return false;
    }
}
