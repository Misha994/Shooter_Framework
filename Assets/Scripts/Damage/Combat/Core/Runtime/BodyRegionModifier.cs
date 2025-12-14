// Assets/Combat/Runtime/BodyRegionModifier.cs
using UnityEngine;

namespace Combat.Core
{
	[DisallowMultipleComponent]
	public sealed class BodyRegionModifier : MonoBehaviour, IDamageModifier
	{
		[SerializeField] private BodyRegionMatrix matrix;
		public float Modify(in DamagePayload p, float dmg)
			=> matrix ? dmg * Mathf.Max(0f, matrix.Get(p.Region)) : dmg;
	}
}