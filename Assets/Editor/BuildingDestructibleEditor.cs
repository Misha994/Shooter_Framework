#if UNITY_EDITOR
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BuildingDestructible))]
public class BuildingDestructibleEditor : Editor
{
    private SerializedProperty sectionsProp;
    private SerializedProperty carriersProp;
    private SerializedProperty requiredCarriersToFailProp;
    private SerializedProperty collapseRatioProp;
    private SerializedProperty ruinsPrefabProp;
    private SerializedProperty ruinsSpawnPointProp;
    private SerializedProperty disableCollidersOnCollapseProp;
    private SerializedProperty disableRenderersOnCollapseProp;
    private SerializedProperty destroyGameObjectOnCollapseProp;
    private SerializedProperty destroyDelayProp;
    private SerializedProperty debugLogsProp;

    private void OnEnable()
    {
        sectionsProp = serializedObject.FindProperty("sections");
        carriersProp = serializedObject.FindProperty("carrierSections");
        requiredCarriersToFailProp = serializedObject.FindProperty("requiredCarriersToFail");
        collapseRatioProp = serializedObject.FindProperty("collapseByDestroyedRatio");
        ruinsPrefabProp = serializedObject.FindProperty("ruinsPrefab");
        ruinsSpawnPointProp = serializedObject.FindProperty("ruinsSpawnPoint");
        disableCollidersOnCollapseProp = serializedObject.FindProperty("disableCollidersOnCollapse");
        disableRenderersOnCollapseProp = serializedObject.FindProperty("disableRenderersOnCollapse");
        destroyGameObjectOnCollapseProp = serializedObject.FindProperty("destroyGameObjectOnCollapse");
        destroyDelayProp = serializedObject.FindProperty("destroyDelay");
        debugLogsProp = serializedObject.FindProperty("debugLogs");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Collect", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Collect Sections (children)"))
                CollectSections();

            if (GUILayout.Button("Clear Sections"))
                sectionsProp.ClearArray();
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.PropertyField(collapseRatioProp, new GUIContent("Collapse by destroyed ratio"));
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Carriers", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(carriersProp, true);
        EditorGUILayout.PropertyField(requiredCarriersToFailProp, new GUIContent("Required carriers to fail"));

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Mark Selected As Carriers")) MarkSelectedAsCarriers();
            if (GUILayout.Button("Clear Carriers")) carriersProp.ClearArray();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Auto-pick Carriers (by area)")) AutoPickCarriers(guessCount: 2);
            if (GUILayout.Button("+1")) AutoPickCarriers(guessCount: GetCarrierCount() + 1);
            if (GUILayout.Button("-1")) AutoPickCarriers(guessCount: Mathf.Max(1, GetCarrierCount() - 1));
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Collapse Result", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(ruinsPrefabProp);
        EditorGUILayout.PropertyField(ruinsSpawnPointProp);

        if (ruinsPrefabProp.objectReferenceValue == null)
        {
            EditorGUILayout.HelpBox("No ruins prefab → on collapse the building will VANISH (see settings below).", MessageType.Info);
            EditorGUILayout.PropertyField(disableCollidersOnCollapseProp);
            EditorGUILayout.PropertyField(disableRenderersOnCollapseProp);
            EditorGUILayout.PropertyField(destroyGameObjectOnCollapseProp);
            if (destroyGameObjectOnCollapseProp.boolValue)
                EditorGUILayout.PropertyField(destroyDelayProp);
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.PropertyField(debugLogsProp);

        serializedObject.ApplyModifiedProperties();
    }

    private void CollectSections()
    {
        var b = (BuildingDestructible)target;
        var all = b.GetComponentsInChildren<DestructibleSection>(true);
        sectionsProp.ClearArray();
        for (int i = 0; i < all.Length; i++)
        {
            sectionsProp.InsertArrayElementAtIndex(i);
            sectionsProp.GetArrayElementAtIndex(i).objectReferenceValue = all[i];
        }
        serializedObject.ApplyModifiedProperties();
        Debug.Log($"[BuildingEditor] Collected {all.Length} sections.");
    }

    private void MarkSelectedAsCarriers()
    {
        var selected = Selection.gameObjects
            .Select(go => go.GetComponentInParent<DestructibleSection>())
            .Where(s => s != null)
            .Distinct()
            .ToList();

        if (selected.Count == 0)
        {
            Debug.LogWarning("[BuildingEditor] Select DestructibleSection objects first.");
            return;
        }

        foreach (var s in selected)
        {
            if (!ContainsRef(carriersProp, s))
            {
                carriersProp.InsertArrayElementAtIndex(carriersProp.arraySize);
                carriersProp.GetArrayElementAtIndex(carriersProp.arraySize - 1).objectReferenceValue = s;
            }
        }
        serializedObject.ApplyModifiedProperties();
        Debug.Log($"[BuildingEditor] Marked {selected.Count} carriers.");
    }

    private void AutoPickCarriers(int guessCount)
    {
        // Беремо зібрані секції, вираховуємо площу по Renderer.bounds або Collider.bounds — і беремо найбільші.
        var b = (BuildingDestructible)target;
        var all = Enumerable.Range(0, sectionsProp.arraySize)
            .Select(i => sectionsProp.GetArrayElementAtIndex(i).objectReferenceValue as DestructibleSection)
            .Where(s => s != null)
            .ToList();

        if (all.Count == 0)
        {
            Debug.LogWarning("[BuildingEditor] No sections collected. Press 'Collect Sections' first.");
            return;
        }

        var scored = all.Select(s =>
        {
            var r = s.GetComponentInChildren<Renderer>();
            Bounds bounds = default;
            if (r != null) bounds = r.bounds;
            else
            {
                var c = s.GetComponentInChildren<Collider>();
                if (c != null) bounds = c.bounds;
                else bounds = new Bounds(s.transform.position, Vector3.one * 1f);
            }
            float area = bounds.size.x * bounds.size.y; // приблизна "площа" стіни
            return (s, area);
        })
        .OrderByDescending(p => p.area)
        .ToList();

        int n = Mathf.Clamp(guessCount, 1, scored.Count);
        carriersProp.ClearArray();
        for (int i = 0; i < n; i++)
        {
            carriersProp.InsertArrayElementAtIndex(i);
            carriersProp.GetArrayElementAtIndex(i).objectReferenceValue = scored[i].s;
        }

        serializedObject.ApplyModifiedProperties();
        Debug.Log($"[BuildingEditor] Auto-picked {n} carrier(s) by largest area.");
    }

    private int GetCarrierCount() => carriersProp.arraySize;

    private static bool ContainsRef(SerializedProperty list, Object o)
    {
        for (int i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == o)
                return true;
        }
        return false;
    }

    // ===== Scene Gizmos =====
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    static void DrawBuildingGizmos(BuildingDestructible b, GizmoType gizmoType)
    {
        if (b == null) return;

        // Спробуємо дістати списки через SerializedObject (щоб працювало і в play, і в edit)
        var so = new SerializedObject(b);
        var sections = so.FindProperty("sections");
        var carriers = so.FindProperty("carrierSections");

        // колір для несучих
        Color carrierCol = new Color(1f, 0.85f, 0.2f, 0.8f);
        Color normalCol = new Color(0.2f, 0.8f, 1f, 0.4f);

        // carrier set для швидкої перевірки
        System.Collections.Generic.HashSet<Object> carrierSet = new System.Collections.Generic.HashSet<Object>();
        for (int i = 0; i < carriers.arraySize; i++)
        {
            var o = carriers.GetArrayElementAtIndex(i).objectReferenceValue;
            if (o != null) carrierSet.Add(o);
        }

        // малюємо секції
        for (int i = 0; i < sections.arraySize; i++)
        {
            var elem = sections.GetArrayElementAtIndex(i).objectReferenceValue as DestructibleSection;
            if (!elem) continue;

            bool isCarrier = carrierSet.Contains(elem);
            var r = elem.GetComponentInChildren<Renderer>();
            Bounds bnd = default;
            if (r != null) bnd = r.bounds;
            else
            {
                var c = elem.GetComponentInChildren<Collider>();
                bnd = c ? c.bounds : new Bounds(elem.transform.position, Vector3.one * 0.5f);
            }

            // рамка
            Gizmos.color = isCarrier ? carrierCol : normalCol;
            Gizmos.DrawWireCube(bnd.center, bnd.size);

            // лейбл
#if UNITY_EDITOR
            var style = new GUIStyle(EditorStyles.boldLabel);
            style.normal.textColor = isCarrier ? new Color(0.95f, 0.7f, 0.1f) : new Color(0.1f, 0.85f, 1f);
            Handles.Label(bnd.center + Vector3.up * (bnd.extents.y + 0.2f),
                isCarrier ? $"Carrier [{i}]\n{elem.name}" : $"Section [{i}]\n{elem.name}",
                style);
#endif
        }
    }
}
#endif
