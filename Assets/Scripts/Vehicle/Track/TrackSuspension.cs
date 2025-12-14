// SPDX-License-Identifier: MIT
using System.Collections.Generic;
using UnityEngine;
using Tracks;

namespace TracksPhysics
{
    /// <summary>
    /// Обʼємна підвіска гусениці: мульти-SphereCast довкола центру сфери + безпечний fallback.
    /// Без ComputePenetration(null,...) — fallback через Overlap + ClosestPoint.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class TrackSuspension : MonoBehaviour
    {
        [Header("Contact points (Axles)")]
        public Transform[] leftPoints;
        public Transform[] rightPoints;

        [Header("Wheel geometry")]
        public float wheelRadius = 0.35f;
        public float restLength = 0.30f;
        public float travel = 0.30f;
        public LayerMask groundMask = ~0;

        [Header("Spring & Damper (defaults)")]
        public float spring = 300000f;
        public float damper = 40000f;

        [Header("RB lateral damping")]
        [Range(0f, 1f)] public float lateralDamping = 0.25f;
        public float lateralMaxForce = 60000f;

        [Header("Friction (μ) for traction cap")]
        public float baseMu = 0.9f;
        public bool useMaterialFriction = true;
        public float materialFrictionScale = 1.0f;

        [Header("Volumetric sampling")]
        [Min(0)] public int ringSamples = 6;
        [Range(0f, 1f)] public float footprintRadiusFrac = 0.55f;
        [Range(0, 2)] public int forwardBackSamples = 2;
        [Range(0f, 1f)] public float forwardBackOffsetFrac = 0.35f;
        public float castSkin = 0.02f;

        [Header("Fallback (no cast hits)")]
        public bool useCapsuleFallback = true;
        [Range(1, 4)] public int capsuleSteps = 2;
        [Range(0.0f, 1.0f)] public float overlapRadiusFrac = 0.98f;

        [Header("Debug")]
        public bool drawGizmos = true;
        public Color gizmoSpringColor = new(0.2f, 0.9f, 0.2f, 1f);
        public Color gizmoNoHitColor = new(0.6f, 0.6f, 0.6f, 1f);
        public Color gizmoNormalColor = new(0.2f, 0.9f, 1f, 1f);

        public struct ContactInfo
        {
            public bool grounded;
            public Vector3 point;
            public Vector3 normal;
            public float normalForce;
            public float friction;
            public Collider collider;
            public float currentLength;      // Axle→центр (м) або -1 якщо немає контакту
            public float compression01;      // (rest - current)/travel
        }

        public ContactInfo[] LeftContacts => _leftContacts;
        public ContactInfo[] RightContacts => _rightContacts;

        Rigidbody _rb;
        ContactInfo[] _leftContacts = System.Array.Empty<ContactInfo>();
        ContactInfo[] _rightContacts = System.Array.Empty<ContactInfo>();

        // per-point overrides (optional)
        (float rest, float travel, float k, float c)[] _leftP, _rightP;
        bool _usePerPoint;

        public void SetPerPointParams(
            List<(float rest, float travel, float k, float c)> left,
            List<(float rest, float travel, float k, float c)> right)
        {
            _leftP = left?.ToArray();
            _rightP = right?.ToArray();
            _usePerPoint =
                _leftP != null && _rightP != null &&
                left.Count == (leftPoints?.Length ?? 0) &&
                right.Count == (rightPoints?.Length ?? 0);
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody>() ?? GetComponentInParent<Rigidbody>();
            ResizeContactBuffers();
            enabled = true;
        }

        void OnValidate()
        {
            wheelRadius = Mathf.Max(0.01f, wheelRadius);
            restLength = Mathf.Max(0.01f, restLength);
            travel = Mathf.Max(0.01f, travel);
            spring = Mathf.Max(0f, spring);
            damper = Mathf.Max(0f, damper);
            lateralMaxForce = Mathf.Max(0f, lateralMaxForce);
            materialFrictionScale = Mathf.Max(0f, materialFrictionScale);
            castSkin = Mathf.Max(0f, castSkin);
            ResizeContactBuffers();
        }

        void ResizeContactBuffers()
        {
            int l = leftPoints != null ? leftPoints.Length : 0;
            int r = rightPoints != null ? rightPoints.Length : 0;
            if (_leftContacts == null || _leftContacts.Length != l) _leftContacts = new ContactInfo[l];
            if (_rightContacts == null || _rightContacts.Length != r) _rightContacts = new ContactInfo[r];
        }

        void FixedUpdate()
        {
            if (!_rb) return;
            ApplyLateralDampingForce();
            ApplyTrackForces(leftPoints, _leftContacts, true);
            ApplyTrackForces(rightPoints, _rightContacts, false);
        }

        static bool TryGetTrackPoint(Transform t, out TrackPoint tp)
        {
            tp = t ? t.GetComponent<TrackPoint>() : null;
            return tp != null;
        }

        static Vector3 GetWorldDown(Transform p)
        {
            Vector3 d = (p && p.TryGetComponent(out TrackPoint tp)) ? tp.WorldDown : -p.up;
            // якщо чомусь догори — перевертаємо
            if (Vector3.Dot(d, Vector3.down) < 0f) d = -d;
            return d.normalized;
        }

        Vector3 GetNeutralCenter(Transform point, float restL)
        {
            if (TryGetTrackPoint(point, out var tp) && tp.wheelTransform)
                return tp.WheelCenterWS;
            Vector3 down = GetWorldDown(point);
            Vector3 up = -down;
            return point.position + up * (restL + wheelRadius);
        }

        void ApplyTrackForces(Transform[] points, ContactInfo[] contactsOut, bool isLeft)
        {
            if (points == null || contactsOut == null) return;

            for (int i = 0; i < contactsOut.Length; i++) contactsOut[i] = default;

            for (int i = 0; i < points.Length; i++)
            {
                var p = points[i]; if (!p) continue;

                float restL = restLength, trav = travel, k = spring, c = damper;
                if (_usePerPoint)
                {
                    var arr = isLeft ? _leftP : _rightP;
                    if (arr != null && i < arr.Length)
                    {
                        restL = Mathf.Max(0.001f, arr[i].rest);
                        trav = Mathf.Max(0.001f, arr[i].travel);
                        k = Mathf.Max(0f, arr[i].k);
                        c = Mathf.Max(0f, arr[i].c);
                    }
                }

                Vector3 down = GetWorldDown(p);
                Vector3 up = -down;

                Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, up).normalized;
                if (fwd.sqrMagnitude < 1e-6f) fwd = Vector3.Cross(up, transform.right).normalized;
                Vector3 side = Vector3.Cross(up, fwd).normalized;

                Vector3 center = GetNeutralCenter(p, restL);

                float ringR = wheelRadius * footprintRadiusFrac;
                int ringN = Mathf.Max(0, ringSamples);
                int fbN = Mathf.Clamp(forwardBackSamples, 0, 2);
                float fbOff = wheelRadius * forwardBackOffsetFrac;

                Vector3 castStartBase = center + up * castSkin;
                float castLen = trav + castSkin;

                bool anyHit = false;
                float bestDist = float.PositiveInfinity;
                Vector3 bestPoint = Vector3.zero, bestNormal = up;
                Collider bestCol = null;

                void TryCastAt(Vector3 localOffset)
                {
                    Vector3 origin = castStartBase + localOffset;
                    if (Physics.SphereCast(origin, wheelRadius, down, out var rh, castLen, groundMask, QueryTriggerInteraction.Ignore))
                    {
                        anyHit = true;
                        if (rh.distance < bestDist)
                        {
                            bestDist = rh.distance;
                            bestPoint = rh.point;
                            bestNormal = rh.normal.sqrMagnitude > 1e-6f ? rh.normal.normalized : up;
                            bestCol = rh.collider;
                        }
                        if (drawGizmos) Debug.DrawRay(origin, down * rh.distance, gizmoSpringColor);
                    }
                    else
                    {
                        if (drawGizmos) Debug.DrawRay(origin, down * castLen, gizmoNoHitColor);
                    }
                }

                // центральний
                TryCastAt(Vector3.zero);
                // кільце
                for (int s = 0; s < ringN; s++)
                {
                    float ang = (s / Mathf.Max(1f, (float)ringN)) * Mathf.PI * 2f;
                    Vector3 radial = (Mathf.Cos(ang) * side + Mathf.Sin(ang) * fwd) * ringR;
                    TryCastAt(radial);
                    if (fbN >= 1) TryCastAt(radial + fwd * fbOff);
                    if (fbN >= 2) TryCastAt(radial - fwd * fbOff);
                }

                // Fallback: Overlap + ClosestPoint (стабільний, без ComputePenetration)
                if (!anyHit && useCapsuleFallback)
                {
                    int steps = Mathf.Max(1, capsuleSteps);
                    float smallR = Mathf.Max(0.001f, wheelRadius * overlapRadiusFrac);

                    for (int s = 0; s < steps; s++)
                    {
                        float t = (steps == 1) ? 1f : (s / (float)(steps - 1));
                        Vector3 a = castStartBase;
                        Vector3 b = castStartBase + down * (t * castLen);

                        var cols = Physics.OverlapCapsule(a, b, smallR, groundMask, QueryTriggerInteraction.Ignore);
                        for (int ci = 0; ci < cols.Length; ci++)
                        {
                            var col = cols[ci]; if (!col) continue;
                            if (col.attachedRigidbody == _rb) continue;

                            // Наближаємо нормаль: напрямок від найближчої точки поверхні до центру сфери
                            Vector3 closest = col.ClosestPoint(center);
                            Vector3 n = (center - closest);
                            if (n.sqrMagnitude < 1e-6f) n = up; else n.Normalize();

                            anyHit = true;
                            bestDist = 0f;
                            bestPoint = closest;
                            bestNormal = n;
                            bestCol = col;
                            break;
                        }
                        if (anyHit) break;
                    }
                }

                if (!anyHit)
                {
                    contactsOut[i] = new ContactInfo
                    {
                        grounded = false,
                        currentLength = -1f,
                        compression01 = -0.0f
                    };
                    continue;
                }

                float currentLen = Mathf.Clamp(restL + bestDist, 0f, restL + trav);
                float compression = (restL - currentLen) / Mathf.Max(0.0001f, trav);

                float velAlong = Vector3.Dot(_rb.GetPointVelocity(center), up);
                float springForce = Mathf.Max(0f, compression * k);
                float damperForce = velAlong * c;
                float along = Mathf.Max(0f, springForce - damperForce);

                _rb.AddForceAtPosition(along * up, center, ForceMode.Force);

                float mu = baseMu;
                if (useMaterialFriction && bestCol && bestCol.sharedMaterial)
                {
                    var mat = bestCol.sharedMaterial;
                    mu = Mathf.Max(mat.staticFriction, mat.dynamicFriction) * materialFrictionScale;
                }

                contactsOut[i] = new ContactInfo
                {
                    grounded = true,
                    point = bestPoint,
                    normal = bestNormal,
                    normalForce = along,
                    friction = Mathf.Max(0f, mu),
                    collider = bestCol,
                    currentLength = currentLen,
                    compression01 = compression
                };

                if (drawGizmos) Debug.DrawRay(bestPoint, bestNormal * 0.35f, gizmoNormalColor);
            }
        }

        void ApplyLateralDampingForce()
        {
            if (lateralDamping <= 0f || lateralMaxForce <= 0f || _rb == null) return;

            Vector3 vel = _rb.velocity;
            Vector3 planar = Vector3.ProjectOnPlane(vel, Vector3.up);

            Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
            Vector3 fwdVel = Vector3.Project(planar, fwd);
            Vector3 sideVel = planar - fwdVel;

            if (sideVel.sqrMagnitude < 1e-6f) return;

            Vector3 F = -sideVel * (lateralDamping * _rb.mass);
            if (F.sqrMagnitude > lateralMaxForce * lateralMaxForce)
                F = F.normalized * lateralMaxForce;

            _rb.AddForce(F, ForceMode.Force);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            if (!drawGizmos) return;

            void DrawSet(Transform[] set)
            {
                if (set == null) return;
                foreach (var p in set)
                {
                    if (!p) continue;
                    float restL = restLength;
                    Vector3 down = GetWorldDown(p);
                    Vector3 up = -down;

                    Vector3 c = GetNeutralCenter(p, restL);
                    Vector3 origin = c + up * castSkin;

                    Gizmos.color = new(1f, 0.8f, 0.2f, 1f);
                    Gizmos.DrawSphere(p.position, 0.04f);

                    Gizmos.color = new(0.2f, 0.9f, 1f, 1f);
                    Gizmos.DrawSphere(c, 0.03f);
                    Gizmos.DrawWireSphere(c, wheelRadius);

                    Gizmos.color = new(0.2f, 0.9f, 0.2f, 1f);
                    Gizmos.DrawLine(origin, origin + down * (travel + castSkin));
                }
            }
            DrawSet(leftPoints);
            DrawSet(rightPoints);
        }
#endif
    }
}
