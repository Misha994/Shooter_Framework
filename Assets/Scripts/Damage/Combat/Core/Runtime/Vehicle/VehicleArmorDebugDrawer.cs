using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

[DisallowMultipleComponent]
public sealed class VehicleArmorDebugDrawer : MonoBehaviour
{
	[System.Serializable]
	public struct Item
	{
		public Vector3 point;
		public Vector3 normal;
		public Vector3? attackerPos;
		public VehicleSection section;
		public float angleDeg;
		public float penMM;
		public float tEffMM;
		public float inDmg;
		public float outDmg;
		public string result;
		public Object target;
	}

	[Header("Drawer Settings")]
	[Min(1)] public int capacity = 64;
	[Tooltip("Скільки секунд тримати елемент у відладці")]
	[Min(0.1f)] public float keepSeconds = 8f;
	[Min(0.05f)] public float normalRayLen = 0.6f;
	[Min(0.05f)] public float shooterRayLen = 1.2f;
	public bool drawLabels = true;

	private struct TimedItem { public Item item; public float time; }
	private readonly List<TimedItem> _items = new List<TimedItem>(64);

	public void Push(Item item)
	{
		if (_items.Count >= Mathf.Max(1, capacity))
			_items.RemoveAt(0);

		_items.Add(new TimedItem { item = item, time = Time.time });
	}

	private void LateUpdate()
	{
		// чистка протермінованих
		float now = Time.time;
		for (int i = _items.Count - 1; i >= 0; i--)
		{
			if (now - _items[i].time > keepSeconds)
				_items.RemoveAt(i);
		}
	}

	private void OnDrawGizmos()
	{
		if (_items.Count == 0) return;

		float now = Application.isPlaying ? Time.time : 0f;
		for (int i = 0; i < _items.Count; i++)
		{
			var ti = _items[i];
			float age = Application.isPlaying ? (now - ti.time) : 0f;
			float alpha = Mathf.Clamp01(1f - age / Mathf.Max(0.001f, keepSeconds));

			Color baseCol = Color.Lerp(new Color(0.8f, 0.1f, 0.1f, 0.35f), new Color(0.1f, 0.8f, 0.1f, 0.35f), 0.5f);
			switch (ti.item.result)
			{
				case "NoPen": baseCol = new Color(1f, 0.25f, 0.25f, 0.6f); break;
				case "Partial": baseCol = new Color(1f, 0.85f, 0.2f, 0.6f); break;
				case "Full": baseCol = new Color(0.3f, 1f, 0.3f, 0.6f); break;
				default: baseCol = new Color(0.3f, 0.6f, 1f, 0.6f); break;
			}

			Color c = new Color(baseCol.r, baseCol.g, baseCol.b, baseCol.a * alpha);

			// Точка влучання
			Gizmos.color = c;
			Gizmos.DrawSphere(ti.item.point, 0.06f);

			// Нормаль плити
			Gizmos.DrawRay(ti.item.point, ti.item.normal * normalRayLen);

			// Напрямок до стрільця, якщо є
			if (ti.item.attackerPos.HasValue)
			{
				Vector3 toAtt = (ti.item.attackerPos.Value - ti.item.point);
				if (toAtt.sqrMagnitude > 1e-4f)
				{
					toAtt.Normalize();
					Gizmos.DrawRay(ti.item.point, toAtt * shooterRayLen);
				}
			}

#if UNITY_EDITOR
			if (drawLabels)
			{
				string label =
					$"{ti.item.section}  ∠{ti.item.angleDeg:0}°  pen {ti.item.penMM:0} / tEff {ti.item.tEffMM:0}\n" +
					$"{ti.item.result}  dmg {ti.item.inDmg:0.##} → {ti.item.outDmg:0.##}";

				UnityEditor.Handles.color = c;
				UnityEditor.Handles.Label(ti.item.point + Vector3.up * 0.05f, label);
			}
#endif
		}
	}
}
