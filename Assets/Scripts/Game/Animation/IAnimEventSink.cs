public interface IAnimEventSink
{
    void OnAnimEvent(string evt);          // "ReloadCommit", "ReloadEnd", "Fire", ...
}
