// Assets/Combat/Runtime/Building/BuildingHitbox.cs
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BuildingHitbox : MonoBehaviour
{
	public BuildingSection section = BuildingSection.Wall;

#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		Color col = section switch
		{
			BuildingSection.Wall => new Color(0.8f, 0.8f, 0.2f, 0.25f),
			BuildingSection.Pillar => new Color(0.9f, 0.6f, 0.2f, 0.25f),
			BuildingSection.Window => new Color(0.2f, 0.8f, 1f, 0.25f),
			BuildingSection.Door => new Color(1f, 0.4f, 0.8f, 0.25f),
			BuildingSection.Roof => new Color(0.6f, 1f, 0.6f, 0.25f),
			BuildingSection.Floor => new Color(0.6f, 0.6f, 1f, 0.25f),
			_ => new Color(1f, 1f, 1f, 0.2f),
		};

		Gizmos.color = col;

		if (!TryGetComponent<Collider>(out var c)) return;

		var m = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
		Gizmos.matrix = m;

		if (c is BoxCollider bc)
			Gizmos.DrawWireCube(bc.center, bc.size);
		else if (c is SphereCollider sc)
			Gizmos.DrawWireSphere(sc.center, sc.radius);
		else if (c is CapsuleCollider cc)
		{
			float half = Mathf.Max(0f, cc.height * 0.5f - cc.radius);
			Vector3 axis = cc.direction switch { 0 => Vector3.right, 1 => Vector3.up, _ => Vector3.forward };
			Gizmos.DrawWireSphere(cc.center + axis * half, cc.radius);
			Gizmos.DrawWireSphere(cc.center - axis * half, cc.radius);
		}

		Gizmos.matrix = Matrix4x4.identity;
		Gizmos.DrawWireSphere(transform.position, 0.075f);
	}
#endif
}
