using System.Collections.Generic;
using UnityEngine;
using Zenject;
using Game.Core;
using Game.UI;
using Game.Core.Interfaces;

public class UIService : IUIService
{
    private readonly SignalBus _signalBus;
    private readonly UIFactory _uiFactory;
    private readonly ISceneLoader _sceneLoader;
    private readonly DiContainer _container;
    private readonly Dictionary<UIViewId, IUIView> _views;
    private readonly Stack<IUIView> _modalStack;
    private readonly Stack<IUIView> _popupStack;
    private readonly Dictionary<UIViewId, List<IUIView>> _viewPool;
    private readonly ISceneRouter _sceneRouter;

    [Inject]
    public UIService(SignalBus signalBus, UIFactory uiFactory, ISceneLoader sceneLoader, DiContainer container, ISceneRouter sceneRouter)
    {
        _signalBus = signalBus;
        _uiFactory = uiFactory;
        _sceneLoader = sceneLoader;
        _container = container;
        _views = new();
        _modalStack = new();
        _popupStack = new();
        _viewPool = new();
        _sceneRouter = sceneRouter;

        _signalBus.Subscribe<PlayerHealthChangedSignal>(OnPlayerHealthChanged);
        _signalBus.Subscribe<GameStartedSignal>(OnGameStarted);
        _signalBus.Subscribe<PlayerDiedSignal>(OnPlayerDied);
        _signalBus.Subscribe<SceneLoadedSignal>(OnSceneLoaded);
        _signalBus.Subscribe<SceneReloadRequestedSignal>(OnSceneReloadRequested);
    }

    public void ShowView(UIViewId viewId, object data = null)
    {
        if (!_views.ContainsKey(viewId))
        {
            var view = GetFromPool(viewId) ?? _uiFactory.Create(viewId);
            if (view != null)
                _views[viewId] = view;
            else
                return;
        }

        if (_modalStack.Count > 0 || _popupStack.Count > 0)
            return;

        _views[viewId].Show(data);
    }

    public void HideView(UIViewId viewId)
    {
        if (_views.TryGetValue(viewId, out var view))
            view.Hide();
    }

    public void ShowModal(UIViewId viewId, object data = null)
    {
        if (!_views.ContainsKey(viewId))
        {
            var view = GetFromPool(viewId) ?? _uiFactory.Create(viewId);
            if (view != null && view.ViewType == UIViewType.Modal)
                _views[viewId] = view;
            else
                return;
        }

        var modal = _views[viewId];
        if (_popupStack.Count > 0) return;
        _modalStack.Push(modal);
        modal.Show(data);
    }

    public void HideModal()
    {
        if (_modalStack.Count > 0)
            _modalStack.Pop().Hide();
    }

    public void ShowPopup(UIViewId viewId, object data = null)
    {
        if (!_views.ContainsKey(viewId))
        {
            var view = GetFromPool(viewId) ?? _uiFactory.Create(viewId);
            if (view != null && view.ViewType == UIViewType.Popup)
                _views[viewId] = view;
            else
                return;
        }

        var popup = _views[viewId];
        _popupStack.Push(popup);
        popup.Show(data);
    }

    public void HidePopup()
    {
        if (_popupStack.Count > 0)
            _popupStack.Pop().Hide();
    }

    public void ClearAllViews()
    {
        foreach (var view in _views.Values)
        {
            if (view is MonoBehaviour mono)
            {
                mono.gameObject.SetActive(false);
                if (!_viewPool.ContainsKey(view.ViewId))
                    _viewPool[view.ViewId] = new();
                _viewPool[view.ViewId].Add(view);
            }
        }

        _views.Clear();
        _modalStack.Clear();
        _popupStack.Clear();
    }

    private IUIView GetFromPool(UIViewId id)
    {
        if (_viewPool.TryGetValue(id, out var pool) && pool.Count > 0)
        {
            var view = pool[0];
            pool.RemoveAt(0);
            if (view is MonoBehaviour mono)
            {
                Transform parent = view.ViewType switch
                {
                    UIViewType.Window => _container.ResolveId<Transform>("WindowsRoot"),
                    UIViewType.Modal => _container.ResolveId<Transform>("ModalsRoot"),
                    UIViewType.Popup => _container.ResolveId<Transform>("PopupsRoot"),
                    _ => null
                };
                mono.transform.SetParent(parent, false);
                mono.gameObject.SetActive(true);
            }
            return view;
        }
        return null;
    }

    private void OnGameStarted()
    {
        Debug.Log("GameStarted: Hiding MainMenu, Showing GameUI");
        HideView(UIViewId.MainMenu);
        ShowView(UIViewId.GameUI);
        _sceneRouter.GoTo(GameScene.Game);
    }

    private void OnPlayerDied()
    {
        HideView(UIViewId.GameUI);
        ShowModal(UIViewId.GameOverModal, "GameOver");
    }

    private void OnSceneLoaded(SceneLoadedSignal signal)
    {
        ClearAllViews();
        switch (signal.SceneName)
        {
            case "MainMenu":
                ShowView(UIViewId.MainMenu, "MainMenuTitle");
                break;
            case "Game":
                ShowView(UIViewId.GameUI);
                ShowPopup(UIViewId.InfoPopup, "InfoPopup");
                break;
        }
    }

    private void OnPlayerHealthChanged(PlayerHealthChangedSignal signal)
    {
        if (_views.TryGetValue(UIViewId.GameUI, out var view) && view is GameUIView gameUI)
        {
            gameUI.UpdateHealth(signal.Health);
        }
    }

    private void OnSceneReloadRequested(SceneReloadRequestedSignal signal)
    {
        Debug.Log($"Reload requested: going to scene {signal.TargetScene}");
        ClearAllViews();
        _sceneRouter.GoTo(signal.TargetScene);
    }
}
