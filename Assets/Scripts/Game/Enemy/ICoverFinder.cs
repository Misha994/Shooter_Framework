using UnityEngine;

namespace Fogoyote.BFLike.AI.Navigation.Cover
{
    public interface ICoverFinder
    {
        bool TryFindCover(Vector3 origin, Transform threat, out Vector3 coverPos, out Vector3 coverNormal, out float score);
    }
}
