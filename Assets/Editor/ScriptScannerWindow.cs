using UnityEditor;
using UnityEngine;
using System.IO;

public class ScriptScannerWindow : EditorWindow
{
    private string directoryPath = "";
    private string outputFileName = "AllScripts.txt";

    [MenuItem("Tools/Script Scanner")]
    public static void ShowWindow()
    {
        GetWindow<ScriptScannerWindow>("Script Scanner");
    }

    private void OnGUI()
    {
        GUILayout.Label("Сканування скриптів у директорії", EditorStyles.boldLabel);

        GUILayout.Space(5);
        GUILayout.Label("Шлях до директорії:");
        directoryPath = EditorGUILayout.TextField(directoryPath);

        if (GUILayout.Button("Вибрати директорію"))
        {
            string selected = EditorUtility.OpenFolderPanel("Оберіть директорію", Application.dataPath, "");
            if (!string.IsNullOrEmpty(selected))
            {
                directoryPath = selected;
            }
        }

        GUILayout.Space(5);
        GUILayout.Label("Ім’я вихідного файлу (у цій самій директорії):");
        outputFileName = EditorGUILayout.TextField(outputFileName);

        GUILayout.Space(10);
        if (GUILayout.Button("Сканувати та зберегти"))
        {
            if (Directory.Exists(directoryPath))
            {
                ScanAndSaveScripts(directoryPath, outputFileName);
            }
            else
            {
                EditorUtility.DisplayDialog("Помилка", "Директорія не існує", "Ок");
            }
        }
    }

    private void ScanAndSaveScripts(string dir, string outputName)
    {
        string[] scriptFiles = Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories);
        string outputPath = Path.Combine(dir, outputName);

        using (StreamWriter writer = new StreamWriter(outputPath))
        {
            foreach (string scriptPath in scriptFiles)
            {
                writer.WriteLine("========== FILE: " + Path.GetFileName(scriptPath) + " ==========");
                string content = File.ReadAllText(scriptPath);
                writer.WriteLine(content);
                writer.WriteLine(); // порожній рядок між файлами
            }
        }

        Debug.Log($"Збережено {scriptFiles.Length} скриптів у файл:\n{outputPath}");
        EditorUtility.RevealInFinder(outputPath);
    }
}
