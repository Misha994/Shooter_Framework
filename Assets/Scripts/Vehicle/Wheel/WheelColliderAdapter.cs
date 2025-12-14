// WheelColliderAdapter.cs
using UnityEngine;

/// <summary>
/// Thin, robust adapter around Unity's WheelCollider exposing a safe, testable API:
/// - Safe getters (ground hit, slips, compression)
/// - Setters for motor/brake/steer
/// - GetWorldPose wrapper
/// - Attempts to auto-assign WheelCollider on Reset/Awake to avoid inspector nulls.
/// </summary>
[DisallowMultipleComponent]
public class WheelColliderAdapter : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WheelCollider _col;

    // runtime cache of last hit (if any)
    private WheelHit _lastHit;
    private bool _lastGrounded;

    public WheelCollider Collider => _col;

    private void Reset()
    {
        // Try to auto-assign WheelCollider on same GameObject to avoid accidental UnassignedReferenceException.
        if (_col == null)
            _col = GetComponent<WheelCollider>();
    }

    private void Awake()
    {
        if (_col == null)
        {
            // attempt again, but do not throw here
            _col = GetComponentInChildren<WheelCollider>();
            if (_col == null)
                Debug.LogWarning($"[WheelColliderAdapter] WheelCollider not assigned on '{name}'. Please assign in inspector.", this);
        }
    }

    /// <summary>Safe wrapper around WheelCollider.GetGroundHit. Returns false if no collider or not grounded.</summary>
    public bool TryGetGroundHit(out WheelHit hit)
    {
        hit = default;
        if (_col == null) return false;
        // WheelCollider.GetGroundHit returns false if not grounded
        _lastGrounded = _col.GetGroundHit(out _lastHit);
        if (_lastGrounded) hit = _lastHit;
        return _lastGrounded;
    }

    /// <summary>Forward slip (absolute). 0 when not grounded or no collider.</summary>
    public float GetForwardSlip()
    {
        if (_col == null) return 0f;
        if (TryGetGroundHit(out var h)) return h.forwardSlip;
        return 0f;
    }

    /// <summary>Sideways slip (absolute). 0 when not grounded or no collider.</summary>
    public float GetSidewaysSlip()
    {
        if (_col == null) return 0f;
        if (TryGetGroundHit(out var h)) return h.sidewaysSlip;
        return 0f;
    }

    /// <summary>
    /// Normalized suspension compression: 0 = fully extended, 1 = fully compressed.
    /// Uses world center / hit.point to compute distance along wheel up axis - compatible with WheelHit.
    /// </summary>
    public float GetNormalizedCompression()
    {
        if (_col == null) return 0f;
        if (TryGetGroundHit(out var h))
        {
            // world center of the WheelCollider (taking center offset into account)
            Vector3 worldCenter = _col.transform.TransformPoint(_col.center);

            // distance from wheel center to contact along wheel up
            float distance = Vector3.Dot(worldCenter - h.point, _col.transform.up);

            float susDist = Mathf.Max(0.0001f, _col.suspensionDistance);
            distance = Mathf.Clamp(distance, 0f, susDist);

            float compression = 1f - (distance / susDist); // 0..1
            return Mathf.Clamp01(compression);
        }
        return 0f;
    }

    // ---- setters used by controller ----
    public void SetMotorTorque(float torque)
    {
        if (_col == null) return;
        _col.motorTorque = torque;
    }

    public void SetBrakeTorque(float torque)
    {
        if (_col == null) return;
        _col.brakeTorque = torque;
    }

    public void SetSteerAngle(float angle)
    {
        if (_col == null) return;
        _col.steerAngle = angle;
    }

    /// <summary>Wrapper for WheelCollider.GetWorldPose (safe fallback to transform if collider missing).</summary>
    public void GetWorldPose(out Vector3 pos, out Quaternion rot)
    {
        if (_col != null)
        {
            _col.GetWorldPose(out pos, out rot);
            return;
        }
        pos = transform.position;
        rot = transform.rotation;
    }
}
