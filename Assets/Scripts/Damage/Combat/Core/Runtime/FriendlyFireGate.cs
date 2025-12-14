// Assets/Combat/Runtime/FriendlyFireGate.cs
using Combat.Core;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class FriendlyFireGate : MonoBehaviour, IDamageModifier
{
	[SerializeField] private FactionRules rules;
	[SerializeField] private MonoBehaviour allegianceProvider; // опціонально
	private IAllegiance _self;

	private void Awake()
	{
		_self = allegianceProvider as IAllegiance ?? GetComponentInParent<IAllegiance>();
	}

	public float Modify(in Combat.Core.DamagePayload p, float dmg)
	{
		if (rules == null || _self == null) return dmg;
		var atk = p.Attacker ? p.Attacker.GetComponentInParent<IAllegiance>() : null;
		if (atk == null) return dmg;

		bool hostile = rules.AreHostile(_self.Faction, atk.Faction);
		if (!rules.friendlyFireEnabled && !hostile) return 0f;
		return dmg;
	}
}
