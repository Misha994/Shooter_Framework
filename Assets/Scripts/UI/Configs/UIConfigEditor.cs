using Game.UI;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UIConfig))]
public class UIConfigEditor : Editor
{
	SerializedProperty _viewsProp;

	void OnEnable()
	{
		_viewsProp = serializedObject.FindProperty("views");
	}

	public override void OnInspectorGUI()
	{
		serializedObject.Update();
		var config = (UIConfig)target;

		if (GUILayout.Button("Auto-populate Views"))
		{
			Undo.RecordObject(config, "Auto-populate Views");
			var viewIds = (UIViewId[])System.Enum.GetValues(typeof(UIViewId));

			_viewsProp.arraySize = viewIds.Length;
			for (int i = 0; i < viewIds.Length; i++)
			{
				var elem = _viewsProp.GetArrayElementAtIndex(i);
				elem.FindPropertyRelative("ViewId").enumValueIndex = (int)viewIds[i];

				var type = viewIds[i] == UIViewId.GameOverModal ? UIViewType.Modal
						 : viewIds[i] == UIViewId.InfoPopup ? UIViewType.Popup
						 : UIViewType.Window;

				elem.FindPropertyRelative("ViewType").enumValueIndex = (int)type;
				elem.FindPropertyRelative("Prefab").objectReferenceValue = null; // не зачіпаємо
			}

			EditorUtility.SetDirty(config);
		}

		EditorGUILayout.Space();
		DrawDefaultInspector();
		serializedObject.ApplyModifiedProperties();
	}
}
