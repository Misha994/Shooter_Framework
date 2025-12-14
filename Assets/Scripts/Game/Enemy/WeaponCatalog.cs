using UnityEngine;

namespace Fogoyote.BFLike.Combat.Weapons.Factory
{
    [CreateAssetMenu(menuName = "Game/WeaponCatalog")]
    public class WeaponCatalog : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public string id;
            public WeaponConfig config;
        }

        [Header("Entries")]
        public Entry[] entries = new Entry[0];

        [Header("Default")]
        [Tooltip("ID дефолтної зброї; якщо порожній — візьмемо перший валідний елемент")]
        public string defaultId;

        public bool TryGet(string id, out WeaponConfig config)
        {
            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    if (entries[i].id == id)
                    {
                        config = entries[i].config;
                        return config != null;
                    }
                }
            }
            config = null;
            return false;
        }

        public WeaponConfig GetDefault()
        {
            if (!string.IsNullOrEmpty(defaultId) && TryGet(defaultId, out var cfg) && cfg != null)
                return cfg;

            for (int i = 0; i < entries.Length; i++)
                if (entries[i].config != null) return entries[i].config;

            return null;
        }
    }
}
