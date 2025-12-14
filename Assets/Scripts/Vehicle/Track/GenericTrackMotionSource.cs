// Assets/Scripts/Vehicle/TracksWC/GenericTrackMotionSourceWC.cs
// SPDX-License-Identifier: MIT
using UnityEngine;

namespace TracksWC
{
    /// <summary>
    /// Оцінює лінійні швидкості бортів (ліва/права гусениця) із Rigidbody та єдиної рамки-орієнтиру.
    /// frame визначає, куди вважаємо "forward" і "up".
    /// v_fwd = проєкція швидкості на forward; yawRate = проєкція кутової швидкості на up.
    /// v_side = yawRate * halfTrack; left = v_fwd - v_side, right = v_fwd + v_side.
    /// </summary>
    [DisallowMultipleComponent]
    public class GenericTrackMotionSource : MonoBehaviour, ITrackMotionSource
    {
        public enum Axis
        {
            X, Y, Z, NegX, NegY, NegZ
        }

        [Header("Frame & RB")]
        [Tooltip("Рамка, яка задає напрямки forward/up. Поверни її як потрібно.")]
        public Transform frame;         // один об’єкт-орієнтир
        public Rigidbody rootRb;        // Rigidbody танка

        [Header("Axis mapping (локальні осі frame)")]
        [Tooltip("Яку локальну вісь frame вважати 'forward' (напрям руху).")]
        public Axis forwardAxis = Axis.Z;
        [Tooltip("Яку локальну вісь frame вважати 'up' (вісь обертання по курсу).")]
        public Axis upAxis = Axis.Y;

        [Header("Geometry")]
        [Tooltip("Половина колії (м): від центра до лівої/правої гусениці.")]
        public float halfTrack = 0.8f;

        void Reset()
        {
            if (!frame) frame = transform;                 // за замовчуванням сама рамка = цей об’єкт
            if (!rootRb) rootRb = GetComponentInParent<Rigidbody>();
        }

        void OnValidate()
        {
            if (!frame) frame = transform;
            if (!rootRb) rootRb = GetComponentInParent<Rigidbody>();
            halfTrack = Mathf.Max(0.001f, halfTrack);
        }

        // ---- ITrackMotionSourceWC ----
        public float GetMetersPerSecond(bool leftSide)
        {
            if (!rootRb || !frame) return 0f;

            Vector3 fwd = AxisToDir(frame, forwardAxis);
            Vector3 up = AxisToDir(frame, upAxis);

            // лінійна швидкість у ЦМ уздовж forward
            Vector3 v = rootRb.GetPointVelocity(rootRb.worldCenterOfMass);
            float vForward = Vector3.Dot(v, fwd);

            // yawRate навколо up (рад/с)
            float yawRate = Vector3.Dot(rootRb.angularVelocity, up);

            // внесок повороту на борту
            float sideTerm = yawRate * halfTrack;

            // лівий борт = внутрішній при додатньому yaw (правило правої руки)
            return leftSide ? (vForward - sideTerm) : (vForward + sideTerm);
        }

        public float GetForwardSpeedMS()
        {
            if (!rootRb || !frame) return 0f;
            Vector3 fwd = AxisToDir(frame, forwardAxis);
            Vector3 v = rootRb.GetPointVelocity(rootRb.worldCenterOfMass);
            return Vector3.Dot(v, fwd);
        }

        // ---- helpers ----
        static Vector3 AxisToDir(Transform t, Axis a)
        {
            // беремо локальні осі трансформу і, за потреби, інвертуємо
            return a switch
            {
                Axis.X => t.right,
                Axis.Y => t.up,
                Axis.Z => t.forward,
                Axis.NegX => -t.right,
                Axis.NegY => -t.up,
                Axis.NegZ => -t.forward,
                _ => t.forward
            };
        }
    }
}
