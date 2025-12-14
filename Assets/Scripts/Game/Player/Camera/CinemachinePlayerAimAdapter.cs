// CinemachinePlayerAimAdapter.cs (smooth + correct order)
using UnityEngine;
using Zenject;
using Cinemachine;

[DefaultExecutionOrder(+90)] // раніше за PlayerYawSyncToCamera (+200), але після інпуту (+100)
public class CinemachinePlayerAimAdapter : MonoBehaviour
{
	[Inject] private IInputService _input;
	[Inject] private GameConfig _cfg;
	[Inject(Optional = true)] private CameraFOVAimControllerCinemachine _fovCtrl;
	[Inject] private SignalBus _bus;

	private CinemachineVirtualCamera _vcam;
	private CinemachinePOV _pov;

	// чутливість
	private float _sensBase;
	private float _sensCurrent;

	// ліміти
	private float _minPitch, _maxPitch;

	// внутрішні кути/цілі
	private float _targetYaw, _targetPitch;
	private float _yaw, _pitch;
	private float _yawVel, _pitchVel;

	// згладжування
	[SerializeField] private float hipSmooth = 0.06f;
	[SerializeField] private float adsSmooth = 0.10f;
	private float _smooth;

	private void Awake()
	{
		_vcam = GetComponent<CinemachineVirtualCamera>();
		_pov = _vcam ? _vcam.GetCinemachineComponent<CinemachinePOV>() : null;
		if (_pov == null && _vcam != null) _pov = _vcam.AddCinemachineComponent<CinemachinePOV>();
		if (_pov == null) { Debug.LogError("[AimAdapter] CinemachinePOV missing"); enabled = false; return; }

		// повністю відключаємо внутрішній інпут POV
		_pov.m_HorizontalAxis.m_InputAxisName = string.Empty;
		_pov.m_VerticalAxis.m_InputAxisName = string.Empty;

		// ВАЖЛИВО: застосовувати POV ДО body → менше артефактів/«кроків»
		_pov.m_ApplyBeforeBody = true;

		_sensBase = _cfg ? _cfg.LookSensitivity : 2f;
		_sensCurrent = _sensBase;
		_minPitch = _cfg ? _cfg.MinPitch : -80f;
		_maxPitch = _cfg ? _cfg.MaxPitch : 80f;

		// стартові значення з POV
		_targetYaw = _yaw = _pov.m_HorizontalAxis.Value;
		_targetPitch = _pitch = Mathf.Clamp(_pov.m_VerticalAxis.Value, _minPitch, _maxPitch);
		_smooth = hipSmooth;

		// (не обов'язково, але допомагає уникнути розсинхрону)
		var brain = Camera.main ? Camera.main.GetComponent<CinemachineBrain>() : null;
		if (brain)
		{
			brain.m_UpdateMethod = CinemachineBrain.UpdateMethod.LateUpdate;
			brain.m_BlendUpdateMethod = CinemachineBrain.BrainUpdateMethod.LateUpdate;
		}
	}

	private void OnEnable() => _bus.Subscribe<AimStateChangedSignal>(OnAimChanged);
	private void OnDisable() => _bus.TryUnsubscribe<AimStateChangedSignal>(OnAimChanged);

	private void OnAimChanged(AimStateChangedSignal sig)
	{
		float mult = 1f;
		if (sig.IsAiming && _fovCtrl != null)
			mult = Mathf.Clamp(_fovCtrl.SensitivityMultiplierADS, 0.05f, 2f);

		_sensCurrent = _sensBase * mult;
		_smooth = sig.IsAiming ? adsSmooth : hipSmooth;
	}

	// ВАЖЛИВО: працюємо в LateUpdate, щоб синхронитися з Brain/rig, до yaw-sync
	private void LateUpdate()
	{
		if (_pov == null || _input == null) return;

		// 1) збираємо інпут
		Vector2 d = _input.GetLookDelta();
		if (d.sqrMagnitude > 0.000001f)
		{
			// mouse delta вже пер-кадровий → без Time.deltaTime
			_targetYaw = Mathf.Repeat(_targetYaw + d.x * _sensCurrent, 360f);
			_targetPitch = Mathf.Clamp(_targetPitch - d.y * _sensCurrent, _minPitch, _maxPitch);
		}

		// 2) легке згладжування (експон. до цілі)
		_yaw = Mathf.SmoothDampAngle(_yaw, _targetYaw, ref _yawVel, _smooth);
		_pitch = Mathf.SmoothDamp(_pitch, _targetPitch, ref _pitchVel, _smooth);

		// 3) пишемо у POV
		_pov.m_HorizontalAxis.Value = _yaw;
		_pov.m_VerticalAxis.Value = _pitch;
	}
}
