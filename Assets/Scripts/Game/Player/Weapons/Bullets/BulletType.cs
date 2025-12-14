// Assets/Combat/Bullets/BulletType.cs
using UnityEngine;

namespace Combat.Core
{
    /// Базовий тип кулі. ЄДИНИЙ контракт — робота з DamagePayload.
    public abstract class BulletType : ScriptableObject
    {
        [Header("VFX")]
        public string bulletName;
        public GameObject hitVFX;

        [Min(0f)] public float damageMultiplier = 1f;

        /// НЕ абстрактний метод: фінальний сценарій попадання.
        /// 1) Preprocess (можна змінити payload, напр. підняти penetration)
        /// 2) VFX
        /// 3) Нанесення шкоди (з урахуванням damageMultiplier)
        /// 4) Postprocess (ефекти на кшталт burn/poison)
        public void OnHit(GameObject target, in DamagePayload payload)
        {
            if (target == null) return;

            var pre = Preprocess(in payload);

            // застосувати множник саме тут, однаково для всіх куль
            var final = pre.WithDamage(pre.Damage * Mathf.Max(0f, damageMultiplier));

            SpawnVfx(in final);

            target.GetComponentInParent<IDamageReceiver>()?.TakeDamage(in final);

            Postprocess(target, in final);
        }

        /// Дозволяє змінити payload ДО застосування урону (наприклад, AP-кулі підвищують Penetration).
        protected virtual DamagePayload Preprocess(in DamagePayload p) => p;

        /// Дозволяє додати ефекти ПІСЛЯ нанесення базової шкоди (підпал, кровотеча тощо).
        protected virtual void Postprocess(GameObject target, in DamagePayload final) { }

        protected void SpawnVfx(in DamagePayload payload)
        {
            if (!hitVFX) return;
            var n = payload.HitNormal == Vector3.zero ? Vector3.up : payload.HitNormal;
            Object.Instantiate(hitVFX, payload.HitPoint, Quaternion.LookRotation(n));
        }
    }

    /// Зручний хелпер для “копії з іншим демеджем”.
    public static class DamagePayloadExtensions
    {
        public static DamagePayload WithDamage(this in DamagePayload p, float dmg)
            => new DamagePayload(
                dmg,
                p.Penetration,
                p.Type,
                p.WeaponTag,
                p.HitPoint,
                p.HitNormal,
                p.Region,
                p.AttackerId,
                p.VictimId
            );
    }
}
