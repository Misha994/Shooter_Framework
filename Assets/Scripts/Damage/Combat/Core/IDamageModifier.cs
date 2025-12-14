// Assets/Combat/Core/DamageContracts.cs
namespace Combat.Core
{
	/// <summary>Плагіноподібний модифікатор урону. Чистий і швидкий.</summary>
	public interface IDamageModifier
	{
		/// <returns>Змінений урон (>=0). Не змінюйте стан; не породжуйте алокацій.</returns>
		float Modify(in DamagePayload payload, float damage);
	}
}
