// Assets/Combat/Runtime/Building/BuildingArmorProfile.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Building Armor Profile", fileName = "BuildingArmorProfile_Default")]
public sealed class BuildingArmorProfile : ScriptableObject
{
	[System.Serializable]
	public struct Entry
	{
		public BuildingSection section;
		[Tooltip("Еквівалент товщини у мм RHAe")]
		public float thicknessMM;
		[Tooltip("Матеріальний коефіцієнт (1 = RHAe). Напр., бетон 1.1–1.3")]
		public float materialK;
	}

	public Entry[] entries;

	public bool TryGet(BuildingSection s, out Entry e)
	{
		if (entries != null)
		{
			for (int i = 0; i < entries.Length; i++)
			{
				if (entries[i].section == s) { e = entries[i]; return true; }
			}
		}
		e = default;
		return false;
	}
}
