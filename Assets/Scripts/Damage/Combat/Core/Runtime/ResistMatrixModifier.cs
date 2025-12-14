// Assets/Combat/Runtime/ResistMatrixModifier.cs
using UnityEngine;

namespace Combat.Core
{
	[DisallowMultipleComponent]
	public sealed class ResistMatrixModifier : MonoBehaviour, IDamageModifier
	{
		[SerializeField] private ResistMatrix matrix;
		public float Modify(in DamagePayload p, float dmg)
			=> matrix ? dmg * Mathf.Max(0f, matrix.Get(p.Type)) : dmg;
	}
}
