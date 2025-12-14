using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CasingShell : PooledObject
{
    [SerializeField] private float lifetime = 4f;

    public override void OnSpawned()
    {
        CancelInvoke(nameof(ReturnNow));
        Invoke(nameof(ReturnNow), lifetime);
    }

    private void ReturnNow()
    {
        ReturnToPool();
    }
}
