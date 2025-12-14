using UnityEngine;

namespace Combat.Core
{
	/// Матеріал поверхні для розрахунку втрат при пробитті.
	public interface IPenetrationMaterial
	{
		/// Множник товщини: ефективна_товщина_мм = geoThickness_m * 1000 * PenetrationMul
		float PenetrationMul { get; }
		/// Скільки урону ЗАЛИШАЄТЬСЯ після проходження 1 метра цього матеріалу (0..1).
		/// Напр., 0.8 => кожен метр зберігає 80% попереднього урону.
		float DamageKeepPerMeter { get; }
	}
}
