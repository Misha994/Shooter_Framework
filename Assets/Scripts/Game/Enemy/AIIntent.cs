using UnityEngine;

namespace Fogoyote.BFLike.AI
{
    public struct AIIntent
    {
        public Vector3 MoveDir;
        public bool Sprint, Aim, Fire;
        public Transform Target;

        public bool HasNavDestination;
        public Vector3 NavDestination;
        public bool WantsCover;
    }
}
