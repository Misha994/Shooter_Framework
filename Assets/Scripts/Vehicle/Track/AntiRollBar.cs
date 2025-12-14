// SPDX-License-Identifier: MIT
using UnityEngine;

namespace TracksWC
{
    /// <summary>
    /// Простий анти-крєн: зрівнює стиснення лівої/правої сторони і додає сили в точки коліс.
    /// Додай на той самий GO, де Rigidbody.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class AntiRollBar : MonoBehaviour
    {
        public WheelCollider[] left;
        public WheelCollider[] right;

        [Tooltip("Жорсткість штанги. Збільшуй для “важчого” відчуття, менше крєну та підскакувань.")]
        public float antiRoll = 25000f;

        Rigidbody _rb;

        void Awake() => _rb = GetComponent<Rigidbody>();

        void FixedUpdate()
        {
            if (_rb == null || left == null || right == null) return;

            int pairs = Mathf.Min(left.Length, right.Length);
            for (int i = 0; i < pairs; i++)
            {
                var l = left[i];
                var r = right[i];
                if (!l || !r) continue;

                float travelL = 1f, travelR = 1f;

                bool groundedL = l.GetGroundHit(out var hitL);
                bool groundedR = r.GetGroundHit(out var hitR);

                if (groundedL)
                    travelL = (-l.transform.InverseTransformPoint(hitL.point).y - l.radius) / Mathf.Max(0.001f, l.suspensionDistance);
                if (groundedR)
                    travelR = (-r.transform.InverseTransformPoint(hitR.point).y - r.radius) / Mathf.Max(0.001f, r.suspensionDistance);

                float antiRollForce = (travelL - travelR) * antiRoll;

                if (groundedL)
                    _rb.AddForceAtPosition(l.transform.up * -antiRollForce, l.transform.position, ForceMode.Force);

                if (groundedR)
                    _rb.AddForceAtPosition(r.transform.up * antiRollForce, r.transform.position, ForceMode.Force);
            }
        }
    }
}
