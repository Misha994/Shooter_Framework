// CameraFOVAimControllerCinemachine.cs
// Повісити на той самий об'єкт, де CinemachineVirtualCamera (або на окремий, але тримай посилання)
using UnityEngine;
using Zenject;
using Cinemachine;

public class CameraFOVAimControllerCinemachine : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera vcam;

    private float hipFOV = 60f;
    private float adsFOV = 50f;
    private float fovSmoothTime = 0.12f;

    private float targetFOV;
    private float currentVel;
    private bool targetIsAim;

    public float SensitivityMultiplierADS { get; private set; } = 1f;

    [Inject]
    public void Construct(SignalBus bus)
    {
        bus.Subscribe<AimStateChangedSignal>(OnAimChanged);
    }

    private void Awake()
    {
        if (!vcam) vcam = GetComponent<CinemachineVirtualCamera>();
        if (vcam) hipFOV = vcam.m_Lens.FieldOfView;
        targetFOV = hipFOV;
    }

    // сумісний API з твоїм старим контролером
    public void SetupFromConfig(WeaponConfig cfg)
    {
        if (cfg == null || vcam == null) return;
        hipFOV = cfg.hipFOV;
        adsFOV = cfg.adsFOV;
        fovSmoothTime = Mathf.Max(0.01f, cfg.fovLerpTime);
        SensitivityMultiplierADS = Mathf.Clamp(cfg.adsSensitivityMultiplier <= 0f ? 1f : cfg.adsSensitivityMultiplier, 0.05f, 2f);

        vcam.m_Lens.FieldOfView = hipFOV;
        targetFOV = hipFOV;
    }

    private void OnAimChanged(AimStateChangedSignal sig)
    {
        targetIsAim = sig.IsAiming;
        targetFOV = targetIsAim ? adsFOV : hipFOV;
    }

    private void Update()
    {
        if (!vcam) return;

        float fov = vcam.m_Lens.FieldOfView;
        fov = Mathf.SmoothDamp(fov, targetFOV, ref currentVel, fovSmoothTime);
        vcam.m_Lens.FieldOfView = fov;
    }
}
