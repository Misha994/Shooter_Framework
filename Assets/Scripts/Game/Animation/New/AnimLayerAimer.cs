using UnityEngine;
using Zenject;

[DefaultExecutionOrder(90)]
public class AnimLayerAimer : MonoBehaviour
{
	[Header("Animator")]
	[SerializeField] private Animator anim;
	[SerializeField] private string aimParam = "Aim01";
	[SerializeField] private int upperBodyLayer = 1; // шар із маскою верхнього тіла

	[Header("Blend")]
	[SerializeField] private float aimLerpSpeed = 10f;   // швидкість виходу в ADS
	[SerializeField] private float hipLerpSpeed = 10f;   // швидкість повернення в HIP

	[Header("State")]
	[SerializeField] private bool hasWeapon = true;
	[SerializeField] private bool disableUpperBodyWhileSprinting = true;

	[Inject(Optional = true)] private IInputService _input;
	[Inject(Optional = true)] private SignalBus _bus;

	private float _aim01;
	private bool _isAiming;     // актуальний aim-стан
	private bool _isSprinting;

	private void Reset()
	{
		anim = GetComponentInChildren<Animator>();
	}

	private void OnEnable()
	{
		_bus?.Subscribe<AimStateChangedSignal>(OnAimChanged);
	}

	private void OnDisable()
	{
		_bus?.TryUnsubscribe<AimStateChangedSignal>(OnAimChanged);
	}

	public void SetHasWeapon(bool v) => hasWeapon = v;

	private void OnAimChanged(AimStateChangedSignal s)
	{
		_isAiming = s.IsAiming;
	}

	private void Update()
	{
		// Якщо SignalBus відсутній – aim і sprint читаємо з інпуту напряму
		if (_bus == null && _input != null)
		{
			_isAiming = _input.IsAimHeld();
			_isSprinting = _input.IsSprintHeld();
		}
		else
		{
			// Якщо SignalBus є – aim приходить із сигналу, а спринт усе одно читаємо з інпуту
			if (_input != null)
				_isSprinting = _input.IsSprintHeld();
		}

		float target = _isAiming ? 1f : 0f;
		float speed = _isAiming ? aimLerpSpeed : hipLerpSpeed;
		_aim01 = Mathf.MoveTowards(_aim01, target, speed * Time.deltaTime);

		if (anim)
		{
			// ЄДИНЕ місце, де виставляється Aim01
			anim.SetFloat(aimParam, _aim01);

			// Вага шару верхнього тіла
			float layerW = (hasWeapon && (!disableUpperBodyWhileSprinting || !_isSprinting)) ? 1f : 0f;
			if (upperBodyLayer >= 0 && upperBodyLayer < anim.layerCount)
				anim.SetLayerWeight(upperBodyLayer, layerW);
		}
	}
}
