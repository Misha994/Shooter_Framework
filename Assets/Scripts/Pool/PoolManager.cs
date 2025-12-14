using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
	public static PoolManager Instance { get; private set; }

	private readonly Dictionary<GameObject, Queue<GameObject>> _pool = new();          // key: prefab
	private readonly Dictionary<GameObject, GameObject> _instanceToPrefab = new();     // inst -> prefab

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStatics() => Instance = null;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	public void Warmup(GameObject prefab, int count, Transform parent = null)
	{
		if (prefab == null || count <= 0) return;

		if (!_pool.ContainsKey(prefab))
			_pool[prefab] = new Queue<GameObject>();

		var queue = _pool[prefab];

		for (int i = 0; i < count; i++)
		{
			var go = CreateNew(prefab, parent);
			// go вже буде неактивний після CreateNew, SetActive(false) тут – просто подвійна гарантія.
			if (go.activeSelf)
				go.SetActive(false);

			queue.Enqueue(go);
		}
	}

	public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null)
	{
		if (prefab == null)
		{
			Debug.LogError("[PoolManager] Spawn called with NULL prefab.");
			return null;
		}

		if (!_pool.TryGetValue(prefab, out var queue))
		{
			queue = new Queue<GameObject>();
			_pool[prefab] = queue;
		}

		GameObject inst = queue.Count > 0 ? queue.Dequeue() : CreateNew(prefab, parent);

		// Оновлюємо батька, якщо треба
		if (parent != null && inst.transform.parent != parent)
			inst.transform.SetParent(parent, false);

		// 1) Спочатку СТАВИМО позицію/ротацію
		inst.transform.SetPositionAndRotation(pos, rot);

		// 2) Потім активуємо (OnEnable бачить уже правильну позицію)
		if (!inst.activeSelf)
			inst.SetActive(true);

		// 3) Нотифікуємо пул-об’єкт
		if (inst.TryGetComponent<PooledObject>(out var po))
			po.OnSpawned();

		return inst;
	}

	public void Despawn(GameObject instance, float delaySeconds = 0f)
	{
		if (instance == null) return;

		if (delaySeconds > 0f)
		{
			StartCoroutine(DespawnDelayed(instance, delaySeconds));
			return;
		}

		if (!_instanceToPrefab.TryGetValue(instance, out var prefab))
		{
			// Instance не з нашого пулу — знищимо, щоб не текло
			Debug.LogWarning("[PoolManager] Trying to Despawn an object that was not pooled. Destroying.");
			Destroy(instance);
			return;
		}

		// Сповіщаємо BEFORE disabling
		if (instance.TryGetComponent<PooledObject>(out var po))
			po.OnDespawned();

		instance.SetActive(false);

		if (instance.transform is RectTransform rt)
			rt.anchoredPosition3D = Vector3.zero; // для UI

		_pool[prefab].Enqueue(instance);
	}

	private System.Collections.IEnumerator DespawnDelayed(GameObject instance, float time)
	{
		yield return new WaitForSeconds(time);
		Despawn(instance, 0f);
	}

	private GameObject CreateNew(GameObject prefab, Transform parent)
	{
		// Створюємо інстанс
		var go = Instantiate(prefab, parent);

		// ВАЖЛИВО: ми НЕ розраховуємося на OnEnable при першому створенні,
		// вся ігрова логіка кулі буде робитися в Init/ResetStateOnSpawn.
		// Тому просто робимо його неактивним для подальшого використання пулом.
		if (go.activeSelf)
			go.SetActive(false);

		// Маркер пулу
		var po = go.GetComponent<PooledObject>();
		if (po == null)
			po = go.AddComponent<PooledObject>();

		po.SourcePrefab = prefab;

		_instanceToPrefab[go] = prefab;
		return go;
	}
}
