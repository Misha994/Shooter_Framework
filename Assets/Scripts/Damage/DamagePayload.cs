// Assets/Combat/Core/DamagePayload.cs
using UnityEngine;

namespace Combat.Core
{
    public partial struct DamagePayload
    {
        public readonly float Damage;
        public readonly float Penetration;
        public readonly DamageType Type;
        public readonly WeaponTag WeaponTag;
        public readonly Vector3 HitPoint;
        public readonly Vector3 HitNormal;
        public readonly BodyRegion Region;
        public readonly int AttackerId;
        public readonly int VictimId;

        // NEW: хто стріляв (може бути null)
        public readonly Transform Attacker;

        public DamagePayload(
            float damage, float penetration, DamageType type, WeaponTag tag,
            Vector3 hitPoint, Vector3 hitNormal, BodyRegion region = BodyRegion.Torso,
            int attackerId = 0, int victimId = 0, Transform attacker = null)
        {
            Damage = damage;
            Penetration = penetration;
            Type = type;
            WeaponTag = tag;
            HitPoint = hitPoint;
            HitNormal = hitNormal;
            Region = region;
            AttackerId = attackerId;
            VictimId = victimId;
            Attacker = attacker;
        }
    }
}
