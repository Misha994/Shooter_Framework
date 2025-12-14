using UnityEngine;

public class WeaponMount : MonoBehaviour
{
    [SerializeField] Transform mountPoint; // той самий об’єкт, де висить компонент
    WeaponBase _current;

    public void Mount(WeaponBase w)
    {
        if (_current == w) return;
        if (_current) { _current.gameObject.SetActive(false); _current.OnHolstered(); }

        _current = w;

        if (_current)
        {
            _current.transform.SetParent(mountPoint ? mountPoint : transform, false);
            _current.transform.localPosition = Vector3.zero;
            _current.transform.localRotation = Quaternion.identity;
            _current.gameObject.SetActive(true);
            _current.OnEquipped();
        }
    }

    public WeaponBase Current => _current;
}
