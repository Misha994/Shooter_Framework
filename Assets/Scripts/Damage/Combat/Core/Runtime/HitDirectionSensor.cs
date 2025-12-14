// Assets/Combat/Core/Runtime/HitDirectionSensor.cs
using UnityEngine;

namespace Combat.Core
{
	[DisallowMultipleComponent]
	public sealed class HitDirectionSensor : MonoBehaviour
	{
		[Header("Last Hit (read-only)")]
		public Vector3 lastRayOrigin;
		public Vector3 lastIncomingDir; // нормалізований напрямок "що влітає у колайдер"
		public float lastTime;          // Time.time останнього хіта
		public Collider sourceCollider; // на якому колайдері висить сенсор

		[Header("Debug")]
		[SerializeField] private float gizmoRayLen = 0.8f;
		[SerializeField] private float gizmoKeepSeconds = 0.5f;
		[SerializeField] private Color gizmoColor = new Color(1, 0.4f, 0.1f, 0.9f);

		void Awake()
		{
			if (!sourceCollider) sourceCollider = GetComponent<Collider>();
		}

		public void ReportRayHit(Vector3 origin, Vector3 incomingDir)
		{
			lastRayOrigin = origin;
			lastIncomingDir = incomingDir.sqrMagnitude > 1e-8f ? incomingDir.normalized : Vector3.forward;
			lastTime = Time.time;
		}

		public bool TryGetRecent(out Vector3 dir, float maxAge)
		{
			if (Time.time - lastTime <= maxAge && lastIncomingDir.sqrMagnitude > 0.5f)
			{
				dir = lastIncomingDir; return true;
			}
			dir = default; return false;
		}

		// ---- Статичні хелпери ----
		public static void TryReportRayHit(Collider c, Vector3 origin, Vector3 incomingDir)
		{
			if (!c) return;
			var sensor = c.GetComponent<HitDirectionSensor>();
			if (!sensor) sensor = c.gameObject.AddComponent<HitDirectionSensor>();
			sensor.sourceCollider = c;
			sensor.ReportRayHit(origin, incomingDir);
		}

		public static HitDirectionSensor FindBestAround(Vector3 point, float radius, float maxAge = 0.35f)
		{
			var cols = Physics.OverlapSphere(point, Mathf.Max(0.01f, radius), ~0, QueryTriggerInteraction.Collide);
			HitDirectionSensor best = null; float bestTime = -999f;
			for (int i = 0; i < cols.Length; i++)
			{
				var s = cols[i].GetComponent<HitDirectionSensor>();
				if (s == null) continue;
				if (Time.time - s.lastTime > maxAge) continue;
				if (s.lastTime > bestTime) { bestTime = s.lastTime; best = s; }
			}
			return best;
		}

#if UNITY_EDITOR
		void OnDrawGizmos()
		{
			if (Application.isPlaying && Time.time - lastTime <= gizmoKeepSeconds)
			{
				Gizmos.color = gizmoColor;
				Gizmos.DrawLine(lastRayOrigin, lastRayOrigin + lastIncomingDir * gizmoRayLen);
				Gizmos.DrawSphere(lastRayOrigin, 0.02f);
			}
		}
#endif
	}
}
