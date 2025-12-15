using UnityEngine;
using UnityEngine.Animations.Rigging;
using Zenject;

/// <summary>
/// Керує вагами рігів (aim / hands / additive) з одного джерела:
///  - Animator (Aim01, IsSprinting, IsReloading)
/// АБО явних викликів SetReloading/SetHasWeapon/SetAim01 (для AI).
/// Для Player:
///  - Aim01 пише AnimLayerAimer
///  - стан спринту/релоду віддає AnimatorParamDriver / стейт-машина.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Animator))]
public class RigWeightBlender : MonoBehaviour
{
	[Header("Rigs")]
	[SerializeField] private Rig aimRig;      // MultiAim/MultiRotation тулуб/голова
	[SerializeField] private Rig handsRig;    // TwoBoneIK руки (спочатку 0)
	[SerializeField] private Rig additiveRig; // дрібні добавки (як правило 1)

	[Header("Weights")]
	[Range(0, 1)][SerializeField] private float hipAim = 0.7f;   // вага aimRig в hip
	[Range(0, 1)][SerializeField] private float ads = 1.0f;      // вага aimRig в ADS
	[Range(0, 1)][SerializeField] private float sprintMul = 0f;  // множник при спринті (0 = вимкнути/заморозити верх)

	[SerializeField] private float blendSpeed = 10f;
	[SerializeField] private bool ignoreSprintWhileReloading = true;

	[Header("Animator")]
	[SerializeField] private Animator anim;
	[SerializeField] private string paramIsSprinting = "IsSprinting";
	[SerializeField] private string paramIsReloading = "IsReloading";

	[Tooltip("Якщо true – основне джерело Aim01 це Animator float (наприклад, AnimLayerAimer). Якщо false – очікується прямий SetAim01 або інпут.")]
	[SerializeField] private bool useAnimatorAimFallback = true;

	[SerializeField] private string aimFloatParam = "Aim01";

	[Header("State (runtime)")]
	[SerializeField] private bool hasWeapon = true;

	private bool _explicitReloading;
	private bool _hasSprintParam, _hasReloadParam, _hasAimFloatParam;

	// Fallback лише для сцен без Animator Aim01 (тестові/AI без AnimLayerAimer).
	[Inject(Optional = true)] private IInputService _input;

	void Reset() => AutoWire();

	void Awake()
	{
		AutoWire();
		CacheAnimatorParams();
	}

	private void AutoWire()
	{
		if (!anim)
			anim = GetComponent<Animator>();

		if (!aimRig)
			aimRig = GetComponentInChildren<Rig>(true);
		// handsRig/additiveRig — задаємо через інспектор
	}

	private void CacheAnimatorParams()
	{
		if (!anim) return;
		_hasSprintParam = HasParam(paramIsSprinting, AnimatorControllerParameterType.Bool);
		_hasReloadParam = HasParam(paramIsReloading, AnimatorControllerParameterType.Bool);
		_hasAimFloatParam = HasParam(aimFloatParam, AnimatorControllerParameterType.Float);
	}

	private bool HasParam(string name, AnimatorControllerParameterType type)
	{
		foreach (var p in anim.parameters)
			if (p.type == type && p.name == name)
				return true;
		return false;
	}

	// === Public API ===========================================================
	public void SetHasWeapon(bool v) => hasWeapon = v;
	public void SetReloading(bool v) => _explicitReloading = v;

	/// <summary>
	/// Пряме встановлення Aim (0=hip,1=ADS), якщо не хочеш залежати від Animator/інпуту (наприклад, для AI).
	/// </summary>
	public void SetAim01(float aim01)
	{
		Apply(aim01, ReadSprinting(), ReadReloading());
	}

	// === Update loop ==========================================================
	private void Update()
	{
		// 1) Джерело aim01:
		//    ГОЛОВНЕ – Animator.Aim01 (AnimLayerAimer),
		//    fallback – інпут (для сцен без AnimLayerAimer).
		float aim01 = 0f;

		if (useAnimatorAimFallback && anim && _hasAimFloatParam)
		{
			aim01 = anim.GetFloat(aimFloatParam);
		}
		else if (_input != null)
		{
			aim01 = _input.IsAimHeld() ? 1f : 0f;
		}

		// 2) Стани спринт/релод
		bool sprinting = ReadSprinting();
		bool reloading = ReadReloading();

		Apply(aim01, sprinting, reloading);
	}

	private void Apply(float aim01, bool sprinting, bool reloading)
	{
		// Вага aim-ріга між hip↔ADS
		float targetAim = Mathf.Lerp(hipAim, ads, Mathf.Clamp01(aim01));

		// Спринт приглушує верх (або ігноруємо, якщо релод)
		if (sprinting && !(ignoreSprintWhileReloading && reloading))
			targetAim *= Mathf.Clamp01(1f - sprintMul);

		// Hands вмикаємо тільки коли є зброя
		float targetHands = hasWeapon ? 1f : 0f;

		// Плавно
		LerpRig(aimRig, targetAim);
		LerpRig(handsRig, targetHands);
		if (additiveRig)
			LerpRig(additiveRig, 1f);
	}

	private void LerpRig(Rig rig, float target)
	{
		if (!rig) return;
		float w = Mathf.MoveTowards(rig.weight, target, blendSpeed * Time.deltaTime);
		rig.weight = w;
	}

	private bool ReadSprinting() =>
		anim && _hasSprintParam && anim.GetBool(paramIsSprinting);

	private bool ReadReloading()
	{
		bool v = _explicitReloading;
		if (anim && _hasReloadParam)
			v |= anim.GetBool(paramIsReloading);
		return v;
	}
}
