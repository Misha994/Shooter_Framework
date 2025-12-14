using UnityEngine;

namespace Fogoyote.BFLike.AI
{
    [CreateAssetMenu(menuName = "AI/Strategies/Offensive")]
    public class OffensiveStrategy : AIStrategyBase
    {
        [SerializeField] private float optimalRange = 25f;

        public override bool Evaluate(ref AIContext ctx, out AIIntent intent, out float utility)
        {
            intent = default; utility = 0f;
            if (!ctx.Target) return false;

            float rangeScore = 1f - Mathf.Clamp01(Mathf.Abs(ctx.TargetDistance - optimalRange) / Mathf.Max(0.001f, optimalRange));
            utility = 0.6f * rangeScore + 0.4f;

            var to = (ctx.Target.position - ctx.Self.position).normalized;
            var strafe = Vector3.Cross(Vector3.up, to).normalized;
            var desired = (rangeScore < 0.5f) ? -to : strafe;

            intent = new AIIntent
            {
                MoveDir = desired,
                Sprint = rangeScore < 0.35f,
                Aim = true,
                Fire = true,
                Target = ctx.Target,
                HasNavDestination = false
            };
            return true;
        }
    }
}
