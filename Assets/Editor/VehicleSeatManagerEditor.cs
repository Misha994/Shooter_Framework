#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VehicleSeatManager))]
public class VehicleSeatManagerEditor : Editor
{
	//public override void OnInspectorGUI()
	//{
	//	// Малюємо стандартний інспектор
	//	DrawDefaultInspector();

	//	EditorGUILayout.Space();
	//	EditorGUILayout.LabelField("Debug / Tools", EditorStyles.boldLabel);

	//	if (GUILayout.Button("Validate seats & exit points"))
	//	{
	//		ValidateSeats((VehicleSeatManager)target);
	//	}
	//}

	//private void ValidateSeats(VehicleSeatManager mgr)
	//{
	//	if (mgr == null)
	//		return;

	//	var seats = mgr.Seats;
	//	if (seats == null || seats.Length == 0)
	//	{
	//		Debug.LogWarning($"[SeatManager VALIDATE] {mgr.name}: seats масив порожній. Використай OnValidate або зафіль усі сидіння вручну.");
	//		return;
	//	}

	//	Transform root = mgr.transform.root;

	//	int okCount = 0;
	//	int warnCount = 0;

	//	for (int i = 0; i < seats.Length; i++)
	//	{
	//		var seat = seats[i];

	//		if (seat == null)
	//		{
	//			Debug.LogWarning($"[SeatManager VALIDATE] {mgr.name}: seats[{i}] == null");
	//			warnCount++;
	//			continue;
	//		}

	//		string seatPath = GetHierarchyPath(seat.transform);

	//		// Перевірка localExitPoint
	//		if (seat.localExitPoint == null)
	//		{
	//			Debug.LogWarning(
	//				$"[SeatManager VALIDATE] {mgr.name}: Seat '{seat.name}' ({seatPath}) не має localExitPoint. " +
	//				"Гравець буде виходити в defaultExitPoint / mount / transform."
	//			);
	//			warnCount++;
	//		}
	//		else
	//		{
	//			// Перевіряємо, чи exit лежить під тим же коренем (танком/префабом)
	//			if (!seat.localExitPoint.IsChildOf(root))
	//			{
	//				Debug.LogWarning(
	//					$"[SeatManager VALIDATE] {mgr.name}: Seat '{seat.name}' має localExitPoint '{seat.localExitPoint.name}', " +
	//					$"але він знаходиться поза коренем '{root.name}'.\n" +
	//					$"Seat path: {seatPath}\n" +
	//					$"Exit path: {GetHierarchyPath(seat.localExitPoint)}\n" +
	//					"Це може призводити до дивних координат виходу (телепорти в (0,0,0) та ін.)."
	//				);
	//				warnCount++;
	//			}
	//			else
	//			{
	//				okCount++;
	//			}
	//		}
	//	}

	//	Debug.Log($"[SeatManager VALIDATE] {mgr.name}: OK exits = {okCount}, warnings = {warnCount}");
	//}

	//private static string GetHierarchyPath(Transform t)
	//{
	//	if (t == null) return "<null>";
	//	string path = t.name;
	//	while (t.parent != null)
	//	{
	//		t = t.parent;
	//		path = t.name + "/" + path;
	//	}
	//	return path;
	//}
}
#endif
