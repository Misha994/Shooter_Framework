#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class DestructibleSectionGizmos
{
    static Texture2D _dot;

    static DestructibleSectionGizmos()
    {
        _dot = EditorGUIUtility.IconContent("d_winbtn_mac_close").image as Texture2D;
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.Pickable)]
    public static void DrawSection(DestructibleSection s, GizmoType type)
    {
        if (s == null) return;

        // рамка секції
        var r = s.GetComponentInChildren<Renderer>();
        Bounds bnd = default;
        if (r != null) bnd = r.bounds;
        else
        {
            var c = s.GetComponentInChildren<Collider>();
            bnd = c ? c.bounds : new Bounds(s.transform.position, Vector3.one * 0.5f);
        }

        Gizmos.color = new Color(0.3f, 1f, 0.6f, 0.35f);
        Gizmos.DrawWireCube(bnd.center, bnd.size);

#if UNITY_EDITOR
        // Підпис HP в PlayMode
        if (Application.isPlaying)
        {
            var style = new GUIStyle(EditorStyles.label);
            style.normal.textColor = Color.white;
        }
#endif
    }
}
#endif
