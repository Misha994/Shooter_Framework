// Assets/Combat/Runtime/DamageReceiver.cs  (ОНОВИТИ)
using System;
using UnityEngine;

namespace Combat.Core
{
	[DisallowMultipleComponent]
	public sealed class DamageReceiver : MonoBehaviour, IDamageReceiver
	{
		[Header("Core")]
		[SerializeField] private HealthComponent health;

		// Зворотна сумісність (старі поля лишаємо, але переводимо на модифікатори)
		[Header("Compatibility (optional)")]
		[SerializeField] private ResistMatrix legacyResistAsset;

		// Подія: (жертва, оригінальний payload, фактичний урон після всіх модифікаторів)
		public event Action<GameObject, DamagePayload, float> Damaged;

		// Кеш модифікаторів (щоб не робити GetComponents щоразу)
		private IDamageModifier[] _modifiers;

		private void Awake()
		{
			if (!health)
				health = GetComponent<HealthComponent>();

			// зібрати всі модифікатори на цьому ж GO
			_modifiers = GetComponents<IDamageModifier>();

			// нічого критичного, якщо немає health — просто не отримаємо урон
			if (!health)
				Debug.LogWarning($"[DamageReceiver] No HealthComponent on '{name}'. Damage will be ignored.", this);
		}

		public void TakeDamage(in DamagePayload payload)
		{
			if (!isActiveAndEnabled || health == null || health.IsDead) return;

			float dmg = Mathf.Max(0f, payload.Damage);

			// 1) Плагіновий ланцюг модифікаторів
			var mods = _modifiers;
			for (int i = 0; i < mods.Length; i++)
				dmg = Mathf.Max(0f, mods[i].Modify(in payload, dmg));

			// 2) Зворотна сумісність зі старими полями (поступовий перехід)
			if (legacyResistAsset != null)
				dmg *= Mathf.Max(0f, legacyResistAsset.Get(payload.Type));

			if (dmg <= 0f) return;

			// 3) Сигналізуємо слухачам (AI / UI / FX)
			Damaged?.Invoke(gameObject, payload, dmg);

			// 4) Накладаємо урон
			health.ApplyDamage(dmg, in payload);
		}
	}
}
