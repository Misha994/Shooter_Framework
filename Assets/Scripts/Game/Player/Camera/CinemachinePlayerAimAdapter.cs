using UnityEngine;
using Cinemachine;
using Zenject;

[DefaultExecutionOrder(-50)]
public class CinemachinePlayerAimAdapter : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private CinemachineVirtualCamera vcam;

	[Header("Base axis speeds (deg/sec)")]
	[Tooltip("Базова горизонтальна швидкість POV при чутливості 1.0")]
	[SerializeField] private float baseHorizontalSpeed = 220f;

	[Tooltip("Базова вертикальна швидкість POV при чутливості 1.0")]
	[SerializeField] private float baseVerticalSpeed = 180f;

	private IInputService _input;
	private GameConfig _cfg;
	private SignalBus _bus;

	private CinemachinePOV _pov;
	private bool _isAiming;

	// ===== DI =====
	[Inject]
	private void Construct(IInputService input, GameConfig cfg, SignalBus bus = null)
	{
		_input = input;
		_cfg = cfg;
		_bus = bus;
	}

	private void Awake()
	{
		if (vcam == null)
			vcam = GetComponent<CinemachineVirtualCamera>();

		if (vcam != null)
			_pov = vcam.GetCinemachineComponent<CinemachinePOV>();

		ConfigurePovFromConfig();

		// Сигнал на зміну стану прицілювання (якщо існує в проекті)
		_bus?.Subscribe<AimStateChangedSignal>(OnAimChanged);
	}

	private void OnDestroy()
	{
		_bus?.TryUnsubscribe<AimStateChangedSignal>(OnAimChanged);
	}

	private void OnAimChanged(AimStateChangedSignal s)
	{
		_isAiming = s.IsAiming;
	}

	private void ConfigurePovFromConfig()
	{
		if (_pov == null)
			return;

		// Кути з GameConfig
		if (_cfg != null)
		{
			_pov.m_VerticalAxis.m_MinValue = _cfg.MinPitch;
			_pov.m_VerticalAxis.m_MaxValue = _cfg.MaxPitch;
		}

		// Забезпечуємо, що POV використовує Mouse X / Mouse Y
		if (string.IsNullOrEmpty(_pov.m_HorizontalAxis.m_InputAxisName))
			_pov.m_HorizontalAxis.m_InputAxisName = "Mouse X";

		if (string.IsNullOrEmpty(_pov.m_VerticalAxis.m_InputAxisName))
			_pov.m_VerticalAxis.m_InputAxisName = "Mouse Y";

		// Скидаємо акселерацію / децелерацію в щось адекватне
		_pov.m_HorizontalAxis.m_AccelTime = 0.1f;
		_pov.m_HorizontalAxis.m_DecelTime = 0.1f;
		_pov.m_VerticalAxis.m_AccelTime = 0.1f;
		_pov.m_VerticalAxis.m_DecelTime = 0.1f;

		// На старті поставимо швидкості для HIP
		float mul = (_cfg != null) ? _cfg.LookSensitivity * _cfg.mouseSensitivityHip : 1f;
		mul = Mathf.Max(0.01f, mul);

		_pov.m_HorizontalAxis.m_MaxSpeed = baseHorizontalSpeed * mul;
		_pov.m_VerticalAxis.m_MaxSpeed = baseVerticalSpeed * mul;

		// Рецентринг нам не потрібен – туловище доганяє через HeadDrivenBodyAligner
		_pov.m_HorizontalRecentering.m_enabled = false;
		_pov.m_VerticalRecentering.m_enabled = false;
	}

	private void Update()
	{
		if (_pov == null || _cfg == null)
			return;

		// Якщо немає SignalBus – просто читаємо стан прицілу з інпуту
		if (_bus == null && _input != null)
			_isAiming = _input.IsAimHeld();

		// Множник HIP / ADS
		float mul = _isAiming ? _cfg.mouseSensitivityADS : _cfg.mouseSensitivityHip;
		float sens = Mathf.Max(0.01f, _cfg.LookSensitivity * mul);

		// Остаточна швидкість поверту POV
		_pov.m_HorizontalAxis.m_MaxSpeed = baseHorizontalSpeed * sens;
		_pov.m_VerticalAxis.m_MaxSpeed = baseVerticalSpeed * sens;
	}
}
