// Assets/Editor/PrefabReportExporter.cs
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public static class PrefabReportExporter
{
    [MenuItem("Tools/Export Prefab Report (TXT)", priority = 2000)]
    private static void ExportPrefabReport()
    {
        var selectedGO = Selection.activeObject as GameObject;
        if (selectedGO == null)
        {
            EditorUtility.DisplayDialog("Немає вибраного префабу",
                "Оберіть .prefab у Project або GameObject у сцені.", "OK");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(selectedGO);
        bool isPrefabAsset = !string.IsNullOrEmpty(assetPath)
                             && assetPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase);

        // Куди зберігати
        string suggested = $"{selectedGO.name}_prefab_report_{DateTime.Now:yyyyMMdd-HHmmss}.txt";
        string savePath = EditorUtility.SaveFilePanel("Зберегти звіт", "", suggested, "txt");
        if (string.IsNullOrEmpty(savePath)) return;

        GameObject root = null;
        bool loadedPrefabContents = false;
        try
        {
            if (isPrefabAsset)
            {
                root = PrefabUtility.LoadPrefabContents(assetPath);
                loadedPrefabContents = true;
            }
            else
            {
                root = selectedGO; // сценний об'єкт
            }

            string report = BuildReport(root);
            File.WriteAllText(savePath, report, Encoding.UTF8);

            EditorUtility.RevealInFinder(savePath);
            Debug.Log($"[PrefabReportExporter] Звіт збережено: {savePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[PrefabReportExporter] Помилка: {ex}");
        }
        finally
        {
            if (loadedPrefabContents && root != null)
                PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static string BuildReport(GameObject root)
    {
        var sb = new StringBuilder(64 * 1024);
        sb.AppendLine($"=== PREFAB REPORT ===");
        sb.AppendLine($"Root: {root.name}");
        sb.AppendLine($"Дата: {DateTime.Now}");
        sb.AppendLine(new string('-', 80));

        Traverse(root.transform, 0, sb);

        sb.AppendLine(new string('-', 80));
        sb.AppendLine("Кінець звіту.");
        return sb.ToString();
    }

    private static void Traverse(Transform t, int depth, StringBuilder sb)
    {
        var go = t.gameObject;
        string indent = new string(' ', depth * 2);

        sb.AppendLine($"{indent}- GameObject: \"{go.name}\" " +
                      $"(Active={go.activeSelf}, Tag={go.tag}, Layer={LayerMask.LayerToName(go.layer)})");

        // Компоненти в порядку як в Інспекторі
        var comps = go.GetComponents<Component>();
        for (int i = 0; i < comps.Length; i++)
        {
            var c = comps[i];
            if (c == null)
            {
                sb.AppendLine($"{indent}  * Missing Component (index {i})");
                continue;
            }

            var type = c.GetType();
            string compHeader = $"{indent}  * {type.Name}";
            if (c is Behaviour b) compHeader += $" (enabled={b.enabled})";

            sb.AppendLine(compHeader);

            // Поля: публічні + приватні з [SerializeField]; інші теж покажемо, помітимо як [non-serialized]
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            FieldInfo[] fields;
            try { fields = type.GetFields(flags); }
            catch { fields = Array.Empty<FieldInfo>(); }

            foreach (var f in fields)
            {
                if (f.IsDefined(typeof(NonSerializedAttribute), true)) continue;           // атрибут [NonSerialized]
                if (f.Name.Contains(">k__BackingField")) continue;                        // автосвойства
                bool isSerialized = (f.IsPublic && !f.IsDefined(typeof(HideInInspector), true))
                                    || f.IsDefined(typeof(SerializeField), true);

                object valObj;
                string valueStr;
                try
                {
                    valObj = f.GetValue(c);
                    valueStr = FormatValue(valObj, 0);
                }
                catch (Exception ex)
                {
                    valueStr = $"<exception: {ex.GetType().Name}: {ex.Message}>";
                }

                string tag = isSerialized ? "[serialized]" : "[non-serialized]";
                sb.AppendLine($"{indent}    - {tag} {f.FieldType.Name} {f.Name} = {valueStr}");
            }
        }

        // Діти
        for (int i = 0; i < t.childCount; i++)
        {
            Traverse(t.GetChild(i), depth + 1, sb);
        }
    }

    // Обмежений та безпечний форматер значень
    private static string FormatValue(object obj, int depth, int maxDepth = 2, int maxItems = 25)
    {
        if (obj == null) return "null";
        if (depth >= maxDepth) return $"<{obj.GetType().Name}>";

        switch (obj)
        {
            case string s:
                return $"\"{s}\"";
            case Enum e:
                return e.ToString();
            case bool b:
                return b ? "true" : "false";
            case char ch:
                return $"'{ch}'";
        }

        var type = obj.GetType();
        if (type.IsPrimitive || type == typeof(decimal) || type == typeof(double) || type == typeof(float))
            return Convert.ToString(obj, System.Globalization.CultureInfo.InvariantCulture);

        // UnityEngine.Object — показуємо тип і ім'я
        if (obj is UnityEngine.Object uo)
        {
            string n = string.IsNullOrEmpty(uo.name) ? "(unnamed)" : uo.name;
            return $"{uo.GetType().Name}(\"{n}\")";
        }

        // Типові struct'и Unity мають адекватний ToString()
        if (type.FullName != null && type.FullName.StartsWith("UnityEngine."))
            return obj.ToString();

        // Колекції
        if (obj is IEnumerable enumerable && !(obj is IDictionary))
        {
            var items = new List<string>();
            int count = 0;
            foreach (var it in enumerable)
            {
                if (count >= maxItems) break;
                items.Add(FormatValue(it, depth + 1, maxDepth, maxItems));
                count++;
            }

            string tail = "";
            // Спроба отримати точну довжину якщо можливо
            try
            {
                int total = (obj as ICollection)?.Count ?? -1;
                if (total >= 0 && total > count) tail = $" … (+{total - count} more)";
            }
            catch { /* ignore */ }

            return "[" + string.Join(", ", items) + "]" + tail;
        }

        // Словники
        if (obj is IDictionary dict)
        {
            var items = new List<string>();
            int count = 0;
            foreach (DictionaryEntry kv in dict)
            {
                if (count >= maxItems) break;
                string k = FormatValue(kv.Key, depth + 1, maxDepth, maxItems);
                string v = FormatValue(kv.Value, depth + 1, maxDepth, maxItems);
                items.Add($"{k}: {v}");
                count++;
            }
            return "{" + string.Join(", ", items) + (dict.Count > count ? " …" : "") + "}";
        }

        // За замовчуванням
        try { return obj.ToString(); }
        catch { return $"<{type.Name}>"; }
    }
}
#endif
