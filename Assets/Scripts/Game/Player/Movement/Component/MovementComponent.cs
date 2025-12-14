using UnityEngine;

public class MovementComponent : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float sprintSpeed = 12f;

    private CharacterController _controller;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    public void Move(Vector2 input, bool isSprinting)
    {
        Vector3 direction = GetInputDirection(input);
        float speed = isSprinting ? sprintSpeed : moveSpeed;
        _controller.Move(direction * speed * Time.deltaTime);
    }

    private Vector3 GetInputDirection(Vector2 input)
    {
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;
        forward.y = 0f; // Забираємо вертикальний компонент
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        return (forward * input.y + right * input.x).normalized;
    }
}
