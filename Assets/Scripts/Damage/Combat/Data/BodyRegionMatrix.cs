// Assets/Combat/Data/BodyRegionMatrix.cs
using UnityEngine;

namespace Combat.Core
{
	[CreateAssetMenu(menuName = "Combat/BodyRegion Matrix", fileName = "BodyRegionMatrix")]
	public class BodyRegionMatrix : ScriptableObject
	{
		[System.Serializable]
		public struct Entry { public BodyRegion region; public float mul; }

		public Entry[] entries;
		public float defaultMul = 1f;

		public float Get(BodyRegion r)
		{
			for (int i = 0; i < (entries?.Length ?? 0); i++)
				if (entries[i].region == r) return entries[i].mul;
			return defaultMul;
		}
	}
}