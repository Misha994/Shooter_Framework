using UnityEngine;
using DG.Tweening;
using Game.UI;

public abstract class UIViewBase : MonoBehaviour, IUIView
{
    [SerializeField] protected CanvasGroup canvasGroup;

    public abstract UIViewId ViewId { get; }
    public abstract UIViewType ViewType { get; }

    protected virtual void Awake()
    {
        if (canvasGroup == null)
        {
            Debug.LogError($"{GetType().Name}: CanvasGroup is not assigned.");
        }
    }

    public virtual void Show(object data = null)
    {
        if (canvasGroup == null)
        {
            Debug.LogError($"{GetType().Name}: CanvasGroup is not assigned.");
            return;
        }

        gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, 0.5f);
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
    }

    public virtual void Hide()
    {
        if (canvasGroup == null)
        {
            Debug.LogError($"{GetType().Name}: CanvasGroup is not assigned.");
            return;
        }

        canvasGroup.DOFade(0f, 0.5f).OnComplete(() =>
        {
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        });
    }
}
