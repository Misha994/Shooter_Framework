using UnityEngine;

[CreateAssetMenu(menuName = "Combat/Vehicle Armor Profile", fileName = "ArmorProfile_Default")]
public sealed class VehicleArmorProfile : ScriptableObject
{
	[System.Serializable]
	public struct Entry
	{
		public VehicleSection section;
		[Tooltip("Товщина в мм RHAe")]
		public float thicknessMM;
		[Tooltip("Матеріальний коефіцієнт (1 = RHAe). Напр., композити 1.1-1.3")]
		public float materialK;
	}

	public Entry[] entries;

	public bool TryGet(VehicleSection s, out Entry e)
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
