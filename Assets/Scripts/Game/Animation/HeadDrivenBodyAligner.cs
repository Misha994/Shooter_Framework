using UnityEngine;
using Cinemachine;
using Zenject;

/// <summary>
/// Arma-style: голова (камера) може відриватися від корпусу на кут freeYaw,
/// після чого тіло плавно доганяє, щоб різниця не перевищувала цей кут.
/// Все через SmoothDampAngle, без ривків.
/// </summary>
[DefaultExecutionOrder(+200)]
public class HeadDrivenBodyAligner : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private CinemachineVirtualCamera vcam;
	[Tooltip("Корінь yaw для персонажа (зазвичай 'Infantryman').")]
	[SerializeField] private Transform yawRoot;
	[Tooltip("Фізична камера (PlayerCamera) – фолбек, якщо немає POV.")]
	[SerializeField] private Transform cameraTransform;

	[Header("Free look (deg)")]
	[Tooltip("Максимальний кут між камерою і корпусом в HIP (як у Arma freelook).")]
	[SerializeField] private float hipFreeYawDeg = 40f;

	[Tooltip("Той самий кут у ADS (зазвичай менший, щоб легше тримати ціль).")]
	[SerializeField] private float adsFreeYawDeg = 10f;

	[Header("Плавність повороту тіла")]
	[Tooltip("Час згладжування повороту тіла в HIP (сек). 0.05–0.12 дає реалістичну інерцію.")]
	[SerializeField] private float hipLagSeconds = 0.08f;

	[Tooltip("Час згладжування повороту тіла в ADS (сек).")]
	[SerializeField] private float adsLagSeconds = 0.06f;

	[Tooltip("Максимальна кутова швидкість повороту тіла (deg/sec).")]
	[SerializeField] private float maxDegreesPerSecond = 720f;

	[Header("Auto recenter (опційно)")]
	[SerializeField] private bool autoRecenter = false;
	[SerializeField] private float recenterDelay = 0.3f;
	[SerializeField] private float recenterLagSeconds = 0.15f;

	private CinemachinePOV _pov;
	private IInputService _input;

	private float _yawVel;      // для SmoothDampAngle
	private float _noLookTimer; // скільки часу мишка не рухалась

	// ===== DI =====
	[Inject]
	private void Construct(IInputService input)
	{
		_input = input;
	}

	private void Awake()
	{
		if (!vcam)
			vcam = FindObjectOfType<CinemachineVirtualCamera>();

		ResolvePov();

		if (!yawRoot)
			yawRoot = transform;

		if (!cameraTransform && Camera.main)
			cameraTransform = Camera.main.transform;
	}

	private void ResolvePov()
	{
		if (vcam == null)
			return;

		// Якщо POV був видалений/переставлений — отримаємо актуальний
		var pov = vcam.GetCinemachineComponent<CinemachinePOV>();
		if (pov != null)
			_pov = pov;
	}

	private void LateUpdate()
	{
		if (!yawRoot)
			return;

		// На випадок MissingReference після маніпуляцій з VCam
		if (_pov == null)
			ResolvePov();

		float camYaw = GetCameraYaw();
		if (float.IsNaN(camYaw))
			return;

		Vector3 euler = yawRoot.eulerAngles;
		float bodyYaw = euler.y;

		bool isADS = _input != null && _input.IsAimHeld();

		float freeYaw = Mathf.Max(0f, isADS ? adsFreeYawDeg : hipFreeYawDeg);
		float lag = Mathf.Max(0.0001f, isADS ? adsLagSeconds : hipLagSeconds);

		// Відстежуємо активність мишки
		Vector2 look = _input != null ? _input.GetLookDelta() : Vector2.zero;
		if (look.sqrMagnitude > 0.0001f)
			_noLookTimer = 0f;
		else
			_noLookTimer += Time.deltaTime;

		// Різниця між корпусом і камерою
		float diff = Mathf.DeltaAngle(bodyYaw, camYaw); // [-180; 180]
		float absDiff = Mathf.Abs(diff);

		// Arma-логіка:
		//  - поки |diff| <= freeYaw → голова "вільна", тіло стоїть
		//  - коли |diff| > freeYaw → тіло крутиться, щоб залишатись на краю цієї зони
		float clampedDiff = Mathf.Clamp(diff, -freeYaw, freeYaw);

		// Цільовий yaw корпусу — такий, щоб камера була відхилена не більше, ніж на freeYaw
		float targetBodyYaw = camYaw - clampedDiff;

		// Опціональний авто-рецентр, коли мишка не рухається і ми всередині зони
		if (autoRecenter && absDiff < freeYaw * 0.5f && _noLookTimer > recenterDelay)
		{
			// повільно підтягувати корпус прямо під камеру
			targetBodyYaw = camYaw;
			lag = recenterLagSeconds;
		}

		// Плавний поворот без ривків
		float newYaw = Mathf.SmoothDampAngle(
			bodyYaw,
			targetBodyYaw,
			ref _yawVel,
			lag,
			maxDegreesPerSecond
		);

		euler.y = newYaw;
		yawRoot.eulerAngles = euler;
	}

	private float GetCameraYaw()
	{
		// Якщо POV живий – беремо yaw звідти (те саме, що крутиш у CinemachinePlayerAimAdapter)
		if (_pov != null)
			return _pov.m_HorizontalAxis.Value;

		// Фолбек — фізична камера
		if (cameraTransform != null)
			return cameraTransform.eulerAngles.y;

		return float.NaN;
	}
}
