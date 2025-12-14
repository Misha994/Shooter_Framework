// Assets/_Project/Code/Runtime/AI/AITacticalPlanner.cs
using System.Collections.Generic;
using Combat.Core;                 // для DamagePayload
using Fogoyote.BFLike.AI.Perception;
using UnityEngine;

namespace Fogoyote.BFLike.AI
{
    [DisallowMultipleComponent]
    public class AITacticalPlanner : MonoBehaviour
    {
        [Header("Perception")]
        [SerializeField] private RaycastVision vision;
        [SerializeField] private List<AIStrategyBase> strategiesAssets = new();

        [Header("Memory / Investigation")]
        [Tooltip("Як довго тримаємо в пам'яті останню видиму ціль")]
        [SerializeField] private float memorySeconds = 2.0f;

        [Tooltip("Скільки секунд NPC буде розслідувати точку пострілу")]
        [SerializeField] private float investigateSecondsOnHit = 4.0f;

        [Tooltip("Коротка реакція корпусом/прицілюванням у бік пострілу")]
        [SerializeField] private float hitReactSeconds = 0.6f;

        private readonly List<IAIStrategy> _strategies = new();
        private Transform _lastTarget;
        private float _lastSeenTime;
        private AIIntent _lastIntent;
        private float _lastUtility;

        // Розслідування точки (куди йти, якщо нікого не бачимо)
        private Vector3 _investigatePoint;
        private float _investigateUntil;

        // Короткий орієнтир від попадання (напрямок, куди дивитись)
        private Vector3 _lastHitDirWS;
        private float _hitReactUntil;

        private void Awake()
        {
            _strategies.Clear();
            foreach (var s in strategiesAssets)
                if (s != null) _strategies.Add(s);
        }

        #region External triggers (call these from your damage pipeline)

        /// <summary>
        /// Викликати, коли NPC отримав шкоду. Сигнатура сумісна з Action&lt;GameObject, DamagePayload, float&gt;.
        /// </summary>
        public void OnDamaged(GameObject victim, DamagePayload payload, float amount)
        {
            // 1) Напрямок у бік стрільця: зазвичай нормаль уже дивиться на стрільця
            Vector3 n = payload.HitNormal;
            n.y = 0f;

            // fallback, якщо нормаль порожня (edge-кейси або тригери)
            if (n.sqrMagnitude < 1e-4f)
                n = (transform.forward);

            n.Normalize();
            _lastHitDirWS = n;
            _hitReactUntil = Time.time + hitReactSeconds;

            // 2) Закласти точку розслідування в місце хіта
            if (payload.HitPoint != Vector3.zero)
                InvestigatePoint(payload.HitPoint, investigateSecondsOnHit);

            // 3) Якщо бачимо стрільця — оновити пам’ять цілі (робиться зору, але на всяк випадок підтримаємо)
            _lastSeenTime = Time.time; // дає шанс стратегіям одразу увійти в бій
        }

        /// <summary>Задати точку для розслідування (наприклад, місце пострілу).</summary>
        public void InvestigatePoint(Vector3 point, float seconds)
        {
            _investigatePoint = point;
            _investigateUntil = Time.time + Mathf.Max(0.25f, seconds);
        }

        /// <summary>Форсувати конкретну ціль (наприклад, коли точно знаємо стрільця).</summary>
        public void ForceEngageTarget(Transform t, float seconds)
        {
            _lastTarget = t;
            _lastSeenTime = Time.time;
            _investigateUntil = Time.time + Mathf.Max(0.25f, seconds);
        }

        #endregion

        public bool TryMakeIntent(out AIIntent intent)
        {
            intent = default;

            // 1) Оновити target по візії або очистити по таймеру пам’яті
            if (vision != null && vision.TryGetBestVisibleEnemy(out var t, out _))
            {
                _lastTarget = t;
                _lastSeenTime = Time.time;
            }
            else if (_lastTarget && (Time.time - _lastSeenTime) > memorySeconds)
            {
                _lastTarget = null;
            }

            var ctx = new AIContext
            {
                Self = transform,
                Target = _lastTarget,
                TargetDistance = _lastTarget ? Vector3.Distance(transform.position, _lastTarget.position) : float.PositiveInfinity,
                DesiredDirection = _lastTarget ? (_lastTarget.position - transform.position).normalized : Vector3.zero,
                Health01 = 1f
            };

            _lastUtility = 0f;
            bool decided = false;

            // 2) Дати шанс стратегіям
            foreach (var st in _strategies)
            {
                if (st.Evaluate(ref ctx, out var i, out var u) && u > _lastUtility)
                {
                    _lastUtility = u;
                    _lastIntent = i;
                    decided = true;
                }
            }

            // 3) Фолбек: є пам'ять цілі — вести бій (рухатись боком і вести вогонь)
            if (!decided && _lastTarget)
            {
                var to = (_lastTarget.position - transform.position).normalized;
                _lastIntent = new AIIntent
                {
                    MoveDir = Vector3.Cross(Vector3.up, to), // невеличкий стрейф
                    Sprint = false,
                    Aim = true,
                    Fire = true,
                    Target = _lastTarget,
                    HasNavDestination = false
                };
                decided = true;
            }

            // 4) Фолбек-2: точка розслідування активна — рушаємо туди, прицілюємось
            if (!decided && Time.time < _investigateUntil && _investigatePoint != Vector3.zero)
            {
                var to = _investigatePoint - transform.position; to.y = 0f;
                var dir = to.sqrMagnitude > 0.001f ? to.normalized : Vector3.zero;

                _lastIntent = new AIIntent
                {
                    MoveDir = dir,
                    Sprint = false,
                    Aim = true,
                    Fire = false,
                    Target = null,
                    HasNavDestination = true,
                    NavDestination = _investigatePoint
                };
                decided = true;
            }

            // 5) Фолбек-3: короткий “хіт-реакт” у бік пострілу (розвернутися/прицілитись), без вогню
            if (!decided && Time.time < _hitReactUntil && _lastHitDirWS.sqrMagnitude > 1e-4f)
            {
                _lastIntent = new AIIntent
                {
                    MoveDir = Vector3.zero,
                    Sprint = false,
                    Aim = true,
                    Fire = false,
                    Target = null,
                    HasNavDestination = false
                };

                // Плавний розворот у бік стрільця
                var fwd = transform.forward; fwd.y = 0f;
                if (_lastHitDirWS.sqrMagnitude > 1e-4f)
                {
                    var targetRot = Quaternion.LookRotation(_lastHitDirWS);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
                }

                decided = true;
            }

            if (decided) { intent = _lastIntent; return true; }
            return false;
        }

        public bool TryPeekIntent(out AIIntent intent)
        {
            intent = _lastIntent;
            return _lastUtility > 0f || _lastIntent.Target != null;
        }
    }
}
