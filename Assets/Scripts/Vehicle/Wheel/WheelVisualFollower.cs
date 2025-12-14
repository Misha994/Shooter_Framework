// Assets/Scripts/Vehicle/Wheel/WheelVisualFollower.cs
using TracksPhysics;
using UnityEngine;

/// <summary>
/// Позиціонує візуальне колесо по контакту підвіски. Працює з TrackSuspension.ContactInfo.
/// </summary>
public class WheelVisualFollower : MonoBehaviour
{
    public enum Side { Left, Right }

    [Header("Refs")]
    public TrackSuspension suspension;
    public Side side = Side.Left;
    [Tooltip("Індекс відповідного якоря в leftPoints/rightPoints")]
    public int pointIndex = 0;

    [Header("Visual tuning")]
    [Tooltip("Якщо pivot меша не в геометричному центрі — підкрути це")]
    public Vector3 localOffset = Vector3.zero;
    public bool alignRotationToAnchor = true;

    [Header("Airborne")]
    [Tooltip("Коли немає контакту: true → повний дріп (rest+travel), false → на rest")]
    public bool droopWhenAirborne = true;

    [Header("Debug")]
    public bool drawCenterGizmo = false;
    public float gizmoRadius = 0.05f;

    void LateUpdate()
    {
        if (!suspension) return;

        // 1) якір
        Transform anchor = null;
        if (side == Side.Left)
        {
            if (suspension.leftPoints == null || pointIndex < 0 || pointIndex >= suspension.leftPoints.Length) return;
            anchor = suspension.leftPoints[pointIndex];
        }
        else
        {
            if (suspension.rightPoints == null || pointIndex < 0 || pointIndex >= suspension.rightPoints.Length) return;
            anchor = suspension.rightPoints[pointIndex];
        }
        if (!anchor) return;

        float rest = Mathf.Max(0.001f, suspension.restLength);
        float trav = Mathf.Max(0.001f, suspension.travel);
        float R = Mathf.Max(0.001f, suspension.wheelRadius);

        // 2) контакт
        TracksPhysics.TrackSuspension.ContactInfo contact = default;
        bool grounded = false;
        if (side == Side.Left)
        {
            var arr = suspension.LeftContacts;
            grounded = arr != null && pointIndex >= 0 && pointIndex < arr.Length && arr[pointIndex].grounded;
            if (grounded) contact = arr[pointIndex];
        }
        else
        {
            var arr = suspension.RightContacts;
            grounded = arr != null && pointIndex >= 0 && pointIndex < arr.Length && arr[pointIndex].grounded;
            if (grounded) contact = arr[pointIndex];
        }

        // 3) центр сфери
        Vector3 center;
        if (grounded) center = contact.point + contact.normal * R;
        else center = anchor.position - anchor.up * (droopWhenAirborne ? (rest + trav) : rest);

        // 4) локальний офсет
        center += anchor.rotation * localOffset;

        // 5) застосування
        transform.position = center;

        if (alignRotationToAnchor)
        {
            Vector3 up = anchor.up;
            Vector3 fwd = Vector3.ProjectOnPlane(anchor.forward, up);
            if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.Cross(up, anchor.right);
            transform.rotation = Quaternion.LookRotation(fwd.normalized, up);
        }

        if (drawCenterGizmo) Debug.DrawLine(center, center + anchor.up * 0.2f, Color.cyan);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!drawCenterGizmo) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, gizmoRadius);
    }
#endif
}
