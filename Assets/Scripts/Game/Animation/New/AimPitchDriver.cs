using UnityEngine;
using Cinemachine;

/// <summary>
/// Знімає pitch із CinemachinePOV і пише його в Animator float параметр (наприклад, "AimPitch").
/// Передбачає 1D або 2D BlendTree по куту.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AimPitchDriver : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera vcam;
    [SerializeField] private string animatorPitchParam = "AimPitch";
    [Tooltip("Мін/Макс кути для нормалізації, в градусах (від'ємний = вниз).")]
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;

    [Tooltip("true: в Animator очікуємо 0..1; false: записуємо сирий кут у градусах.")]
    [SerializeField] private bool normalize01 = true;

    private Animator _anim;
    private CinemachinePOV _pov;

    private void Awake()
    {
        _anim = GetComponent<Animator>();
        if (!vcam) vcam = FindObjectOfType<CinemachineVirtualCamera>();
        if (vcam) _pov = vcam.GetCinemachineComponent<CinemachinePOV>();
        if (!_pov)
        {
            Debug.LogWarning("[AimPitchDriver] CinemachinePOV не знайдено — скрипт відключено.");
            enabled = false; return;
        }
    }

    private void Update()
    {
        float pitch = _pov.m_VerticalAxis.Value; // у градусах
        float value = normalize01 ? Mathf.InverseLerp(minPitch, maxPitch, pitch) : pitch;
        _anim.SetFloat(animatorPitchParam, value);
    }
}
