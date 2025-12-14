using System;
using UnityEngine;

namespace Combat.Core
{
	public interface IDamageReceiver
	{
		void TakeDamage(in DamagePayload payload);
	}
}
