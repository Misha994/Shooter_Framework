using UnityEngine;
using Cinemachine;

/// <summary>
/// Head-driven body align: вільний огляд до freeYawDeg (тіло не крутиться).
/// Після порогу — тіло підлаштовується під голову (камеру).
/// </summary>
[DefaultExecutionOrder(+180)]
public class HeadDrivenBodyAligner : MonoBehaviour
{
	[Header("Refs")]
	[Tooltip("Якщо задати vcam, візьмемо yaw з CinemachinePOV; інакше з cameraTransform")]
	public CinemachineVirtualCamera vcam;
	public Transform cameraTransform;   // fallback: основна камера/CameraTarget

	[Header("Angles (deg)")]
	[Tooltip("До цього кута — повністю вільний огляд (тіло нерухоме)")]
	public float freeYawDeg = 90f;
	[Tooltip("Понад це тіло доганяє дуже швидко (жорсткий поріг)")]
	public float hardYawDeg = 105f;

	[Header("Speeds (deg/s)")]
	[Tooltip("Швидкість підтягування в межах (free..hard)")]
	public float followSpeed = 360f;
	[Tooltip("Швидкість доганяння за межами hardYawDeg")]
	public float catchUpSpeed = 900f;

	[Header("Optional recentre")]
	[Tooltip("Авто-рецентр, коли гравець перестає крутити огляд (якщо треба)")]
	public bool autoRecenter = false;
	public float recenterDelay = 0.25f;
	public float recenterSpeed = 120f;

	private CinemachinePOV _pov;
	private IInputService _input;
	private float _noLookTimer;

	void Awake()
	{
		if (!vcam) vcam = GetComponentInParent<CinemachineVirtualCamera>();
		if (vcam) _pov = vcam.GetCinemachineComponent<CinemachinePOV>();
		if (!cameraTransform && Camera.main) cameraTransform = Camera.main.transform;
		_input = GetComponentInParent<IInputService>();
	}

	void Update()
	{
		// 1) yaw голови/камери
		float headYaw = GetHeadYaw();
		float bodyYaw = transform.eulerAngles.y;
		float delta = Mathf.DeltaAngle(bodyYaw, headYaw);
		float absDelta = Mathf.Abs(delta);

		// 2) чи є рух камери цього кадру
		bool hasLook = false;
		if (_input != null)
		{
			var look = _input.GetLookDelta();
			hasLook = (look.sqrMagnitude > 0.000001f);
		}
		_noLookTimer = hasLook ? 0f : (_noLookTimer + Time.deltaTime);

		// 3) логіка зон
		if (absDelta >= hardYawDeg)
		{
			// жорстке доганяння
			RotateTowards(headYaw, catchUpSpeed);
		}
		else if (absDelta > freeYawDeg)
		{
			// плавне підтягування (що ближче до hard — то швидше)
			float t = Mathf.InverseLerp(freeYawDeg, hardYawDeg, absDelta);
			float speed = Mathf.Lerp(followSpeed * 0.6f, catchUpSpeed, t);
			RotateTowards(headYaw, speed);
		}
		else
		{
			// повністю вільний огляд
			if (autoRecenter && _noLookTimer >= recenterDelay && recenterSpeed > 0f)
				RotateTowards(headYaw, recenterSpeed);
		}
	}

	float GetHeadYaw()
	{
		if (_pov != null)
			return _pov.m_HorizontalAxis.Value;

		if (cameraTransform)
			return cameraTransform.eulerAngles.y;

		return transform.eulerAngles.y;
	}

	void RotateTowards(float targetYaw, float degPerSec)
	{
		float current = transform.eulerAngles.y;
		float step = degPerSec * Time.deltaTime;
		float newYaw = Mathf.MoveTowardsAngle(current, targetYaw, step);
		transform.rotation = Quaternion.Euler(0f, newYaw, 0f);
	}
}
