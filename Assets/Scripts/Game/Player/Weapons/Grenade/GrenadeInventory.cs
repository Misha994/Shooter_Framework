using System.Collections.Generic;
using UnityEngine;

public class GrenadeInventory
{
    private readonly List<GameObject> _prefabs;
    private readonly List<int> _counts;
    private int _currentIndex;

    public GrenadeInventory(List<GameObject> prefabs, List<int> startCounts, int startIndex = 0)
    {
        _prefabs = prefabs ?? new List<GameObject>();
        _counts = startCounts ?? new List<int>();

        // ¬ир≥внюЇмо довжини
        while (_counts.Count < _prefabs.Count) _counts.Add(0);
        if (_prefabs.Count == 0)
            Debug.LogError("[GrenadeInventory] Prefab list is empty!");

        _currentIndex = Mathf.Clamp(startIndex, 0, Mathf.Max(0, _prefabs.Count - 1));
    }

    public int TypesCount => _prefabs.Count;
    public int CurrentIndex => _currentIndex;
    public GameObject CurrentPrefab => IsValidIndex(_currentIndex) ? _prefabs[_currentIndex] : null;
    public int CurrentCount => IsValidIndex(_currentIndex) ? _counts[_currentIndex] : 0;

    public int TotalCount
    {
        get
        {
            int sum = 0;
            for (int i = 0; i < _counts.Count; i++) sum += _counts[i];
            return sum;
        }
    }

    public bool HasAny => TotalCount > 0;
    public bool HasCurrent => CurrentCount > 0;

    public void SwitchNext()
    {
        if (_prefabs.Count == 0) return;

        int start = _currentIndex;
        do
        {
            _currentIndex = (_currentIndex + 1) % _prefabs.Count;
            // якщо хочемо пропускати порожн≥ стеки, розкоментуй:
            // if (_counts[_currentIndex] > 0) break;
            if (_currentIndex == start) break; // об≥йшли коло
        } while (true);

        Debug.Log($"[GrenadeInventory] Switched to {_currentIndex} (count: {CurrentCount})");
    }

    public void SwitchPrev()
    {
        if (_prefabs.Count == 0) return;

        int start = _currentIndex;
        do
        {
            _currentIndex = (_currentIndex - 1 + _prefabs.Count) % _prefabs.Count;
            // if (_counts[_currentIndex] > 0) break;
            if (_currentIndex == start) break;
        } while (true);

        Debug.Log($"[GrenadeInventory] Switched to {_currentIndex} (count: {CurrentCount})");
    }

    public IGrenade TakeCurrent(Transform spawnPoint)
    {
        if (!IsValidIndex(_currentIndex))
        {
            Debug.LogWarning("[GrenadeInventory] Invalid current index.");
            return null;
        }

        if (_counts[_currentIndex] <= 0)
        {
            Debug.LogWarning("[GrenadeInventory] No grenades left for current type.");
            return null;
        }

        var prefab = _prefabs[_currentIndex];
        if (prefab == null)
        {
            Debug.LogError("[GrenadeInventory] Current prefab is null.");
            return null;
        }

        _counts[_currentIndex]--;

        var go = Object.Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
        var grenade = go.GetComponent<IGrenade>();
        if (grenade == null)
        {
            Debug.LogError("[GrenadeInventory] Prefab has no IGrenade component.");
        }

        return grenade;
    }

    public void AddToType(int typeIndex, int amount)
    {
        if (!IsValidIndex(typeIndex)) return;
        _counts[typeIndex] = Mathf.Max(0, _counts[typeIndex] + Mathf.Max(0, amount));
    }

    public void AddAll(int amountEach)
    {
        for (int i = 0; i < _counts.Count; i++)
            _counts[i] = Mathf.Max(0, _counts[i] + Mathf.Max(0, amountEach));
    }

    private bool IsValidIndex(int idx) => idx >= 0 && idx < _prefabs.Count;
}
