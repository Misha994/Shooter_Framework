using System.Collections.Generic;
using UnityEngine;
using Zenject;

public class GrenadeInstaller : MonoInstaller
{
    [System.Serializable]
    public class GrenadeEntry
    {
        public GameObject prefab;
        public int startCount = 3;
    }

    [Header("Grenades")]
    [SerializeField] private List<GrenadeEntry> grenades = new();

    [Header("Defaults")]
    [SerializeField] private int startIndex = 0;

    public override void InstallBindings()
    {
        var prefabs = new List<GameObject>(grenades.Count);
        var counts = new List<int>(grenades.Count);

        foreach (var g in grenades)
        {
            if (g.prefab == null)
            {
                Debug.LogError("[GrenadeInstaller] One of grenade prefabs is null!");
                continue;
            }
            prefabs.Add(g.prefab);
            counts.Add(Mathf.Max(0, g.startCount));
        }

        Container.Bind<GrenadeInventory>()
                 .AsSingle()
                 .WithArguments(prefabs, counts, startIndex);
    }
}
