using UnityEngine;

namespace Fogoyote.BFLike.AI
{
    public abstract class AIStrategyBase : ScriptableObject, IAIStrategy
    {
        public abstract bool Evaluate(ref AIContext ctx, out AIIntent intent, out float utility);
    }
}
