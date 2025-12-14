// Assets/Destruction/Runtime/DestructibleSection.cs
using UnityEngine;
using Combat.Core;
using System;

[RequireComponent(typeof(HealthComponent))]
[DisallowMultipleComponent]
public class DestructibleSection : MonoBehaviour
{
	[Header("Profile")]
	[SerializeField] private SectionDestructionProfile profile;

	[Header("Mesh Swap / Debris (runtime prefab)")]
	[Tooltip("Якщо false і задано embeddedFracturedRoot, то нічого не спавнимо, а лише перемикаємо гілки в сцені.")]
	[SerializeField] private bool usePrefabSpawn = true;

	[SerializeField] private GameObject fracturedPrefab;
	[SerializeField] private Transform fracturedSpawnPoint;

	[Header("Embedded fractured (усе вже в префабі)")]
	[Tooltip("Гілка з цілим мешем. Якщо null — використаємо gameObject, але тоді fracturedRoot НЕ має бути його нащадком.")]
	[SerializeField] private GameObject intactRootOverride;

	[Tooltip("Гілка з розбитими шматками (має бути вимкнена у префабі).")]
	[SerializeField] private GameObject embeddedFracturedRoot;

	[Header("VFX")]
	[SerializeField] private GameObject hitVfx;
	[SerializeField] private GameObject breakVfx;

	[Header("Pooling / Vanish")]
	[SerializeField] private bool usePooling = true;

	[Tooltip("Як поводитись, якщо немає ні fracturedPrefab (або usePrefabSpawn=false), ні embeddedFracturedRoot.")]
	[SerializeField] private bool vanishOnBreakIfNoSwap = true;

	[SerializeField] private bool disableCollidersOnVanish = true;
	[SerializeField] private bool disableRenderersOnVanish = true;
	[SerializeField] private bool destroyGameObjectOnVanish = false;
	[SerializeField] private float destroyDelay = 0f;

	public event Action<DestructibleSection> Destroyed;

	private HealthComponent _health;
	private DamageReceiver _receiver;

	private void Awake()
	{
		_health = GetComponent<HealthComponent>();
		if (_health && profile != null)
		{
			// даємо секції HP з профілю
			_health.SetMax(profile.maxHealth, clampCurrent: false);
		}

		// Під’єднати модифікатор (якщо ще не стоїть)
		_receiver = GetComponent<DamageReceiver>();
		if (!_receiver)
			_receiver = gameObject.AddComponent<DamageReceiver>();

		if (!GetComponent<SectionDamageModifier>())
		{
			var mod = gameObject.AddComponent<SectionDamageModifier>();
			// профіль прокидаємо через SendMessage, щоб не ломати існуючий код модифікатора
			mod.SendMessage("SetProfile", profile, SendMessageOptions.DontRequireReceiver);
		}

		if (hitVfx && _receiver != null)
			_receiver.Damaged += OnDamaged;
	}

	private void OnEnable()
	{
		if (_health != null) _health.Died += OnDied;
	}

	private void OnDisable()
	{
		if (_health != null) _health.Died -= OnDied;
		if (_receiver != null) _receiver.Damaged -= OnDamaged;
	}

	private void OnDamaged(GameObject victim, DamagePayload p, float amount)
	{
		if (hitVfx)
			SpawnFx(hitVfx, p.HitPoint, Quaternion.LookRotation(p.HitNormal));
	}

	private void OnDied()
	{
		// 1) break VFX (центр – точка спавну фрагментів або сам об'єкт)
		if (breakVfx)
		{
			var center = fracturedSpawnPoint ? fracturedSpawnPoint.position : transform.position;
			SpawnFx(breakVfx, center, Quaternion.identity);
		}

		// 2) Візуальний перехід у стан "зруйновано"
		bool handled = false;

		// 2.1. Якщо є embedded-модель – ПЕРШОЧЕРГОВО використовуємо її
		if (embeddedFracturedRoot != null)
		{
			HandleEmbeddedSwap();
			handled = true;
		}
		// 2.2. Інакше, якщо дозволено спавн префаба – старий режим
		else if (usePrefabSpawn && fracturedPrefab != null)
		{
			HandlePrefabSpawn();
			handled = true;
		}

		// 2.3. Якщо нічого із вище – fall-back: просто "зникаємо" / відключаємось
		if (!handled)
		{
			HandleVanishNoSwap();
		}

		// 3) Сповістити BuildingDestructible / інші слухачі
		Destroyed?.Invoke(this);
	}

	// ================== ВІЗУАЛЬНІ РЕЖИМИ ==================

	/// <summary>
	/// Новий варіант: нічого не створюємо, просто гасимо "цілий" меш і вмикаємо "зруйнований".
	/// </summary>
	private void HandleEmbeddedSwap()
	{
		// 1) Увімкнути розбиту гілку (має бути Inactive у префабі)
		if (!embeddedFracturedRoot.activeSelf)
			embeddedFracturedRoot.SetActive(true);

		// 2) Вимкнути рендери/колайдери лише на цілій гілці
		var intactRoot = intactRootOverride != null ? intactRootOverride : gameObject;

		if (disableRenderersOnVanish)
		{
			foreach (var r in intactRoot.GetComponentsInChildren<Renderer>(true))
				r.enabled = false;
		}

		if (disableCollidersOnVanish)
		{
			foreach (var c in intactRoot.GetComponentsInChildren<Collider>(true))
				c.enabled = false;
		}

		// ВАЖЛИВО:
		// Не робимо intactRoot.SetActive(false) за замовчуванням,
		// щоб:
		// - не відрубати сам DestructibleSection (він може бути на root),
		// - не погасити embeddedFracturedRoot, якщо той випадково є нащадком.
		//
		// Якщо хочеш повністю відключати цілу гілку "Intact" – можна
		// або винести логіку на окремий parent, або додати ще один прапорець типу
		// "deactivateIntactRootOnEmbeddedSwap".
	}

	/// <summary>
	/// Старий варіант: спавнимо fracturedPrefab і вимикаємо початкову секцію.
	/// </summary>
	private void HandlePrefabSpawn()
	{
		var pos = fracturedSpawnPoint ? fracturedSpawnPoint.position : transform.position;
		var rot = fracturedSpawnPoint ? fracturedSpawnPoint.rotation : transform.rotation;

		if (usePooling && PoolManager.Instance)
			PoolManager.Instance.Spawn(fracturedPrefab, pos, rot);
		else
			Instantiate(fracturedPrefab, pos, rot);

		// Як було раніше – просто вимикаємо секцію цілком.
		gameObject.SetActive(false);
	}

	/// <summary>
	/// Fall-back, коли нема ні embedded fractured, ні префаба (або usePrefabSpawn=false).
	/// </summary>
	private void HandleVanishNoSwap()
	{
		if (!vanishOnBreakIfNoSwap)
		{
			gameObject.SetActive(false);
			return;
		}

		if (disableRenderersOnVanish)
		{
			foreach (var r in GetComponentsInChildren<Renderer>(true))
				r.enabled = false;
		}

		if (disableCollidersOnVanish)
		{
			foreach (var c in GetComponentsInChildren<Collider>(true))
				c.enabled = false;
		}

		if (destroyGameObjectOnVanish)
			Destroy(gameObject, Mathf.Max(0f, destroyDelay));
		else
			gameObject.SetActive(false);
	}

	private void SpawnFx(GameObject fx, Vector3 pos, Quaternion rot)
	{
		if (!fx) return;

		if (usePooling && PoolManager.Instance)
			PoolManager.Instance.Spawn(fx, pos, rot);
		else
			Instantiate(fx, pos, rot);
	}
}
