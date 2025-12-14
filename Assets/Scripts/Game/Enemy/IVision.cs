using UnityEngine;

namespace Fogoyote.BFLike.AI.Perception
{
    public interface IVision
    {
        bool TryGetBestVisibleEnemy(out Transform target, out float score);
    }
}
