using UnityEngine;

public class JumpComponent : MonoBehaviour
{
    [SerializeField] private float jumpForce = 7f;

    private float _yVelocity;

    public float VerticalVelocity => _yVelocity;

    public void Jump(ref float yVelocity)
    {
        yVelocity = jumpForce;
    }
}
