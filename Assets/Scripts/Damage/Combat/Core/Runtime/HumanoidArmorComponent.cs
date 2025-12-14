// Assets/Combat/Runtime/ArmorComponent.cs (додано реалізацію IDamageModifier)
using UnityEngine;

namespace Combat.Core
{
	[DisallowMultipleComponent]
	public sealed class HumanoidArmorComponent : MonoBehaviour, IDamageModifier
	{
		[Header("Base Mitigation [0..1]")]
		[Range(0f, 1f)] public float baseMitigation = 0.3f;

		[Header("Region multipliers")]
		public float headMul = 1.2f;
		public float torsoMul = 1.0f;
		public float limbsMul = 0.6f;

		public float GetMitigation(DamageType type, BodyRegion region)
		{
			float m = Mathf.Clamp01(baseMitigation);
			switch (region)
			{
				case BodyRegion.Head: m *= headMul; break;
				case BodyRegion.Torso: m *= torsoMul; break;
				case BodyRegion.Arm:
				case BodyRegion.Leg: m *= limbsMul; break;
				default: m *= torsoMul; break;
			}
			return Mathf.Clamp01(m);
		}

		// IDamageModifier
		public float Modify(in DamagePayload p, float dmg)
		{
			float mit = GetMitigation(p.Type, p.Region);
			float pen = Mathf.Clamp01(p.Penetration);
			return dmg * (1f - mit * (1f - pen));
		}
	}
}
