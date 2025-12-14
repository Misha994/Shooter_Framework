using UnityEngine;

public class PooledObject : MonoBehaviour
{
    [HideInInspector] public GameObject SourcePrefab;

    // Викликається менеджером пулу
    public virtual void OnSpawned() { }
    public virtual void OnDespawned() { }

    /// <summary> Зручний хелпер для повернення в пул із коду. </summary>
    public void ReturnToPool(float delay = 0f)
    {
        if (PoolManager.Instance == null)
        {
            Destroy(gameObject);
            return;
        }
        PoolManager.Instance.Despawn(gameObject, delay);
    }
}
