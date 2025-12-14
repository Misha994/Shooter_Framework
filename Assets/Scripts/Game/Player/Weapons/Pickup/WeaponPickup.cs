using UnityEngine;

[RequireComponent(typeof(Collider))]
public class WeaponPickup : MonoBehaviour
{
    public WeaponConfig WeaponConfig;

    private void Reset()
    {
        // Автоматично вмикаємо триггер на колайдері
        var col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Перевіряємо, чи в об'єкта є хендлер підбору зброї
        if (other.TryGetComponent<IWeaponPickupHandler>(out var handler))
        {
            handler.TryPickupWeapon(this);
        }
    }
}
