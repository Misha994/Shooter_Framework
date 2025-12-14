using Fogoyote.BFLike.AI.Navigation.Cover;
using UnityEngine;

namespace Fogoyote.BFLike.AI
{
    [CreateAssetMenu(menuName = "AI/Strategies/TakeCover")]
    public class TakeCoverStrategy : AIStrategyBase
    {
        [SerializeField] private float triggerDistance = 22f;
        [SerializeField] private float minHealthToIgnore = 0.65f;
        [SerializeField] private float utilityBias = 0.55f;

        public override bool Evaluate(ref AIContext ctx, out AIIntent intent, out float utility)
        {
            intent = default; utility = 0f;
            if (!ctx.Target) return false;

            bool shouldSeek = ctx.TargetDistance < triggerDistance || ctx.Health01 < minHealthToIgnore;
            if (!shouldSeek) return false;

            var finder = ctx.Self.GetComponentInParent<ICoverFinder>();
            if (finder != null && finder.TryFindCover(ctx.Self.position, ctx.Target, out var coverPos, out _, out var score))
            {
                utility = Mathf.Clamp01(score + utilityBias);
                intent = new AIIntent
                {
                    HasNavDestination = true,
                    NavDestination = coverPos,
                    WantsCover = true,
                    Target = ctx.Target,
                    Aim = false,
                    Fire = false,
                    Sprint = true,
                    MoveDir = (coverPos - ctx.Self.position).normalized
                };
                return true;
            }
            return false;
        }
    }
}
