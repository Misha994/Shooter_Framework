// Assets/Combat/Runtime/MultiSection/MultiSectionEntityHealth.cs
using System.Collections.Generic;
using UnityEngine;

namespace Combat.Core
{
	/// Керує "смертю" всього ворога + знає про секції.
	[DisallowMultipleComponent]
	public sealed class MultiSectionEntityHealth : MonoBehaviour
	{
		[SerializeField] private HealthComponent mainHealth;
		[SerializeField] private MonoBehaviour[] behavioursToDisableOnDeath;
		[SerializeField] private GameObject[] objectsToDestroyOnDeath;
		[SerializeField] private GameObject deathFxPrefab;

		private bool _isDead;
		private readonly Dictionary<string, SectionHealth> _sectionsById = new();

		private void Awake()
		{
			if (!mainHealth)
				mainHealth = GetComponent<HealthComponent>();

			// збираємо секції
			var sections = GetComponentsInChildren<SectionHealth>(includeInactive: true);
			foreach (var sec in sections)
			{
				if (sec == null) continue;
				if (!string.IsNullOrEmpty(sec.SectionId))
					_sectionsById[sec.SectionId] = sec;
			}

			if (mainHealth != null)
				mainHealth.Died += OnMainHealthDied;
		}

		private void OnDestroy()
		{
			if (mainHealth != null)
				mainHealth.Died -= OnMainHealthDied;
		}

		private void OnMainHealthDied()
		{
			if (_isDead) return;
			KillEntity();
		}

		/// Викликається SectionHealth, коли секція помирає.
		public void KillFromSection(SectionHealth section)
		{
			if (_isDead) return;

			// Варіант А: секція вбиває ще й "офіційний" mainHealth:
			if (mainHealth != null && !mainHealth.IsDead)
			{
				mainHealth.Kill(); // → OnMainHealthDied → KillEntity()
			}
			else
			{
				KillEntity();
			}
		}

		private void KillEntity()
		{
			if (_isDead) return;
			_isDead = true;

			// Вимикаємо поведінку (рух, стрільба, ІІ...)
			if (behavioursToDisableOnDeath != null)
			{
				for (int i = 0; i < behavioursToDisableOnDeath.Length; i++)
				{
					if (behavioursToDisableOnDeath[i] != null)
						behavioursToDisableOnDeath[i].enabled = false;
				}
			}

			// Ламаємо / ховаємо обʼєкти
			if (objectsToDestroyOnDeath != null)
			{
				for (int i = 0; i < objectsToDestroyOnDeath.Length; i++)
				{
					var go = objectsToDestroyOnDeath[i];
					if (go != null)
						Destroy(go);
				}
			}

			// FX смерті
			if (deathFxPrefab != null)
			{
				Instantiate(deathFxPrefab, transform.position, transform.rotation);
			}
		}

		// опціонально: доступ до секції по id
		public SectionHealth GetSection(string id)
		{
			_sectionsById.TryGetValue(id, out var s);
			return s;
		}
	}
}
