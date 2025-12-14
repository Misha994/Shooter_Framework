// WheeledDriveController.cs
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Колісний контролер на основі WheelColliderAdapter.
/// - Ніякого прямого rb.AddTorque для керування поворотом (керування через WheelCollider steer + тертя).
/// - SetSeatInput не змінює enabled; SetDisabled контролює роботу.
/// - Простий traction control, anti-roll по парах ліво/право, lateral damping.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class WheeledDriveController : MonoBehaviour, ISeatInputConsumer, IDriveController
{
    [Serializable]
    public class WheelDefinition
    {
        public string name = "Wheel";
        public WheelColliderAdapter adapter; // REQUIRED
        public Transform visual;
        public bool steer = false;
        public bool drive = false;
        public bool brake = true;
        [Range(-180f, 180f)] public float steerOffset = 0f;
    }

    [Header("Wheels")]
    public WheelDefinition[] wheels = new WheelDefinition[0];

    [Header("Drive")]
    public float maxMotorTorque = 4000f;
    public float maxBrakeTorque = 6000f;
    public float maxSpeed = 30f; // m/s

    [Header("Steering")]
    public float maxSteerAngle = 35f;
    [Range(0.01f, 20f)] public float steerLerpSpeed = 8f;

    [Header("Traction")]
    [Range(0f, 1f)] public float slipThreshold = 0.3f;
    [Range(0f, 1f)] public float minWheelShareWhenSlip = 0.25f;
    public float slipDecaySpeed = 5f;
    public float slipRecoverSpeed = 2f;

    [Header("Stability")]
    public float antiRollStiffness = 5000f;
    [Range(0f, 1f)] public float lateralDamping = 0.35f;

    [Header("Visual / fallback input")]
    public bool updateVisuals = true;
    [SerializeField] private MonoBehaviour diInputFallback; // optional: IInputService

    // runtime
    private Rigidbody _rb;
    private IInputService _input;
    private float[] _targetSteer;
    private float _currentWheelShare = 1f;
    private bool _disabled = false;

    public bool IsActive => enabled && !_disabled;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        if (_rb == null) _rb = GetComponentInParent<Rigidbody>();
        if (wheels == null) wheels = new WheelDefinition[0];
        _targetSteer = new float[wheels.Length];
    }

    void Start()
    {
        for (int i = 0; i < wheels.Length; i++)
            if (wheels[i] != null && wheels[i].adapter == null)
                Debug.LogWarning($"[WheeledDrive] wheel {i} adapter not assigned.", this);
    }

    // ISeatInputConsumer (не змінюємо enabled автоматично)
    public void SetSeatInput(IInputService input) => _input = input;
    public void SetInput(IInputService input) => SetSeatInput(input);

    // IDriveController
    public void KillDrive()
    {
        if (wheels == null) return;
        foreach (var w in wheels)
        {
            if (w == null || w.adapter == null) continue;
            w.adapter.SetMotorTorque(0f);
            w.adapter.SetBrakeTorque(maxBrakeTorque);
        }
        if (_rb != null)
        {
            _rb.velocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    public void SetDisabled(bool disabled)
    {
        _disabled = disabled;
        if (disabled) KillDrive();
    }

    void FixedUpdate()
    {
        if (_disabled || _rb == null) return;

        var inputSource = _input ?? (diInputFallback as IInputService);
        Vector2 move = inputSource != null ? inputSource.GetMoveAxis() : Vector2.zero;
        float throttle = Mathf.Clamp(move.y, -1f, 1f);
        float steer = Mathf.Clamp(move.x, -1f, 1f);

        float speed = _rb.velocity.magnitude;
        float speedK = Mathf.Clamp01(1f - (speed / Mathf.Max(0.0001f, maxSpeed)));

        // traction average forward slip across drive wheels
        float avgSlip = 0f; int slipCount = 0;
        for (int i = 0; i < wheels.Length; i++)
        {
            var w = wheels[i];
            if (w == null || w.adapter == null) continue;
            if (!w.drive) continue;
            avgSlip += Mathf.Abs(w.adapter.GetForwardSlip()); slipCount++;
        }
        avgSlip = slipCount > 0 ? avgSlip / slipCount : 0f;

        if (avgSlip > slipThreshold)
            _currentWheelShare = Mathf.MoveTowards(_currentWheelShare, minWheelShareWhenSlip, slipDecaySpeed * Time.fixedDeltaTime);
        else
            _currentWheelShare = Mathf.MoveTowards(_currentWheelShare, 1f, slipRecoverSpeed * Time.fixedDeltaTime);

        float motorInput = throttle * _currentWheelShare;
        float motorTorqueValue = motorInput * maxMotorTorque * speedK;

        int driveCount = 0;
        for (int i = 0; i < wheels.Length; i++) if (wheels[i] != null && wheels[i].drive && wheels[i].adapter != null) driveCount++;
        driveCount = Math.Max(1, driveCount);

        bool applyingBrake = Mathf.Abs(throttle) < 0.05f || (throttle * Vector3.Dot(_rb.velocity, transform.forward) < -0.1f);

        for (int i = 0; i < wheels.Length; i++)
        {
            var w = wheels[i];
            if (w == null || w.adapter == null) continue;

            // steering
            if (w.steer)
            {
                float desired = steer * maxSteerAngle;
                _targetSteer[i] = Mathf.MoveTowards(_targetSteer[i], desired, steerLerpSpeed * Time.fixedDeltaTime * Mathf.Abs(desired - _targetSteer[i]));
                w.adapter.SetSteerAngle(_targetSteer[i] + w.steerOffset);
            }
            else
            {
                _targetSteer[i] = 0f;
                w.adapter.SetSteerAngle(w.steerOffset);
            }

            // motor
            if (w.drive)
            {
                float forwardSlip = Mathf.Abs(w.adapter.GetForwardSlip());
                float slipFactor = 1f;
                if (forwardSlip > slipThreshold) slipFactor = Mathf.Clamp01(1f - (forwardSlip - slipThreshold) * 5f);
                float applyTorque = (motorTorqueValue / driveCount) * slipFactor;
                w.adapter.SetMotorTorque(applyTorque);
            }
            else
            {
                w.adapter.SetMotorTorque(0f);
            }

            // brakes
            w.adapter.SetBrakeTorque((applyingBrake && w.brake) ? maxBrakeTorque : 0f);

            // visuals
            if (updateVisuals && w.visual != null)
            {
                // adapter.GetWorldPose already returns wheel world rotation including steering & roll
                w.adapter.GetWorldPose(out Vector3 pos, out Quaternion rot);
                w.visual.position = pos;

                // Apply only fixed "steerOffset" to compensate model orientation,
                // DO NOT re-apply dynamic steer angle (rot already contains it).
                Vector3 upAxis = (w.adapter != null) ? w.adapter.transform.up : Vector3.up;
                Quaternion offset = Quaternion.AngleAxis(w.steerOffset, upAxis);
                w.visual.rotation = rot * offset;
            }
        }

        ApplyAntiRoll();
        ApplyLateralDamping();
    }

    private void ApplyAntiRoll()
    {
        if (antiRollStiffness <= 0f || wheels == null || wheels.Length < 2) return;

        for (int i = 0; i < wheels.Length; i++)
        {
            var wi = wheels[i];
            if (wi == null || wi.adapter == null) continue;
            Vector3 localI = transform.InverseTransformPoint(wi.adapter.transform.position);

            int bestJ = -1; float bestDist = float.MaxValue;
            for (int j = 0; j < wheels.Length; j++)
            {
                if (j == i) continue;
                var wj = wheels[j];
                if (wj == null || wj.adapter == null) continue;
                Vector3 localJ = transform.InverseTransformPoint(wj.adapter.transform.position);
                if (Mathf.Sign(localI.x) == Mathf.Sign(localJ.x)) continue;
                float dist = Mathf.Abs(localI.z - localJ.z);
                if (dist < bestDist) { bestDist = dist; bestJ = j; }
            }
            if (bestJ < 0) continue;

            var wj2 = wheels[bestJ];
            float compI = wi.adapter.GetNormalizedCompression();
            float compJ = wj2.adapter.GetNormalizedCompression();
            float anti = (compI - compJ) * antiRollStiffness;
            Vector3 force = transform.up * -anti;
            Vector3 posI = wi.adapter.transform.position;
            Vector3 posJ = wj2.adapter.transform.position;

            if (_rb != null)
            {
                _rb.AddForceAtPosition(force, posI, ForceMode.Force);
                _rb.AddForceAtPosition(-force, posJ, ForceMode.Force);
            }
        }
    }

    private void ApplyLateralDamping()
    {
        if (_rb == null || lateralDamping <= 0f) return;
        Vector3 vel = _rb.velocity;
        Vector3 planar = Vector3.ProjectOnPlane(vel, Vector3.up);
        Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 forwardVel = Vector3.Project(planar, fwd);
        Vector3 sideVel = planar - forwardVel;
        _rb.velocity = forwardVel + Vector3.Lerp(sideVel, Vector3.zero, lateralDamping) + Vector3.Project(vel, Vector3.up);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (wheels == null) return;
        Gizmos.color = Color.yellow;
        for (int i = 0; i < wheels.Length; i++)
        {
            var w = wheels[i];
            if (w == null || w.adapter == null) continue;
            if (w.adapter.Collider == null) continue;
            Vector3 worldCenter = w.adapter.Collider.transform.TransformPoint(w.adapter.Collider.center);
            Gizmos.DrawWireSphere(worldCenter, 0.05f);
        }
    }
#endif
}
