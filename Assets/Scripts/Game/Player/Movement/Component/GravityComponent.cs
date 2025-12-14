using UnityEngine;

public class GravityComponent : MonoBehaviour
{
    [SerializeField] private float gravity = 20f;

    public void ApplyGravity(ref float verticalVelocity, bool isGrounded)
    {
        if (isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f; // Легкий "прилипання" до землі
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime;
        }
    }
}
