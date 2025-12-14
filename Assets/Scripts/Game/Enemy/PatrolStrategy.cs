// Assets/_Project/Code/Runtime/AI/Decision/Strategies/PatrolStrategy.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Fogoyote.BFLike.AI
{
    [CreateAssetMenu(menuName = "AI/Strategies/Patrol")]
    public class PatrolStrategy : AIStrategyBase
    {
        [Header("Area")]
        [Tooltip("–ад≥ус блуканн€ навколо точки привТ€зки.")]
        [SerializeField] private float patrolRadius = 15f;

        [Tooltip("яку м≥н≥мальну/максимальну в≥дстань проходити за один крок (щоб не тикатись поруч).")]
        [SerializeField] private Vector2 stepDistanceMinMax = new Vector2(5f, 12f);

        [Header("Timing")]
        [Tooltip("—к≥льки сто€ти на точц≥ (сек).")]
        [SerializeField] private Vector2 waitSecondsMinMax = new Vector2(1.2f, 3.0f);

        [Header("Motion")]
        [Tooltip("’одимо п≥шки (Sprint=false).")]
        [SerializeField] private bool useSprint = false;

        // ¬нутр≥шн≥й стан на кожного агента (ScriptableObject сп≥льний, тож збер≥гаЇмо стан у словнику)
        private class State
        {
            public Vector3 anchor;
            public Vector3 currentDest;
            public bool hasDest;
            public float waitUntil; // time до €кого стоњмо
        }

        private readonly Dictionary<Transform, State> _states = new();

        public override bool Evaluate(ref AIContext ctx, out AIIntent intent, out float utility)
        {
            intent = default;
            utility = 0f;

            // ѕатруль працюЇ т≥льки коли ц≥л≥ немаЇ
            if (ctx.Target) return false;

            if (!_states.TryGetValue(ctx.Self, out var s))
            {
                s = new State
                {
                    anchor = ctx.Self.position,
                    hasDest = false,
                    waitUntil = 0f
                };
                _states[ctx.Self] = s;
            }

            // якщо ще чекаЇмо на м≥сц≥ Ч просто стоњмо
            if (Time.time < s.waitUntil)
            {
                intent = new AIIntent
                {
                    MoveDir = Vector3.zero,
                    Sprint = false,
                    Aim = false,
                    Fire = false,
                    HasNavDestination = false,
                    WantsCover = false,
                    Target = null
                };
                utility = 0.05f;          // дуже низький utility, щоб будь-€кий Ђбойовийї нам≥р його перекрив
                return true;
            }

            // якщо нема активноњ точки Ч обираЇмо нову
            if (!s.hasDest)
            {
                s.currentDest = PickRandomPointOnNavmesh(s.anchor, patrolRadius, stepDistanceMinMax);
                s.hasDest = true;
            }

            //  оли прибули до точки Ч переходимо у стан оч≥куванн€
            float dist = Vector3.Distance(ctx.Self.position, s.currentDest);
            if (dist <= 0.75f)
            {
                s.hasDest = false;
                s.waitUntil = Time.time + Random.Range(waitSecondsMinMax.x, waitSecondsMinMax.y);

                intent = new AIIntent
                {
                    MoveDir = Vector3.zero,
                    Sprint = false,
                    Aim = false,
                    Fire = false,
                    HasNavDestination = false
                };
                utility = 0.05f;
                return true;
            }

            // ≤накше Ч крокуЇмо до точки
            var moveDir = (s.currentDest - ctx.Self.position);
            moveDir.y = 0f;
            moveDir = moveDir.sqrMagnitude > 0.001f ? moveDir.normalized : Vector3.zero;

            intent = new AIIntent
            {
                MoveDir = moveDir,
                Sprint = useSprint,
                Aim = false,
                Fire = false,
                HasNavDestination = true,
                NavDestination = s.currentDest,
                WantsCover = false,
                Target = null
            };
            utility = 0.05f;
            return true;
        }

        private static Vector3 PickRandomPointOnNavmesh(Vector3 anchor, float radius, Vector2 stepMinMax)
        {
            // ўоб не тикатись у др≥бТ€зок Ч беремо ц≥ль не ближчу за stepMin
            float stepMin = Mathf.Max(0.1f, stepMinMax.x);
            float stepMax = Mathf.Max(stepMin, stepMinMax.y);

            for (int i = 0; i < 12; i++)
            {
                var rnd = Random.insideUnitCircle.normalized * Random.Range(stepMin, Mathf.Min(radius, stepMax));
                var candidate = new Vector3(anchor.x + rnd.x, anchor.y, anchor.z + rnd.y);
                if (NavMesh.SamplePosition(candidate, out var hit, 1.5f, NavMesh.AllAreas))
                    return hit.position;
            }
            // ‘олбек: €кщо не знайшли Ч повертаЇмось до €кор€
            return anchor;
        }
    }
}
