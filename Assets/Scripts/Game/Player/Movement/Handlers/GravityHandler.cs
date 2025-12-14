using UnityEngine;

public class GravityHandler
{
    private float _gravity;

    public GravityHandler(float gravity)
    {
        _gravity = gravity;
    }

    public float ApplyGravity(float yVelocity, bool isGrounded)
    {
        if (isGrounded && yVelocity < 0)
            return -1f;
        return yVelocity - _gravity * Time.deltaTime;
    }
}
