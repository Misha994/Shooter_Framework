using Zenject;
using UnityEngine;
using Game.UI;

public class UIFactory : IFactory<UIViewId, IUIView>
{
    private readonly DiContainer _container;
    private readonly UIConfig _config;
    private readonly Transform _windowsRoot;
    private readonly Transform _modalsRoot;
    private readonly Transform _popupsRoot;

    public UIFactory(
        DiContainer container,
        UIConfig config,
        [Inject(Id = "WindowsRoot")] Transform windowsRoot,
        [Inject(Id = "ModalsRoot")] Transform modalsRoot,
        [Inject(Id = "PopupsRoot")] Transform popupsRoot)
    {
        _container = container;
        _config = config;
        _windowsRoot = windowsRoot;
        _modalsRoot = modalsRoot;
        _popupsRoot = popupsRoot;
    }

    public IUIView Create(UIViewId viewId)
    {
        foreach (var viewConfig in _config.Views)
        {
            if (viewConfig.ViewId == viewId)
            {
                Transform parent = viewConfig.ViewType switch
                {
                    UIViewType.Window => _windowsRoot,
                    UIViewType.Modal => _modalsRoot,
                    UIViewType.Popup => _popupsRoot,
                    _ => _windowsRoot
                };

                var instance = _container.InstantiatePrefab(viewConfig.Prefab, parent);
                var view = instance.GetComponent<IUIView>();
                if (view != null) return view;

                Object.Destroy(instance);
                return null;
            }
        }

        Debug.LogError($"View with ID {viewId} not found in UIConfig.");
        return null;
    }
}
