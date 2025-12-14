// ----------------------------
// File: Assets/Combat/Data/ResistMatrix.cs
// ----------------------------
using System;
using System.Linq;
using UnityEngine;


namespace Combat.Core
{
    [CreateAssetMenu(menuName = "Combat/Resist Matrix", fileName = "ResistMatrix")]
    public class ResistMatrix : ScriptableObject
    {
        [Serializable]
        public struct Entry
        {
            public DamageType type;
            [Range(0f, 5f)] public float multiplier; // 1 = neutral, <1 resist, >1 vulnerable
        }


        public Entry[] entries;
        [Range(0f, 5f)] public float defaultMultiplier = 1f;


        public float Get(DamageType t)
        {
            for (int i = 0; i < (entries?.Length ?? 0); i++)
            {
                if (entries[i].type == t) return entries[i].multiplier;
            }
            return defaultMultiplier;
        }
    }
}