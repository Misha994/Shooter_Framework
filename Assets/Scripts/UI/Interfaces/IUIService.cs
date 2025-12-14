using Game.UI;

namespace Game.Core
{
    public interface IUIService
    {
        void ShowView(UIViewId viewId, object data = null);
        void HideView(UIViewId viewId);
        void ShowModal(UIViewId viewId, object data = null);
        void HideModal();
        void ShowPopup(UIViewId viewId, object data = null);
        void HidePopup();
        void ClearAllViews();
    }
}