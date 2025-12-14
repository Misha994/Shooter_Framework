using System;
using UnityEngine;

public class MechLeg : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Transform hip;
	[SerializeField] private Transform foot;
	[SerializeField] private Transform ikTarget;
	[SerializeField] private Transform ikHint;
	[SerializeField] private Transform body;

	[Header("Profile (optional)")]
	[SerializeField] private MechLegProfile profile; // optional

	[Header("Leg Id")]
	[SerializeField] private int legIndex = 0;
	[SerializeField] private bool isLeftSide = true;

	[Header("Grounding")]
	[SerializeField] private LayerMask terrainLayer = ~0;
	[SerializeField] private Vector3 footOffset = new Vector3(0f, 0.2f, 0f);
	[SerializeField] private float footRaycastDistance = 3f;

	[Header("Step Tuning (for base speed)")]
	[Tooltip("Максимальна висота дуги кроку.")]
	[SerializeField] private float stepHeight = 0.6f;

	[Tooltip("Час одного кроку при базовій швидкості (SpeedScale = 1).")]
	[SerializeField] private float stepDuration = 0.35f;

	[Tooltip("Мінімальна дистанція від ідеалу, щоб нога захотіла зробити крок (для базової швидкості).")]
	[SerializeField] private float minStepDistance = 0.5f;

	[Tooltip("Максимальна адекватна дистанція від ідеальної позиції (для базової швидкості).")]
	[SerializeField] private float maxStepDistance = 2.5f;

	[Header("Hint Offsets")]
	[SerializeField] private float hintForwardOffset = 0.3f;
	[SerializeField] private float hintSideOffset = 0.4f;
	[SerializeField] private float hintUpOffset = 0f;

	[Header("Debug")]
	[SerializeField] private bool drawDebug = false;
	[SerializeField] private bool debugAllowManualIK = false; // для сумісності інспектора

	// ===== Runtime =====
	[NonSerialized] private MechGaitController_New _controller;
	[NonSerialized] private Vector3 _defaultFootLocal;
	[NonSerialized] private bool _isStepping;
	[NonSerialized] private float _lastStepTime;
	[NonSerialized] private Vector3 _stepStartPos;
	[NonSerialized] private Vector3 _stepEndPos;
	[NonSerialized] private float _stepT;
	[NonSerialized] private Vector3 _plantedPos;
	[NonSerialized] private Vector3 _prevBodyPos;
	[NonSerialized] private Quaternion _prevBodyRot;

	public int LegIndex => legIndex;
	public bool IsStepping => _isStepping;
	public float LastStepTime => _lastStepTime;

	private float SpeedScale => _controller != null ? _controller.SpeedScale : 1f;

	public float CurrentStepDuration
	{
		get
		{
			float scale = Mathf.Max(SpeedScale, 0.1f);
			return Mathf.Max(0.0001f, stepDuration / scale);
		}
	}

	public float CurrentMinStepDistance
	{
		get
		{
			float scale = Mathf.Max(SpeedScale, 0.1f);
			return minStepDistance * scale;
		}
	}

	public float CurrentMaxStepDistance
	{
		get
		{
			float scale = Mathf.Max(SpeedScale, 0.1f);
			return maxStepDistance * scale;
		}
	}

	// Для сумісності з іншими скриптами, якщо вони юзають ці властивості
	public float MinStepDistance => CurrentMinStepDistance;
	public float MaxStepDistance => CurrentMaxStepDistance;

	private void Awake()
	{
		_controller = GetComponentInParent<MechGaitController_New>();

		if (body == null)
			body = _controller != null ? _controller.BodyTransform : transform.root;

		if (hip == null)
			hip = transform;

		if (foot == null)
			foot = transform;

		if (_controller != null)
			_controller.RegisterLeg(this);
	}

	public void Initialize(Transform bodyTransform)
	{
		if (bodyTransform != null)
			body = bodyTransform;

		if (body == null)
			body = transform.root;

		_defaultFootLocal = body.InverseTransformPoint(foot.position);
		_plantedPos = foot.position;
		_prevBodyPos = body.position;
		_prevBodyRot = body.rotation;
		_stepT = 1f;
		_isStepping = false;

		UpdateIKTargets(_plantedPos);
	}

	/// <summary>
	/// Наскільки нога "проситься" зробити крок.
	/// Контролер порівнює це значення з власним порогом.
	/// </summary>
	public float GetStepScore()
	{
		Vector3 ideal = GetIdealFootPosition();
		float dist = Vector3.Distance(_plantedPos, ideal);
		return dist;
	}

	/// <summary>
	/// Ідеальна позиція стопи відносно тіла + прилипання до землі.
	/// </summary>
	public Vector3 GetIdealFootPosition()
	{
		if (body == null)
			return foot.position;

		// 1) Базова позиція з локального офсету (вже містить правильний X/Z для цієї ноги)
		Vector3 ideal = body.TransformPoint(_defaultFootLocal);

		// 2) Використовуємо footOffset ТІЛЬКИ по Y (щоб не зламати досяжність по X/Z)
		Vector3 verticalOffset = new Vector3(0f, footOffset.y, 0f);
		ideal += body.TransformVector(verticalOffset);

		// 3) Рейкаст до землі, щоб нога стояла на поверхні
		Vector3 origin = ideal + Vector3.up * (footRaycastDistance * 0.5f);
		if (Physics.Raycast(origin, Vector3.down, out var hit, footRaycastDistance, terrainLayer, QueryTriggerInteraction.Ignore))
		{
			ideal = hit.point + body.up * footOffset.y;

			if (drawDebug)
				Debug.DrawLine(origin, hit.point, Color.green);
		}
		else if (drawDebug)
		{
			Debug.DrawLine(origin, origin + Vector3.down * footRaycastDistance, Color.red);
		}

		// (опціонально) Кламп дистанції від тазу, щоб не лізти в повний розрив ланцюга
		if (hip != null)
		{
			float restLen = Vector3.Distance(hip.position, foot.position);
			float maxReach = restLen * 1.5f; // трошки запасу
			Vector3 fromHip = ideal - hip.position;
			float dist = fromHip.magnitude;
			if (dist > maxReach)
			{
				ideal = hip.position + fromHip.normalized * maxReach;
			}
		}

		return ideal;
	}

	public void BeginStep(Vector3 targetPos)
	{
		_isStepping = true;
		_stepT = 0f;
		_stepStartPos = _plantedPos;
		_stepEndPos = targetPos;
	}

	public bool Tick(float deltaTime)
	{
		if (body == null)
			return false;

		_prevBodyPos = body.position;
		_prevBodyRot = body.rotation;

		if (_isStepping)
		{
			_stepT += deltaTime / CurrentStepDuration;
			float t = Mathf.Clamp01(_stepT);

			Vector3 basePos = Vector3.Lerp(_stepStartPos, _stepEndPos, t);
			float height = Mathf.Sin(t * Mathf.PI) * stepHeight;
			Vector3 pos = basePos + body.up * height;

			_plantedPos = pos;

			if (t >= 0.999f)
			{
				_isStepping = false;
				_lastStepTime = Time.time;
				_plantedPos = _stepEndPos;
				UpdateIKTargets(_plantedPos);
				return true;
			}
		}
		else
		{
			Vector3 grounded = GetIdealFootPosition();
			float dist = Vector3.Distance(_plantedPos, grounded);
			if (dist > CurrentMaxStepDistance * 1.5f)
			{
				_plantedPos = grounded;
			}
		}

		UpdateIKTargets(_plantedPos);
		return false;
	}

	private void UpdateIKTargets(Vector3 footWorld)
	{
		if (ikTarget != null)
		{
			ikTarget.position = footWorld;
		}

		if (ikHint != null && hip != null)
		{
			Vector3 rootPos = hip.position;
			Vector3 forward = body != null ? body.forward : Vector3.forward;
			Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
			if (isLeftSide)
				side = -side;

			Vector3 hintPos = rootPos
							  + forward * hintForwardOffset
							  + side * hintSideOffset
							  + Vector3.up * hintUpOffset;

			ikHint.position = hintPos;
		}

		if (drawDebug)
		{
			Debug.DrawLine(hip.position, footWorld, Color.cyan);
		}
	}
}
