// Assets/Destruction/Runtime/SectionDamageModifier.cs
using Combat.Core;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class SectionDamageModifier : MonoBehaviour, IDamageModifier
{
	[SerializeField] private SectionDestructionProfile profile;

	public float Modify(in DamagePayload p, float dmg)
	{
		if (!profile) return dmg;
		if (dmg < profile.minDamageThreshold) return 0f;

		dmg *= profile.GetTypeMultiplier(p.Type);
		dmg *= profile.GetWeaponTagMultiplier(p.WeaponTag);
		return Mathf.Max(0f, dmg);
	}
}
