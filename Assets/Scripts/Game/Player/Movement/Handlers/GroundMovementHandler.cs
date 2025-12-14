using UnityEngine;

public class GroundMovementHandler
{
    public Vector3 Calculate(Vector2 input, Transform cam)
    {
        Vector3 forward = cam.forward;
        Vector3 right = cam.right;

        // Убираємо вертикальні компоненти
        forward.y = 0f;
        right.y = 0f;

        // Нормалізуємо напрямки
        forward.Normalize();
        right.Normalize();

        // Рух по осях
        Vector3 move = forward * input.y + right * input.x;

        // Нормалізація вектора, щоб не було збільшення швидкості по діагоналі
        return move.sqrMagnitude > Mathf.Epsilon ? move.normalized : move;
    }
}
