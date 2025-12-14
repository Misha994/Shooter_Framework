// ----------------------------
// File: Assets/Combat/Runtime/HealthComponent.cs
// ----------------------------
using System;
using UnityEngine;

namespace Combat.Core
{
    /// Óí³âåðñàëüíå «æèòòÿ» íà floaò — áåç ëº´àñ³-³âåíò-ôîëáåê³â ³ int-àðèôìåòèêè.
    [DisallowMultipleComponent]
    public sealed class HealthComponent : MonoBehaviour
    {
        [Min(1f)][SerializeField] private float max = 100f;
        [SerializeField] private bool destroyOnDeath = true;

        public float Max => max;
        public float Current => current;
        public bool IsDead { get; private set; }

        public event Action<float, float> Changed; // (current, max)
        public event Action Died;

        private float current;

        private void Awake()
        {
            current = Mathf.Clamp(current <= 0 ? max : current, 0f, max);
            IsDead = current <= 0f;
            Changed?.Invoke(current, max);
        }

        public void ApplyDamage(float amount, in DamagePayload source)
        {
            if (IsDead || amount <= 0f) return;
            SetCurrent(current - amount);
        }

        public void Heal(float amount)
        {
            if (IsDead || amount <= 0f) return;
            SetCurrent(current + amount);
        }

        public void SetMax(float newMax, bool clampCurrent = true)
        {
            max = Mathf.Max(1f, newMax);
            if (clampCurrent) current = Mathf.Clamp(current, 0f, max);
            Changed?.Invoke(current, max);
        }

        public void Kill()
        {
            if (IsDead) return;
            SetCurrent(0f);
        }

        private void SetCurrent(float value)
        {
            float before = current;
            current = Mathf.Clamp(value, 0f, max);
            if (!Mathf.Approximately(before, current))
                Changed?.Invoke(current, max);

            if (!IsDead && current <= 0f)
            {
                IsDead = true;
                Died?.Invoke();
                if (destroyOnDeath) Destroy(gameObject);
            }
        }
    }
}
