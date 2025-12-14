using UnityEngine;
using UnityEngine.AI;

namespace Fogoyote.BFLike.AI.Navigation.Cover
{
    [DisallowMultipleComponent]
    public class CoverFinder : MonoBehaviour, ICoverFinder
    {
        [Header("Scan")]
        [SerializeField] private float searchRadius = 18f;
        [SerializeField] private LayerMask coverMask;
        [SerializeField] private LayerMask losBlockMask;
        [SerializeField] private float sampleStep = 0.6f;

        [Header("Placement")]
        [SerializeField] private float backOffset = 0.6f;
        [SerializeField] private float lateralProbe = 1.0f;
        [SerializeField] private float standClearance = 0.5f;

        public bool TryFindCover(Vector3 origin, Transform threat, out Vector3 bestPos, out Vector3 bestNormal, out float bestScore)
        {
            bestPos = default; bestNormal = default; bestScore = 0f;
            if (!threat) return false;

            var cols = Physics.OverlapSphere(origin, searchRadius, coverMask, QueryTriggerInteraction.Ignore);
            if (cols.Length == 0) return false;

            Vector3 toThreat = threat.position - origin;
            float distT = toThreat.magnitude;
            Vector3 threatDir = toThreat.sqrMagnitude > 0.0001f ? toThreat / distT : Vector3.forward;

            foreach (var col in cols)
            {
                var b = col.bounds;
                int stepsX = Mathf.Max(1, Mathf.RoundToInt(b.size.x / sampleStep));
                int stepsZ = Mathf.Max(1, Mathf.RoundToInt(b.size.z / sampleStep));

                for (int ix = 0; ix <= stepsX; ix++)
                    for (int iz = 0; iz <= stepsZ; iz++)
                    {
                        float x = Mathf.Lerp(b.min.x + standClearance, b.max.x - standClearance, ix / (float)stepsX);
                        float z = Mathf.Lerp(b.min.z + standClearance, b.max.z - standClearance, iz / (float)stepsZ);
                        var sample = new Vector3(x, b.center.y, z);

                        if (Physics.Raycast(origin + Vector3.up * 1.2f, (sample - origin).normalized, out var hit, searchRadius, coverMask))
                        {
                            var n = hit.normal;
                            if (n.y > 0.7f) continue;

                            var candidate = hit.point + n * backOffset;
                            candidate.y = origin.y;

                            if (HasHardCover(candidate, threat.position))
                            {
                                var right = Vector3.Cross(Vector3.up, n).normalized;
                                var leftPeek = candidate + right * lateralProbe;
                                var rightPeek = candidate - right * lateralProbe;

                                bool leftCovered = HasHardCover(leftPeek, threat.position);
                                bool rightCovered = HasHardCover(rightPeek, threat.position);

                                float distScore = 1f / Mathf.Max(1f, Vector3.Distance(origin, candidate));
                                float angleScore = 1f - Mathf.Abs(Vector3.Dot(n, threatDir));
                                float peekScore = (leftCovered || rightCovered) ? 0.15f : 0f;
                                float score = distScore * 0.6f + angleScore * 0.25f + peekScore;

                                if (score > bestScore && NavMesh.SamplePosition(candidate, out var navHit, 1.0f, NavMesh.AllAreas))
                                {
                                    bestScore = score; bestPos = navHit.position; bestNormal = n;
                                }
                            }
                        }
                    }
            }
            return bestScore > 0f;
        }

        private bool HasHardCover(Vector3 from, Vector3 threatPos)
        {
            var a = from + Vector3.up * 1.4f;
            var b = threatPos + Vector3.up * 1.4f;
            var dir = b - a; var dist = dir.magnitude;
            return Physics.Raycast(a, dir / Mathf.Max(0.0001f, dist), out var hit, dist, losBlockMask) && hit.collider;
        }
    }
}
