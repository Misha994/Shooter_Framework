using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class BuildingArmorDebugDrawer : MonoBehaviour
{
	[System.Serializable]
	public struct Item
	{
		public Vector3 point;
		public Vector3 normal;
		public Vector3? attackerPos;
		public BuildingSection section;
		public float angleDeg, penMM, tEffMM, inDmg, outDmg;
		public string result; // "NoPen" / "Partial" / "Full"
		public int chainIndex;
		public Object target;
	}

	[Header("Drawer Settings")]
	[Min(4)] public int capacity = 128;
	[Min(0.1f)] public float keepSeconds = 10f;
	[Min(0.05f)] public float normalRayLen = 0.6f;
	[Min(0.05f)] public float shooterRayLen = 1.2f;
	public bool drawLabels = true;
	public bool drawChains = true;

	private struct TimedItem { public Item item; public float time; }
	private readonly List<TimedItem> _items = new List<TimedItem>(128);

	void OnEnable() { BuildingArmorDebug.OnEvent += Handle; }
	void OnDisable() { BuildingArmorDebug.OnEvent -= Handle; }

	private void Handle(BuildingArmorDebugInfo e)
	{
		if (_items.Count >= Mathf.Max(1, capacity)) _items.RemoveAt(0);
		_items.Add(new TimedItem
		{
			item = new Item
			{
				point = e.point,
				normal = e.normal,
				attackerPos = e.attacker ? e.attacker.position : (Vector3?)null,
				section = e.section,
				angleDeg = e.angleDeg,
				penMM = e.penMM,
				tEffMM = e.tEffMM,
				inDmg = e.dmgIn,
				outDmg = e.dmgOut,
				result = e.result.ToString(),
				chainIndex = e.chainIndex,
				target = e.victim
			},
			time = Time.time
		});
	}

	void LateUpdate()
	{
		float now = Time.time;
		for (int i = _items.Count - 1; i >= 0; i--)
			if (now - _items[i].time > keepSeconds) _items.RemoveAt(i);
	}

	void OnDrawGizmos()
	{
		if (_items.Count == 0) return;
		float now = Application.isPlaying ? Time.time : 0f;

		// Ланцюжки для з’єднання послідовних хітів
		if (drawChains)
		{
			Vector3? prev = null;
			for (int i = 0; i < _items.Count; i++)
			{
				var ti = _items[i];
				if (ti.item.chainIndex == 0) prev = ti.item.point; // «вхід» у першу стіну
				else if (prev.HasValue)
				{
					Gizmos.color = new Color(1f, 1f, 1f, 0.25f);
					Gizmos.DrawLine(prev.Value, ti.item.point);
					prev = ti.item.point;
				}
			}
		}

		for (int i = 0; i < _items.Count; i++)
		{
			var ti = _items[i];
			float age = Application.isPlaying ? (now - ti.time) : 0f;
			float alpha = Mathf.Clamp01(1f - age / Mathf.Max(0.001f, keepSeconds));

			Color baseCol = ti.item.result switch
			{
				"NoPen" => new Color(1f, 0.3f, 0.3f, 0.7f),
				"Partial" => new Color(1f, 0.85f, 0.2f, 0.7f),
				"Full" => new Color(0.35f, 1f, 0.35f, 0.7f),
				_ => new Color(0.3f, 0.6f, 1f, 0.7f),
			};
			Color c = new Color(baseCol.r, baseCol.g, baseCol.b, baseCol.a * alpha);

			// Точка і нормаль
			Gizmos.color = c;
			Gizmos.DrawSphere(ti.item.point, 0.06f);
			Gizmos.DrawRay(ti.item.point, ti.item.normal * normalRayLen);

			// Промінь до стрільця
			if (ti.item.attackerPos.HasValue)
			{
				Vector3 toAtt = (ti.item.attackerPos.Value - ti.item.point);
				if (toAtt.sqrMagnitude > 1e-4f)
					Gizmos.DrawRay(ti.item.point, toAtt.normalized * shooterRayLen);
			}

#if UNITY_EDITOR
			if (drawLabels)
			{
				string label = $"#{ti.item.chainIndex}  {ti.item.section}  ∠{ti.item.angleDeg:0}°\n" +
							   $"pen {ti.item.penMM:0} / tEff {ti.item.tEffMM:0}\n" +
							   $"{ti.item.result}  {ti.item.inDmg:0.##} → {ti.item.outDmg:0.##}";
				UnityEditor.Handles.color = c;
				UnityEditor.Handles.Label(ti.item.point + Vector3.up * 0.05f, label);
			}
#endif
		}
	}
}
