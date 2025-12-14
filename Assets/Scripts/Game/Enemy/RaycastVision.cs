// Assets/_Project/Code/Runtime/AI/Perception/RaycastVision.cs
using UnityEngine;

namespace Fogoyote.BFLike.AI.Perception
{
    [DisallowMultipleComponent]
    public class RaycastVision : MonoBehaviour, IVision
    {
        [Header("Vision")]
        [SerializeField] private float viewDistance = 60f;
        [SerializeField, Range(1f, 179f)] private float fovDeg = 110f;

        [Header("Masks")]
        [Tooltip("Ўари, на €ких шукаЇмо ц≥л≥ (гравц≥в/NPC)")]
        [SerializeField] private LayerMask targetsMask = ~0;
        [Tooltip("Ўари, €к≥ ЅЋќ ”ё“№ л≥н≥ю зору (ст≥ни, укритт€)")]
        [SerializeField] private LayerMask losBlockersMask = ~0;

        [Header("Allies/Enemies")]
        [SerializeField] private FactionRules rules;
        [SerializeField] private bool requireHostility = true;

        [Header("Eyes")]
        [SerializeField] private Transform eyes;

        [Header("Debug")]
        [SerializeField] private bool drawGizmos = false;

        public bool TryGetBestVisibleEnemy(out Transform best, out float bestScore)
        {
            best = null; bestScore = 0f;

            if (!eyes)
            {
                eyes = transform; // автоп≥дстановка
                Debug.LogWarning($"[RaycastVision] 'eyes' не задано на {name}. ¬икористовую transform.", this);
            }

            var me = GetComponentInParent<IAllegiance>();
            if (requireHostility && (me == null || rules == null))
            {
                Debug.LogWarning($"[RaycastVision] requireHostility ув≥мкнено, але в≥дсутн≥ IAllegiance або FactionRules на {name}", this);
            }

            var colls = Physics.OverlapSphere(eyes.position, viewDistance, targetsMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < colls.Length; i++)
            {
                var tRoot = colls[i].transform;

                if (requireHostility)
                {
                    var their = tRoot.GetComponentInParent<IAllegiance>();
                    if (their == null || me == null || rules == null || !rules.AreHostile(me.Faction, their.Faction))
                        continue;
                }

                Vector3 dir = tRoot.position - eyes.position;
                float dist = dir.magnitude;
                if (dist > viewDistance) continue;

                float angle = Vector3.Angle(eyes.forward, dir);
                if (angle > fovDeg * 0.5f) continue;

                // ѕерев≥рка л≥н≥њ зору
                if (Physics.Raycast(eyes.position, dir / Mathf.Max(0.0001f, dist), out var hit, dist, losBlockersMask, QueryTriggerInteraction.Ignore))
                {
                    // якщо перша перешкода Ч сам кандидат, вважаЇмо, що бачимо
                    if (!hit.transform.IsChildOf(tRoot)) continue;
                }

                float score = 1f / Mathf.Max(5f, dist);
                if (score > bestScore) { bestScore = score; best = tRoot; }
            }

            return best != null;
        }

        void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;
            var origin = eyes ? eyes.position : transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, viewDistance);

            Vector3 fwd = (eyes ? eyes.forward : transform.forward);
            Quaternion left = Quaternion.AngleAxis(-fovDeg * 0.5f, Vector3.up);
            Quaternion right = Quaternion.AngleAxis(+fovDeg * 0.5f, Vector3.up);
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(origin, left * fwd * viewDistance);
            Gizmos.DrawRay(origin, right * fwd * viewDistance);
        }
    }
}
