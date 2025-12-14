// Assets/Editor/CodeExportTool.cs
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using IOCompression = System.IO.Compression; // <-- alias, щоб уникнути конфлікту імен

#if !UNITY_WEBGL
using System.IO.Compression; // ZipFile
#endif

[Serializable]
public class CodeFileEntry
{
    public string path;        // "Assets/.../Foo.cs" або "Packages/.../Bar.cs"
    public int lines;
    public string sha256;
    public string content;
}

public static class CodeExportTool
{
    private static string TimeStamp => DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
    private static string AssetsAbs => Application.dataPath;
    private static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

    [MenuItem("Tools/Code Export/Export ZIP of .cs")]
    public static void ExportZip()
    {
#if UNITY_WEBGL
        EditorUtility.DisplayDialog("Unsupported", "ZIP недоступний у цільовій платформі WebGL.", "OK");
        return;
#else
        try
        {
            string tempDir = Path.Combine(ProjectRoot, $"_CodeExport_{TimeStamp}");
            Directory.CreateDirectory(tempDir);

            foreach (var rel in GetCsPaths())
            {
                // Копіюємо ТІЛЬКИ ті, що реально в Assets/, бо Packages/ може бути віртуальним.
                if (!rel.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)) continue;

                string abs = ToAbsoluteAssets(rel);
                if (!File.Exists(abs)) continue;

                string target = Path.Combine(tempDir, rel);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.Copy(abs, target, true);
            }

            string zipPath = Path.Combine(ProjectRoot, $"CodeScripts_{TimeStamp}.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);
            ZipFile.CreateFromDirectory(tempDir, zipPath, IOCompression.CompressionLevel.Optimal, includeBaseDirectory: false);
            Directory.Delete(tempDir, recursive: true);

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Code Export", $"ZIP збережено:\n{zipPath}", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CodeExport] ZIP error: {ex}");
            EditorUtility.DisplayDialog("Code Export", "Помилка при створенні ZIP. Див. Console.", "OK");
        }
#endif
    }

    [MenuItem("Tools/Code Export/Export single TXT")]
    public static void ExportSingleTxt()
    {
        try
        {
            string outDir = Path.Combine(AssetsAbs, "CodeSnapshot");
            Directory.CreateDirectory(outDir);

            string outPath = Path.Combine(outDir, $"AllScripts_{TimeStamp}.txt");
            using (var sw = new StreamWriter(outPath, false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                foreach (var rel in GetCsPaths())
                {
                    if (!TryReadScriptText(rel, out var txt)) continue;

                    sw.WriteLine(new string('=', 80));
                    sw.WriteLine($"// FILE: {rel}");
                    sw.WriteLine(new string('=', 80));
                    sw.WriteLine(txt);
                    sw.WriteLine();
                }
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Code Export", $"TXT збережено:\n{outPath}", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CodeExport] TXT error: {ex}");
            EditorUtility.DisplayDialog("Code Export", "Помилка при створенні TXT. Див. Console.", "OK");
        }
    }

    [MenuItem("Tools/Code Export/Export JSON (paths+content)")]
    public static void ExportJson()
    {
        try
        {
            string outDir = Path.Combine(AssetsAbs, "CodeSnapshot");
            Directory.CreateDirectory(outDir);

            string outPath = Path.Combine(outDir, $"CodeSnapshot_{TimeStamp}.json");
            var list = new List<CodeFileEntry>();

            foreach (var rel in GetCsPaths())
            {
                if (!TryReadScriptText(rel, out var txt)) continue;

                list.Add(new CodeFileEntry
                {
                    path = rel,
                    lines = CountLinesFast(txt),
                    sha256 = Sha256(txt),
                    content = txt
                });
            }

            string json = JsonUtility.ToJson(new Wrapper<List<CodeFileEntry>> { items = list }, prettyPrint: true);
            File.WriteAllText(outPath, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Code Export", $"JSON збережено:\n{outPath}", "OK");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CodeExport] JSON error: {ex}");
            EditorUtility.DisplayDialog("Code Export", "Помилка при створенні JSON. Див. Console.", "OK");
        }
    }

    // === helpers ===

    private static IEnumerable<string> GetCsPaths()
    {
        // Усі MonoScript-и (і з Assets/, і з Packages/)
        var guids = AssetDatabase.FindAssets("t:MonoScript");
        return guids.Select(AssetDatabase.GUIDToAssetPath)
                    .Where(p => p.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
                    .Distinct()
                    .OrderBy(p => p, StringComparer.Ordinal);
    }

    private static bool TryReadScriptText(string assetPath, out string text)
    {
        // 1) Основний шлях — через MonoScript (працює і для Packages/)
        var mono = AssetDatabase.LoadAssetAtPath<MonoScript>(assetPath);
        if (mono != null)
        {
            text = mono.text; // Unity повертає джерело
            return text != null;
        }

        // 2) Фолбек: якщо це Assets/, можемо спробувати з диска
        if (assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
        {
            string abs = ToAbsoluteAssets(assetPath);
            if (File.Exists(abs))
            {
                text = File.ReadAllText(abs, Encoding.UTF8);
                return true;
            }
        }

        text = null;
        Debug.LogWarning($"[CodeExport] Не вдалося прочитати: {assetPath}");
        return false;
    }

    private static string ToAbsoluteAssets(string assetsRelative)
    {
        // Тільки для шляхів, що починаються з "Assets/"
        string rel = assetsRelative.Replace("Assets", "").TrimStart('/', '\\');
        return Path.Combine(AssetsAbs, rel).Replace("\\", "/");
    }

    private static int CountLinesFast(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        int count = 1;
        for (int i = 0; i < s.Length; i++) if (s[i] == '\n') count++;
        return count;
    }

    private static string Sha256(string s)
    {
        using (var sha = SHA256.Create())
        {
            var bytes = Encoding.UTF8.GetBytes(s);
            var hash = sha.ComputeHash(bytes);
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }

    [Serializable] private class Wrapper<T> { public T items; }
}
#endif
