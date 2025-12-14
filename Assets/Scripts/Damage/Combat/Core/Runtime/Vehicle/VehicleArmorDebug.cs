// Assets/Combat/Debug/VehicleArmorDebug.cs
using Combat.Core;
using System;
using UnityEngine;

public enum ArmorPenResult { NoPen, Partial, Full }

public struct VehicleArmorDebugInfo
{
	public GameObject victim;
	public Vector3 hitPoint;
	public Vector3 hitNormal;
	public Transform attacker;
	public VehicleSection section;
	public float angleDeg;
	public float penMM;
	public float tEffMM;
	public float dmgIn;
	public float dmgOut;
	public ArmorPenResult result;
	public float time;
	public WeaponTag weapon;
	public DamageType type;
}

public static class VehicleArmorDebug
{
	public static bool Enabled = true; // перемикач
	public static event Action<VehicleArmorDebugInfo> OnEvent;

	public static void Emit(VehicleArmorDebugInfo info)
	{
		if (!Enabled) return;
		OnEvent?.Invoke(info);
#if UNITY_EDITOR
		// Короткий лог у консоль
		Debug.Log(
			$"[ArmorDBG] {info.victim?.name} " +
			$"sec={info.section} angle={info.angleDeg:0.#}° pen={info.penMM:0.#}mm tEff={info.tEffMM:0.#}mm " +
			$"→ {info.result}  dmg {info.dmgIn:0.#} → {info.dmgOut:0.#}");
#endif
	}
}
