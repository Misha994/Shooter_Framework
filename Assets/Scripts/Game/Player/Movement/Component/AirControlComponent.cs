using UnityEngine;

public class AirControlComponent : MonoBehaviour
{
    [SerializeField] private float airControlFactor = 0.15f;
    private Vector3 _airMomentum;

    public void UpdateAirControl(Vector2 input, ref Vector3 horizontalVelocity)
    {
        if (input == Vector2.zero) return;

        Vector3 inputDir = GetInputDirection(input);
        _airMomentum = Vector3.Lerp(_airMomentum, inputDir * _airMomentum.magnitude, airControlFactor * Time.deltaTime);

        horizontalVelocity = _airMomentum;
    }

    private Vector3 GetInputDirection(Vector2 input)
    {
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;
        forward.y = right.y = 0f;

        forward.Normalize();
        right.Normalize();

        return (forward * input.y + right * input.x).normalized;
    }
}
