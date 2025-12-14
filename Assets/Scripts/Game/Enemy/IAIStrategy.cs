namespace Fogoyote.BFLike.AI
{
    public interface IAIStrategy
    {
        bool Evaluate(ref AIContext ctx, out AIIntent intent, out float utility);
    }
}
