using UnityEngine;

public abstract class GrenadeBase : MonoBehaviour, IGrenade
{
    [SerializeField] protected Rigidbody rb;
    [SerializeField] protected float lifeTime = 5f;

    public virtual void Throw(Vector3 position, Vector3 direction, float force)
    {
        transform.position = position;
        gameObject.SetActive(true);

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(direction * force, ForceMode.Impulse);
        }

        Invoke(nameof(Explode), lifeTime);
    }

    protected abstract void Explode();
}
