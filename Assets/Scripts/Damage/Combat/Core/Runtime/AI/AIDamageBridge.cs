// Assets/_Project/Code/Runtime/AI/AIDamageBridge.cs
using Combat.Core;
using UnityEngine;

[RequireComponent(typeof(Fogoyote.BFLike.AI.AITacticalPlanner))]
public sealed class AIDamageBridge : MonoBehaviour
{
	[SerializeField] private DamageReceiver receiver;
	[SerializeField] private Fogoyote.BFLike.AI.AITacticalPlanner planner;

	private void Reset()
	{
		if (!receiver) receiver = GetComponentInParent<DamageReceiver>();
		if (!planner) planner = GetComponent<Fogoyote.BFLike.AI.AITacticalPlanner>();
	}

	private void OnEnable()
	{
		if (receiver && planner) receiver.Damaged += OnDamaged;
	}
	private void OnDisable()
	{
		if (receiver) receiver.Damaged -= OnDamaged;
	}

	private void OnDamaged(GameObject victim, DamagePayload payload, float amount)
	{
		planner.OnDamaged(victim, payload, amount);
	}
}
