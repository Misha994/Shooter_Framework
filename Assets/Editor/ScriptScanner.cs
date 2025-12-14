using UnityEngine;
using UnityEditor;
using System.IO;

public class ScriptScanner : MonoBehaviour
{
    [MenuItem("Tools/Scan Scripts to Text File")]
    public static void ScanScripts()
    {
        // Вкажи тут директорію (можна змінити під свої потреби)
        string targetDirectory = Application.dataPath + "/Scripts"; // або будь-яка інша директорія
        string outputPath = Application.dataPath + "/AllScripts.txt";

        if (!Directory.Exists(targetDirectory))
        {
            Debug.LogError("Directory does not exist: " + targetDirectory);
            return;
        }

        string[] scriptFiles = Directory.GetFiles(targetDirectory, "*.cs", SearchOption.AllDirectories);
        using (StreamWriter writer = new StreamWriter(outputPath))
        {
            foreach (string scriptPath in scriptFiles)
            {
                writer.WriteLine("========== FILE: " + Path.GetFileName(scriptPath) + " ==========");
                string content = File.ReadAllText(scriptPath);
                writer.WriteLine(content);
                writer.WriteLine(); // порожній рядок між скриптами
            }
        }

        Debug.Log("All scripts saved to: " + outputPath);
        AssetDatabase.Refresh();
    }
}
