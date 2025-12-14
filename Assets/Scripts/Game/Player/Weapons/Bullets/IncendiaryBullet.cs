// Assets/Combat/Bullets/IncendiaryBullet.cs
using UnityEngine;

namespace Combat.Core
{
    [CreateAssetMenu(menuName = "Bullets/Incendiary")]
    public class IncendiaryBullet : BulletType
    {
        [Header("Burn Effect")]
        [Min(0f)] public float burnDps = 5f;
        [Min(0f)] public float burnDuration = 3f;

        protected override void Postprocess(GameObject target, in DamagePayload final)
        {
            if (target && target.GetComponentInParent<IBurnable>() is { } burnable)
                burnable.ApplyBurn(burnDps, burnDuration);
        }
    }
}
