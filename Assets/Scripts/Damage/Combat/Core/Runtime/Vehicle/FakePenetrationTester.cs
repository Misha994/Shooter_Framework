// Assets/Combat/Debug/FakePenetrationTester.cs
using UnityEngine;
using Combat.Core;

public class FakePenetrationTester : MonoBehaviour
{
	public float damage = 100f;
	public float penetrationMM = 200f;
	public DamageType type = DamageType.Kinetic;
	public WeaponTag weapon = WeaponTag.Cannon;
	public Transform attacker; // можна поставити FirePoint

	void Update()
	{
		if (Input.GetKeyDown(KeyCode.P))
		{
			if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out var hit, 1000f))
			{
				var dr = hit.collider.GetComponentInParent<DamageReceiver>();
				if (!dr) return;

				var payload = new DamagePayload(
					damage, penetrationMM, type, weapon,
					hit.point, hit.normal, BodyRegion.Torso,
					0, 0, attacker);

				dr.TakeDamage(in payload);
			}
		}
	}
}
