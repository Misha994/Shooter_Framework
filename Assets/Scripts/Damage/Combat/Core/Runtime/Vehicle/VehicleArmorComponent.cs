// Assets/Scripts/Damage/Combat/Core/Runtime/Vehicle/VehicleArmorComponent.cs
using UnityEngine;
using Combat.Core;
using Debug = UnityEngine.Debug;

[DisallowMultipleComponent]
public sealed class VehicleArmorComponent : MonoBehaviour, IDamageModifier
{
	[Header("Profile")]
	public VehicleArmorProfile profile;

	[Header("Fallbacks / Heuristics")]
	public VehicleSection defaultSection = VehicleSection.HullFront;
	[Range(0, 89)] public float defaultImpactAngleDeg = 0f;
	[Min(0f)] public float hitboxProbeRadius = 0.15f;

	[Header("Damage Mapping")]
	[Range(0f, 1f)] public float fullPenDamageFactor = 1.0f;
	[Range(0f, 0.5f)] public float noPenDamageFactor = 0.05f;
	[Range(0f, 1f)] public float partialPenBlend = 0.5f;

	private static float EffectiveThicknessMM(float tMM, float materialK, float impactAngleDeg)
	{
		float cos = Mathf.Cos(Mathf.Deg2Rad * Mathf.Clamp(impactAngleDeg, 0f, 89f));
		float pathMul = 1f / Mathf.Max(0.001f, cos);
		return tMM * Mathf.Max(0.1f, materialK) * pathMul;
	}

	private float GuessImpactAngleDeg(in DamagePayload p)
	{
		return ImpactAngleUtil.GuessImpactAngleDeg(
			in p, p.HitPoint, p.HitNormal,
			defaultImpactAngleDeg, hitboxProbeRadius, 0.5f);
	}

	private VehicleSection ResolveSection(in DamagePayload p)
	{
		if (hitboxProbeRadius > 0f)
		{
			var cols = Physics.OverlapSphere(p.HitPoint, hitboxProbeRadius, ~0, QueryTriggerInteraction.Ignore);
			for (int i = 0; i < cols.Length; i++)
				if (cols[i].TryGetComponent<VehicleHitbox>(out var hb))
					return hb.section;
		}

		// евристика за напрямком
		Transform root = transform;
		Vector3 toShooter = p.Attacker ? (p.Attacker.position - root.position) : (p.HitPoint - root.position);

		if (toShooter.sqrMagnitude > 1e-6f)
		{
			toShooter.Normalize();
			Vector3 f = root.forward;
			Vector3 u = root.up;

			float frontDot = Vector3.Dot(f, toShooter);
			float rearDot = Vector3.Dot(-f, toShooter);
			float upDot = Mathf.Abs(Vector3.Dot(u, (p.HitPoint - root.position).normalized));

			if (upDot > 0.85f) return VehicleSection.HullTop;
			if (frontDot > 0.6f) return VehicleSection.HullFront;
			if (rearDot > 0.6f) return VehicleSection.HullRear;
			return VehicleSection.HullSide;
		}
		return defaultSection;
	}

	public float Modify(in DamagePayload p, float dmg)
	{
		if (profile == null) return dmg;

		// вибухи
		if (p.Type == DamageType.Explosive)
		{
			var s = ResolveSection(in p);
			if (profile.TryGet(s, out var e0))
			{
				float tEff0 = EffectiveThicknessMM(e0.thicknessMM, e0.materialK, GuessImpactAngleDeg(in p));
				float atten = Mathf.Clamp01(1f - tEff0 / 800f);
				float outDmg = dmg * atten;
				Debug.Log($"[ArmorDBG] {name} EXP sec={s} ∠={GuessImpactAngleDeg(in p):0}° tEff={tEff0:0}mm  dmg {dmg} → {outDmg:0.##}", this);
				return outDmg;
			}
			return dmg * 0.8f;
		}

		// кінетика/кумулятив
		float penMM = Mathf.Max(0f, p.Penetration);
		var sec = ResolveSection(in p);
		if (!profile.TryGet(sec, out var e)) return dmg;

		float angle = GuessImpactAngleDeg(in p);
		float tEff = EffectiveThicknessMM(Mathf.Max(1f, e.thicknessMM), Mathf.Max(0.1f, e.materialK), angle);

		float outDamage;
		if (penMM <= 0.0001f)
		{
			outDamage = dmg * noPenDamageFactor;
			Debug.Log($"[ArmorDBG] {name} sec={sec} ∠={angle:0}° pen={penMM:0}mm tEff={tEff:0}mm  NoPen  dmg {dmg} → {outDamage:0.##}  type={p.Type}/{p.WeaponTag}", this);
			return outDamage;
		}
		if (penMM >= tEff * 1.05f)
		{
			outDamage = dmg * fullPenDamageFactor;
			Debug.Log($"[ArmorDBG] {name} sec={sec} ∠={angle:0}° pen={penMM:0}mm tEff={tEff:0}mm  Full   dmg {dmg} → {outDamage:0.##}  type={p.Type}/{p.WeaponTag}", this);
			return outDamage;
		}
		if (penMM <= tEff * 0.9f)
		{
			outDamage = dmg * noPenDamageFactor;
			Debug.Log($"[ArmorDBG] {name} sec={sec} ∠={angle:0}° pen={penMM:0}mm tEff={tEff:0}mm  NoPen  dmg {dmg} → {outDamage:0.##}  type={p.Type}/{p.WeaponTag}", this);
			return outDamage;
		}

		float t = Mathf.InverseLerp(tEff * 0.9f, tEff * 1.05f, penMM);
		float k = Mathf.Lerp(noPenDamageFactor, fullPenDamageFactor, partialPenBlend * t);
		outDamage = dmg * k;

		Debug.Log($"[ArmorDBG] {name} sec={sec} ∠={angle:0}° pen={penMM:0}mm tEff={tEff:0}mm  Partial t={t:0.00}  dmg {dmg} → {outDamage:0.##}  type={p.Type}/{p.WeaponTag}", this);
		return outDamage;
	}

#if UNITY_EDITOR
	private void OnDrawGizmosSelected()
	{
		Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
		Gizmos.DrawWireSphere(transform.position, 0.25f);
	}
#endif
}
