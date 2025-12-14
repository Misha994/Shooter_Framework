// SPDX-FileCopyrightText: 2025
// SPDX-License-Identifier: MIT

using UnityEngine;

namespace Tracks
{
    /// <summary>
    /// Маркер точки підвіски (Axle) + опціональна "прив'язка" до кістки-центра колеса.
    /// Якщо задано wheelTransform, нейтральний центр береться з неї (з урахуванням localOriginOffset).
    /// </summary>
    public class TrackPoint : MonoBehaviour
    {
        public enum AxisMode
        {
            /// <summary>Вісь визначається від батька (Parent.up); якщо батька нема — від власної up.</summary>
            AutoFromParentUp = 0,
            /// <summary>Завжди від світового Up.</summary>
            AutoFromWorldUp = 1,
            /// <summary>Власний локальний напрям (manualLocal), перетворений у світ.</summary>
            ManualLocal = 2,
        }

        [Header("Axis setup")]
        public AxisMode axisMode = AxisMode.AutoFromParentUp;
        public Vector3 manualLocal = Vector3.up;
        public bool invert = false;

        [Header("Wheel center source")]
        [Tooltip("Якщо задано — нейтральний центр колеса береться з позиції цієї кістки.")]
        public Transform wheelTransform;

        [Tooltip("Локальний офсет відносно wheelTransform (якщо задано) або відносно цього TrackPoint.")]
        public Vector3 localOriginOffset = Vector3.zero;

        [Header("Auto-find by token (не обов'язково)")]
        public string autoWheelToken = "Wheel";

        [Header("Gizmos")]
        public Color gizmoColor = new Color(0.9f, 0.6f, 0.1f, 1f);
        public float gizmoLen = 1.0f;
        public float gizmoWheelRadius = 0.35f;
        public bool drawGizmoSphere = true;

        /// <summary>Світовий напрямок "вниз" для цієї точки (для підвіски).</summary>
        public Vector3 WorldDown
        {
            get
            {
                Vector3 upWS;
                switch (axisMode)
                {
                    case AxisMode.AutoFromWorldUp:
                        upWS = Vector3.up;
                        break;
                    case AxisMode.ManualLocal:
                        upWS = transform.TransformDirection(
                            manualLocal.sqrMagnitude > 1e-6f ? manualLocal.normalized : Vector3.up);
                        break;
                    default: // AutoFromParentUp
                        upWS = transform.parent ? transform.parent.up : transform.up;
                        break;
                }
                var down = -upWS.normalized;
                if (invert) down = -down;
                return down;
            }
        }

        /// <summary>
        /// Нейтральний центр колеса у світі. Якщо призначено wheelTransform —
        /// повертає її позицію з локальним офсетом. Інакше — використовує свій Transform.
        /// </summary>
        public Vector3 WheelCenterWS
        {
            get
            {
                if (wheelTransform)
                    return wheelTransform.TransformPoint(localOriginOffset);
                return transform.TransformPoint(localOriginOffset);
            }
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            Gizmos.color = gizmoColor;

            // Вісь стояка
            var down = WorldDown;
            var up = -down;
            Gizmos.DrawLine(transform.position, transform.position + up * gizmoLen);

            // Центр колеса (нейтраль)
            var c = WheelCenterWS;
            if (drawGizmoSphere)
            {
                Gizmos.DrawSphere(transform.position, 0.04f); // Axle
                Gizmos.color = new Color(0.2f, 0.9f, 1f, 1f);
                Gizmos.DrawSphere(c, 0.03f);
                Gizmos.DrawWireSphere(c, gizmoWheelRadius);
            }
        }
#endif
    }
}
