namespace Game.UI
{
    public interface IUIView
    {
        UIViewId ViewId { get; }
        UIViewType ViewType { get; }
        void Show(object data = null);
        void Hide();
    }
}