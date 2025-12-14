using UnityEngine;

public class RecoilKick : MonoBehaviour
{
    [SerializeField] private Transform recoilEffector;
    [SerializeField] private float posKick = 0.015f;  // відкат назад
    [SerializeField] private float rotKick = 2.5f;    // кивок вгору (градуси)
    [SerializeField] private float returnSpeed = 12f;

    private Vector3 _pos0; private Quaternion _rot0; private Vector3 _offset; private float _rotX;

    void Awake() { if (recoilEffector) { _pos0 = recoilEffector.localPosition; _rot0 = recoilEffector.localRotation; } }
    public void OnFired()
    {
        _offset.z -= posKick;
        _rotX -= rotKick;
    }

    void LateUpdate()
    {
        if (!recoilEffector) return;
        _offset = Vector3.Lerp(_offset, Vector3.zero, 1 - Mathf.Exp(-returnSpeed * Time.deltaTime));
        _rotX = Mathf.Lerp(_rotX, 0f, 1 - Mathf.Exp(-returnSpeed * Time.deltaTime));
        recoilEffector.localPosition = _pos0 + _offset;
        recoilEffector.localRotation = _rot0 * Quaternion.Euler(_rotX, 0, 0);
    }
}
