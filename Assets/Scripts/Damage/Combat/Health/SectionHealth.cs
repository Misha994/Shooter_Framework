// Assets/Combat/Runtime/MultiSection/SectionHealth.cs
using UnityEngine;

namespace Combat.Core
{
	/// Секція з власним HP (нога, голова, модуль і т.д.).
	/// Приймає урон (IDamageReceiver) і при смерті може:
	/// - просто знищити свою частину
	/// - і/або вбити весь мех через MultiSectionEntityHealth.
	[DisallowMultipleComponent]
	public sealed class SectionHealth : MonoBehaviour, IDamageReceiver
	{
		[SerializeField] private string sectionId = "Unnamed";
		[SerializeField] private HealthComponent health;

		[Header("Death Behaviour")]
		[SerializeField] private bool killEntityOnDeath = false;           // <- чи валити весь мех
		[SerializeField] private bool destroySectionOnDeath = true;        // <- чи ламати візуал секції
		[SerializeField] private GameObject[] extraObjectsToDestroy;       // <- додаткові частини (моделі, FX-кілки)

		private MultiSectionEntityHealth _entity;

		public string SectionId => sectionId;
		public HealthComponent Health => health;

		private void Awake()
		{
			if (!health)
				health = GetComponent<HealthComponent>();

			_entity = GetComponentInParent<MultiSectionEntityHealth>();

			if (health != null)
				health.Died += OnSectionDied;
		}

		private void OnDestroy()
		{
			if (health != null)
				health.Died -= OnSectionDied;
		}

		/// Сюди прилітає урон з Projectile / Laser через IDamageReceiver.
		public void TakeDamage(in DamagePayload payload)
		{
			if (health == null || health.IsDead)
				return;

			// тут можна робити локальні резісти, мультиплікатори і т.д.
			health.ApplyDamage(payload.Damage, in payload);
		}

		private void OnSectionDied()
		{
			// 1) Ламаємо / ховаємо секцію
			if (destroySectionOnDeath)
			{
				// або Destroy(gameObject), або тільки візуал, якщо Health сидить на якомусь вузлі
				Destroy(gameObject);
			}

			if (extraObjectsToDestroy != null)
			{
				for (int i = 0; i < extraObjectsToDestroy.Length; i++)
				{
					if (extraObjectsToDestroy[i] != null)
						Destroy(extraObjectsToDestroy[i]);
				}
			}

			// 2) Вбиваємо весь мех, якщо секція критична
			if (killEntityOnDeath && _entity != null)
			{
				_entity.KillFromSection(this);
			}
		}
	}
}
