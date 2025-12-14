using UnityEngine;

public class GrenadeFireHandler : IFireHandler
{
    private readonly GameObject projectilePrefab;
    private readonly float projectileForce;
    private readonly Transform throwPoint;

    public GrenadeFireHandler(GameObject prefab, float force, Transform throwOrigin)
    {
        projectilePrefab = prefab;
        projectileForce = force;
        throwPoint = throwOrigin;
    }

    public void ExecuteFire(Transform _, Vector3 direction)
    {
        if (projectilePrefab == null || throwPoint == null)
        {
            Debug.LogError("[GrenadeFireHandler] Prefab or throwPoint is NULL!");
            return;
        }

        var grenade = PoolManager.Instance
            ? PoolManager.Instance.Spawn(projectilePrefab, throwPoint.position, Quaternion.LookRotation(direction))
            : Object.Instantiate(projectilePrefab, throwPoint.position, Quaternion.LookRotation(direction));

        if (grenade.TryGetComponent<Rigidbody>(out var rb))
            rb.AddForce(direction * projectileForce, ForceMode.Impulse);
    }
}
