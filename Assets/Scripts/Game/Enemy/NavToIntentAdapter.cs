using UnityEngine;
using UnityEngine.AI;

namespace Fogoyote.BFLike.AI.Navigation
{
    [RequireComponent(typeof(NavMeshAgent))]
    public class NavToIntentAdapter : MonoBehaviour
    {
        [SerializeField] private AITacticalPlanner planner;
        [SerializeField] private float arrivedThreshold = 0.75f;

        private NavMeshAgent _agent;

        public Vector3 DesiredMoveWorld { get; private set; }
        public bool ReachedDestination { get; private set; }

        void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.updateRotation = false;
        }

        void Update()
        {
            if (planner != null && planner.TryPeekIntent(out var intent))
            {
                if (intent.HasNavDestination)
                    _agent.SetDestination(intent.NavDestination);
                else if (intent.Target)
                    _agent.SetDestination(intent.Target.position);

                var vel = _agent.desiredVelocity;
                DesiredMoveWorld = vel.sqrMagnitude > 0.01f ? vel.normalized : intent.MoveDir;

                ReachedDestination = !_agent.pathPending &&
                                     _agent.remainingDistance != Mathf.Infinity &&
                                     _agent.remainingDistance <= Mathf.Max(arrivedThreshold, _agent.stoppingDistance);
            }
            else
            {
                DesiredMoveWorld = Vector3.zero;
                ReachedDestination = true;
            }
        }
    }
}
