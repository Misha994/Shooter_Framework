// NEW: IPoolManager.cs
using UnityEngine;

public interface IPoolManager
{
    void Warmup(GameObject prefab, int count, Transform parent = null);
    GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null);
    void Despawn(GameObject instance, float delaySeconds = 0f);
}
