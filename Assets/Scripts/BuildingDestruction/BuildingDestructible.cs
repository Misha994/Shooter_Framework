using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

/// <summary>
/// Менеджер будівлі: відслідковує знищення секцій і тригерить "total collapse".
/// Працює поверх твоїх DestructibleSection (які вже підтримують DamagePayload).
/// </summary>
public class BuildingDestructible : MonoBehaviour
{
	[Header("Collect")]
	[Tooltip("Якщо порожньо — зберемо всі DestructibleSection в дочірніх.")]
	[SerializeField] private List<DestructibleSection> sections = new();

	[Header("Collapse Rules")]
	[Tooltip("Зруйнувати всю будівлю, якщо відсоток знищених секцій >= цьому порогу.")]
	[Range(0f, 1f)] public float collapseByDestroyedRatio = 0.6f;

	[Tooltip("Несучі секції: якщо з них знищено >= requiredCarriersToFail — також колапс.")]
	[SerializeField] private List<DestructibleSection> carrierSections = new();
	[SerializeField] private int requiredCarriersToFail = 2;

	[Header("Collapse Result (embedded ruins)")]
	[Tooltip("Об'єкт руїн, який ВЖЕ лежить у префабі будинку (дитина/об'єкт у сцені).\n" +
			 "На старті він буде вимкнений, при колапсі — увімкнеться.")]
	[SerializeField] private GameObject ruinsPrefab;

	// ruinsSpawnPoint залишив як поле, але не використовую – на випадок якщо колись захочеш повернути спавн
	[SerializeField] private Transform ruinsSpawnPoint;

	[Header("Vanish Settings (when no ruinsPrefab)")]
	[SerializeField] private bool disableCollidersOnCollapse = true;
	[SerializeField] private bool disableRenderersOnCollapse = true;
	[SerializeField] private bool destroyGameObjectOnCollapse = false;
	[SerializeField] private float destroyDelay = 0f;

	[Header("Debug")]
	[SerializeField] private bool debugLogs = true;

	private int _totalSections;
	private int _destroyedSections;
	private int _destroyedCarriers;
	private bool _collapsed;

	[Inject(Optional = true)] private SignalBus _bus;

	private void Awake()
	{
		// Якщо список не заданий — збираємо всі секції з дочірніх
		if (sections == null || sections.Count == 0)
		{
			sections = new List<DestructibleSection>(GetComponentsInChildren<DestructibleSection>(true));
		}

		_totalSections = 0;
		_destroyedSections = 0;
		_destroyedCarriers = 0;

		foreach (var s in sections)
		{
			if (!s) continue;
			_totalSections++;
			s.Destroyed += OnSectionDestroyed;
		}

		// На старті переконуємось, що глобальна руїна вимкнена
		if (ruinsPrefab != null && ruinsPrefab.activeSelf)
			ruinsPrefab.SetActive(false);

		if (debugLogs)
			Debug.Log($"[Building] '{name}' collected {_totalSections} sections. Carriers: {carrierSections?.Count ?? 0}");
	}

	private void OnDestroy()
	{
		if (sections != null)
		{
			foreach (var s in sections)
			{
				if (s != null) s.Destroyed -= OnSectionDestroyed;
			}
		}
	}

	private void OnSectionDestroyed(DestructibleSection s)
	{
		if (_collapsed) return;

		_destroyedSections++;

		bool wasCarrier = (carrierSections != null && carrierSections.Contains(s));
		if (wasCarrier) _destroyedCarriers++;

		if (debugLogs)
		{
			float ratio = _totalSections > 0 ? (float)_destroyedSections / _totalSections : 0f;
			Debug.Log($"[Building] Section destroyed: {s.name}. " +
					  $"Destroyed {_destroyedSections}/{_totalSections} ({ratio:P0}). " +
					  $"Carriers destroyed: {_destroyedCarriers}/{Mathf.Max(1, requiredCarriersToFail)}");
		}

		EvaluateCollapse();
	}

	private void EvaluateCollapse()
	{
		if (_collapsed) return;

		bool ratioRule = _totalSections > 0 &&
						 ((float)_destroyedSections / _totalSections) >= collapseByDestroyedRatio;

		bool carrierRule = (carrierSections != null && carrierSections.Count > 0) &&
						   _destroyedCarriers >= Mathf.Max(1, requiredCarriersToFail);

		if (ratioRule || carrierRule)
		{
			Collapse();
		}
	}

	private void Collapse()
	{
		if (_collapsed) return;
		_collapsed = true;

		if (debugLogs)
			Debug.Log($"[Building] '{name}' COLLAPSE TRIGGERED!");

		if (ruinsPrefab != null)
		{
			// Вмикаємо глобальну руїну (об'єкт у сцені/дитина будинку)
			ruinsPrefab.SetActive(true);

			// Вимикаємо рендери/колайдери ВСЬОГО, крім самої руїни
			DisableVisualsAndCollidersExcept(ruinsPrefab.transform);

			// Root будинку НЕ вимикаємо, щоб руїна (як дитина) залишалась активною.
		}
		else
		{
			// Якщо глобальна руїна не задана – працюємо старим способом (vanish / destroy)
			DisableVisualsAndCollidersExcept(null);

			if (destroyGameObjectOnCollapse)
				Destroy(gameObject, Mathf.Max(0f, destroyDelay));
			else
				gameObject.SetActive(false);
		}

		// Zenject-сигнал (якщо треба реагувати ззовні)
		_bus?.TryFire(new BuildingCollapsedSignal(this));
	}

	/// <summary>
	/// Вимикає всі Renderer/Collider у дочірніх, крім гілки, що належить exceptRoot (якщо не null).
	/// </summary>
	private void DisableVisualsAndCollidersExcept(Transform exceptRoot)
	{
		if (disableRenderersOnCollapse)
		{
			foreach (var r in GetComponentsInChildren<Renderer>(true))
			{
				if (exceptRoot != null && r.transform.IsChildOf(exceptRoot))
					continue;
				r.enabled = false;
			}
		}

		if (disableCollidersOnCollapse)
		{
			foreach (var c in GetComponentsInChildren<Collider>(true))
			{
				if (exceptRoot != null && c.transform.IsChildOf(exceptRoot))
					continue;
				c.enabled = false;
			}
		}
	}
}

public struct BuildingCollapsedSignal
{
	public readonly BuildingDestructible Building;
	public BuildingCollapsedSignal(BuildingDestructible b) { Building = b; }
}
