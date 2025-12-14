using System.Collections.Generic;
using UnityEngine;

public class MechGaitController : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Rigidbody rb;
	[SerializeField] private Transform body;
	[SerializeField] private MechWalkerMotor motor;

	[Header("Gait Settings")]
	[Tooltip("Скільки ніг може робити крок одночасно.")]
	[SerializeField] private int maxSimultaneousSteps = 1;

	[Tooltip("Мінімальний час між стартами кроків (глобально по всіх ногах).")]
	[SerializeField] private float globalMinIntervalBetweenSteps = 0.1f;

	[Tooltip("Мінімальний score, щоб нога взагалі почала крок.")]
	[SerializeField] private float stepDistanceThreshold = 0.7f;

	// Ті ж non-serialized поля, що й у префабі
	[System.NonSerialized] private readonly List<MechLeg> _legs = new List<MechLeg>();
	[System.NonSerialized] private float _lastGlobalStepTime;
	[System.NonSerialized] private int _currentSteppingCount;
	[System.NonSerialized] private int _nextLegIndex; // не обов’язково, але лишаємо для сумісності/можливих розширень
	[System.NonSerialized] private float _cachedSpeed;

	public Transform BodyTransform => body != null ? body : transform;
	public IReadOnlyList<MechLeg> Legs => _legs;

	private void Awake()
	{
		if (rb == null)
			rb = GetComponent<Rigidbody>();

		if (body == null)
			body = transform;
	}

	private void Start()
	{
		// Автоматично знайти всі MechLeg в дітях (наприклад Mech_Legs, Mech_Legs(1), Mech_Legs(2)...)
		var found = GetComponentsInChildren<MechLeg>(true);
		foreach (var leg in found)
		{
			RegisterLeg(leg);
		}

		// Ініціалізація лап
		foreach (var leg in _legs)
		{
			if (leg != null)
				leg.Initialize(body);
		}
	}

	/// <summary>
	/// Викликається з MechLeg.Awake() та зі Start() при автопошуку.
	/// </summary>
	public void RegisterLeg(MechLeg leg)
	{
		if (leg == null || _legs.Contains(leg))
			return;

		_legs.Add(leg);
		// Сортуємо по legIndex, щоб control був стабільним (0,1,2,3,...)
		_legs.Sort((a, b) => a.LegIndex.CompareTo(b.LegIndex));
	}

	private void Update()
	{
		if (_legs.Count == 0)
			return;

		float dt = Time.deltaTime;

		// Оновити всі ноги й відстежити, які завершили крок
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

	private void TryStartNewStep()
	{
		// Глобальний кулдаун між стартами кроків
		if (Time.time - _lastGlobalStepTime < globalMinIntervalBetweenSteps)
			return;

		if (_currentSteppingCount >= Mathf.Max(1, maxSimultaneousSteps))
			return;

		_cachedSpeed = rb != null ? rb.velocity.magnitude : 0f;

		MechLeg bestLeg = null;
		float bestScore = stepDistanceThreshold;

		// Вибрати ногу, яка найбільше "потребує" кроку
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
