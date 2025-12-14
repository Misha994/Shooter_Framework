using UnityEngine;

public class CasingAutoReturn : MonoBehaviour
{
    private float _lifetime = 3f;

    // Сумісність зі старим підписом
    public void Begin(GameObject _ignoredPrefabKey, float lifetime) => Begin(lifetime);

    public void Begin(float lifetime)
    {
        _lifetime = Mathf.Max(0.1f, lifetime);
        CancelInvoke(nameof(Dispose));
        Invoke(nameof(Dispose), _lifetime);
    }

    private void Dispose()
    {
        if (TryGetComponent<Rigidbody>(out var rb))
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // Повертаємо у пул, якщо він є
        if (TryGetComponent<PooledObject>(out var po))
            po.ReturnToPool();
        else if (PoolManager.Instance != null)
            PoolManager.Instance.Despawn(gameObject);
        else
            Destroy(gameObject);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(Dispose));
    }
}
