using System.Collections.Generic;
using UnityEngine;

public class MechGaitController_New : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Rigidbody rb;
	[SerializeField] private Transform body;
	[SerializeField] private MechWalkerMotor motor;

	[Header("Gait Settings (base tuning)")]
	[Tooltip("Скільки ніг може робити крок одночасно.")]
	[SerializeField] private int maxSimultaneousSteps = 1;

	[Tooltip("Мінімальний час між стартами кроків (глобально по всіх ногах) для базової швидкості.")]
	[SerializeField] private float globalMinIntervalBetweenSteps = 0.1f;

	[Tooltip("Мінімальний score (дистанція від ідеалу), щоб нога почала крок — для базової швидкості.")]
	[SerializeField] private float stepDistanceThreshold = 0.7f;

	[Header("Speed scaling")]
	[Tooltip("Швидкість (м/с), при якій ти тюнив параметри ходьби. Наприклад, коли mech рухається повільно з maxSpeed = 1.")]
	[SerializeField] private float baseMoveSpeed = 1f;

	[Tooltip("Мінімальний коефіцієнт скейлу (щоб при майже нульовій швидкості не було нескінченних пауз/кроків).")]
	[SerializeField] private float minSpeedScale = 0.25f;

	[Tooltip("Максимальний коефіцієнт скейлу при дуже великих швидкостях.")]
	[SerializeField] private float maxSpeedScale = 3f;

	// ==== Runtime ====
	[System.NonSerialized] private readonly List<MechLeg> _legs = new List<MechLeg>();
	[System.NonSerialized] private float _lastGlobalStepTime;
	[System.NonSerialized] private int _currentSteppingCount;
	[System.NonSerialized] private int _nextLegIndex;
	[System.NonSerialized] private float _cachedSpeed;

	public Transform BodyTransform => body != null ? body : transform;
	public IReadOnlyList<MechLeg> Legs => _legs;

	/// <summary>
	/// Поточний коефіцієнт скейлу відносно baseMoveSpeed.
	/// 1.0f — це саме та швидкість, при якій ти тюнив gait.
	/// </summary>
	public float SpeedScale { get; private set; } = 1f;

	/// <summary>
	/// Поточна оцінка глобального інтервалу між кроками з урахуванням швидкості.
	/// При більшій швидкості – менший інтервал (частіше кроки).
	/// </summary>
	public float ScaledGlobalMinIntervalBetweenSteps
	{
		get
		{
			float scale = Mathf.Max(SpeedScale, 0.1f);
			return globalMinIntervalBetweenSteps / scale;
		}
	}

	/// <summary>
	/// Поточний поріг score'а для старту кроку (чим швидше, тим більшу дистанцію ми дозволяємо, щоб частіше крокувати).
	/// </summary>
	public float ScaledStepDistanceThreshold
	{
		get
		{
			float scale = Mathf.Max(SpeedScale, 0.1f);
			return stepDistanceThreshold * scale;
		}
	}

	public float CachedSpeed => _cachedSpeed;

	private void Awake()
	{
		if (rb == null)
			rb = GetComponent<Rigidbody>();

		if (body == null)
			body = transform;

		if (motor == null)
			motor = GetComponent<MechWalkerMotor>();
	}

	private void Start()
	{
		var found = GetComponentsInChildren<MechLeg>(true);
		foreach (var leg in found)
		{
			RegisterLeg(leg);
		}

		foreach (var leg in _legs)
		{
			if (leg != null)
				leg.Initialize(body);
		}
	}

	public void RegisterLeg(MechLeg leg)
	{
		if (leg == null || _legs.Contains(leg))
			return;

		_legs.Add(leg);
		_legs.Sort((a, b) => a.LegIndex.CompareTo(b.LegIndex));
	}

	private void Update()
	{
		if (_legs.Count == 0)
			return;

		float dt = Time.deltaTime;

		UpdateSpeedScale();

		for (int i = 0; i < _legs.Count; i++)
		{
			var leg = _legs[i];
			if (leg == null)
				continue;

			bool finished = leg.Tick(dt);
			if (finished)
			{
				_currentSteppingCount = Mathf.Max(0, _currentSteppingCount - 1);
			}
		}

		TryStartNewStep();
	}

	private void UpdateSpeedScale()
	{
		if (rb == null || baseMoveSpeed <= 0.01f)
		{
			_cachedSpeed = 0f;
			SpeedScale = 1f;
			return;
		}

		_cachedSpeed = rb.velocity.magnitude;
		float raw = (_cachedSpeed / baseMoveSpeed);

		// нормалізуємо в адекватний діапазон
		float min = Mathf.Max(minSpeedScale, 0.1f);
		float max = Mathf.Max(maxSpeedScale, min);
		SpeedScale = Mathf.Clamp(raw, min, max);
	}

	private void TryStartNewStep()
	{
		// Враховуємо скейловану паузу між кроками
		if (Time.time - _lastGlobalStepTime < ScaledGlobalMinIntervalBetweenSteps)
			return;

		if (_currentSteppingCount >= Mathf.Max(1, maxSimultaneousSteps))
			return;

		MechLeg bestLeg = null;
		float bestScore = ScaledStepDistanceThreshold;

		for (int i = 0; i < _legs.Count; i++)
		{
			var leg = _legs[i];
			if (leg == null || leg.IsStepping)
				continue;

			float score = leg.GetStepScore();
			if (score > bestScore)
			{
				bestScore = score;
				bestLeg = leg;
			}
		}

		if (bestLeg == null)
			return;

		Vector3 targetPos = bestLeg.GetIdealFootPosition();
		bestLeg.BeginStep(targetPos);

		_currentSteppingCount++;
		_lastGlobalStepTime = Time.time;
	}
}
