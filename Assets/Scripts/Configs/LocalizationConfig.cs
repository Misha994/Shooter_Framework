using UnityEngine;

[CreateAssetMenu(fileName = "LocalizationConfig", menuName = "Configs/LocalizationConfig")]
public class LocalizationConfig : ScriptableObject
{
    [System.Serializable]
    public struct TextEntry
    {
        public string Key;
        public string English;
        public string Ukrainian;
    }

    [SerializeField] private TextEntry[] texts;

    public string GetText(string key, string language = "English")
    {
        foreach (var entry in texts)
        {
            if (entry.Key == key)
            {
                return language == "Ukrainian" ? entry.Ukrainian : entry.English;
            }
        }
        Debug.LogWarning($"Localization key {key} not found.");
        return key;
    }
}