// Вже є ArmorPenResult у VehicleArmorDebug.cs — використовуємо його, не дублюємо.

using System;
using UnityEngine;
using Combat.Core;

public struct BuildingArmorDebugInfo
{
	public GameObject victim;
	public Vector3 point;
	public Vector3 normal;
	public Transform attacker;
	public BuildingSection section;
	public float angleDeg;
	public float penMM;
	public float tEffMM;
	public float dmgIn;
	public float dmgOut;
	public ArmorPenResult result; // NoPen / Partial / Full
	public int chainIndex;        // 0 = вхід у першу стіну, 1..N = хіти позаду
	public WeaponTag weapon;
	public DamageType type;
	public float time;
}

public static class BuildingArmorDebug
{
	public static bool Enabled = true;
	public static event Action<BuildingArmorDebugInfo> OnEvent;

	public static void Emit(BuildingArmorDebugInfo info)
	{
		if (!Enabled) return;
		OnEvent?.Invoke(info);
#if UNITY_EDITOR
		Debug.Log(
			$"[BArmorDBG] {info.victim?.name} sec={info.section} " +
			$"∠={info.angleDeg:0.#}° pen={info.penMM:0.#}mm tEff={info.tEffMM:0.#}mm " +
			$"→ {info.result} dmg {info.dmgIn:0.#} → {info.dmgOut:0.#} chain={info.chainIndex}");
#endif
		// Мінімальна візуалізація навіть у білді
		Debug.DrawRay(info.point, info.normal * 0.6f, Color.cyan, 4f);
		if (info.attacker) Debug.DrawLine(info.point, info.attacker.position, Color.white, 4f);
	}
}
