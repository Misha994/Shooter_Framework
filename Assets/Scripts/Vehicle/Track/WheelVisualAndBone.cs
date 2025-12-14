// SPDX-License-Identifier: MIT
using UnityEngine;

/// <summary>
/// Синхронізує pivot видимого катка (mesh/pivot) з WheelCollider,
/// а кістку гусениці рухає або:
///  - 1) точно за візуалом (boneFollowVisual = true), без обертання;
///  - 2) або лише вздовж заданої локальної осі (boneAxisLocal), з урахуванням ходу підвіски.
/// </summary>
[DisallowMultipleComponent]
public class WheelVisualAndBone : MonoBehaviour
{
    [Header("Refs")]
    public WheelCollider wheel;
    [Tooltip("Pivot/mesh видимого катка (крутиться та рухається за WheelCollider).")]
    public Transform wheelVisual;
    [Tooltip("Кістка стрічки, яка має їхати тільки вгору/вниз (без обертання).")]
    public Transform trackBone;

    [Header("Visual (wheel)")]
    [Tooltip("Локальний офсет для меша (якщо pivot не в геометричному центрі).")]
    public Vector3 visualLocalOffset = Vector3.zero;

    [Tooltip("LEGACY: додаткове «провисання» колеса в повітрі (м).")]
    public float droop = 0.10f;

    [Tooltip("Увімкнути legacy-поведінку провисання (droop) у польоті.")]
    public bool useLegacyDroop = false;

    [Tooltip("Додаткове обертання лише для візуалу (під твій ріг).")]
    public Vector3 visualExtraEuler = Vector3.zero;

    [Header("Track bone mode")]
    [Tooltip("Якщо true — кістка отримує світову позицію візуалу колеса 1:1 (без обертання).")]
    public bool boneFollowVisual = false;

    [Header("Track bone (axis mode)")]
    [Tooltip("Локальна вісь батька кістки, вздовж якої кістка рухається (режим осі).")]
    public Vector3 boneAxisLocal = Vector3.up;

    [Tooltip("Мін./макс. відхилення уздовж осі (м) від REST-позиції (режим осі).")]
    public float minOffset = -0.20f, maxOffset = 0.10f;

    [Tooltip("Додатковий локальний офсет кістки від REST-позиції (обидва режими).")]
    public Vector3 boneExtraLocalOffset = Vector3.zero;

    [Tooltip("Швидкість повернення кістки до 0, коли колесо в повітрі (м/с, режим осі).")]
    public float boneReturnToRest = 8f;

    [Tooltip("Обмеження швидкості візуального руху кістки (м/с, режим осі).")]
    public float boneMaxVisualSpeed = 3f;

    [Range(0f, 1f)]
    [Tooltip("Згладження візуального руху кістки (режим осі).")]
    public float boneSmooth = 0.25f;

    // ---- internal state ----
    Vector3 _boneRestLocal;
    float _boneCur; // поточний зсув уздовж осі (м), режим осі

    void Reset()
    {
        if (!wheel) wheel = GetComponentInChildren<WheelCollider>();
        if (!trackBone) trackBone = transform;
    }

    void OnEnable()
    {
        if (trackBone)
            _boneRestLocal = trackBone.localPosition;
        _boneCur = 0f;
    }

    void LateUpdate()
    {
        if (!wheel) return;

        // 1) ПОЗА КОЛЕСА ВІД WheelCollider (враховує компресію підвіски)
        wheel.GetWorldPose(out Vector3 wPos, out Quaternion wRot);
        bool grounded = wheel.GetGroundHit(out WheelHit hit);

        // ДОДАТКОВЕ ПРОВИСАННЯ (опціонально)
        if (useLegacyDroop && !grounded)
        {
            Vector3 down = wRot * Vector3.up * -1f;
            wPos += down * droop;
        }

        // 1a) ВІЗУАЛ КОЛЕСА: позиція + обертання
        if (wheelVisual)
        {
            var visRot = wRot * Quaternion.Euler(visualExtraEuler);
            wheelVisual.SetPositionAndRotation(wPos, visRot);

            if (visualLocalOffset != Vector3.zero)
                wheelVisual.position += wheelVisual.TransformVector(visualLocalOffset);
        }

        // 2) КІСТКА СТРІЧКИ
        if (!trackBone) return;

        // ---- РЕЖИМ: КІСТКА ЙДЕ РІВНО ЗА ВІЗУАЛОМ (без обертання) ----
        if (boneFollowVisual)
        {
            var parent = trackBone.parent;

            Vector3 targetWS = wPos;
            if (boneExtraLocalOffset != Vector3.zero)
            {
                Vector3 offWS = parent ? parent.TransformVector(boneExtraLocalOffset) : boneExtraLocalOffset;
                targetWS += offWS;
            }

            // Лише позиція; rotation НЕ чіпаємо
            Vector3 finalLocalFollow = parent ? parent.InverseTransformPoint(targetWS) : targetWS;
            trackBone.localPosition = finalLocalFollow;
            return;
        }

        // ---- РЕЖИМ: ЛИШЕ ВЗДОВЖ ОСІ (пружна логіка) ----
        float dt = Time.deltaTime;
        float targetOffset;

        if (grounded)
        {
            float alongUp = Vector3.Dot(wPos - hit.point, wRot * Vector3.up);
            float travel = (wheel.radius + wheel.suspensionDistance) - alongUp;
            targetOffset = Mathf.Clamp(-travel, minOffset, maxOffset);
        }
        else
        {
            targetOffset = Mathf.MoveTowards(_boneCur, 0f, boneReturnToRest * dt);
        }

        float maxStep = boneMaxVisualSpeed * dt;
        float raw = Mathf.MoveTowards(_boneCur, targetOffset, maxStep);
        _boneCur = Mathf.Lerp(_boneCur, raw, 1f - Mathf.Pow(1f - boneSmooth, dt * 60f));

        var p = trackBone.parent;

        Vector3 axisWorld = p ? p.TransformDirection(boneAxisLocal.normalized)
                              : boneAxisLocal.normalized;

        Vector3 baseWorld = p ? p.TransformPoint(_boneRestLocal + boneExtraLocalOffset)
                              : (_boneRestLocal + boneExtraLocalOffset);

        Vector3 finalWorld = baseWorld + axisWorld * _boneCur;
        Vector3 finalLocalAxis = p ? p.InverseTransformPoint(finalWorld) : finalWorld;

        trackBone.localPosition = finalLocalAxis;
        // rotation НЕ змінюємо
    }
}
