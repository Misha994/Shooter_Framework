using UnityEngine;
using UnityEngine.Animations.Rigging;
using Zenject;

[RequireComponent(typeof(Animator))]
public class CharacterAnimController : MonoBehaviour, ICharacterAnimator, IAnimEventSink
{
    [SerializeField] protected Animator animator;
    [SerializeField] protected Rig upperBodyRig;    // можна не вказувати (Animator layer зам≥сть Rig)
    [SerializeField] protected Rig additiveRig;

    protected WeaponAnimationSet _set;

    protected virtual void Awake()
    {
        if (!animator) animator = GetComponent<Animator>();
    }

    public virtual void SetupWeapon(WeaponAnimationSet set) => _set = set;

    public virtual void SetLocomotion(float speed, float sx, float sy, bool grounded)
    {
        animator.SetFloat(AnimParams.Speed, speed);
        animator.SetFloat(AnimParams.StrafeX, sx);
        animator.SetFloat(AnimParams.StrafeY, sy);
        animator.SetBool(AnimParams.Grounded, grounded);
    }

    public virtual void SetAim(bool isAiming)
    {
        animator.SetBool(AnimParams.IsAiming, isAiming);
        // якщо використовуЇш RigТи зам≥сть layerТ≥в Ч вагу блендить RigWeightBlender
    }

    public virtual void PlayFire()
    {
        animator.ResetTrigger(AnimParams.FireTrig);
        animator.SetTrigger(AnimParams.FireTrig);
    }

    public virtual void PlayReload()
    {
        animator.ResetTrigger(AnimParams.ReloadTrig);
        animator.SetTrigger(AnimParams.ReloadTrig);
    }

    public virtual void CompleteReload() { /* заповнюЇтьс€ у нащадк≥в */ }

    public virtual void PlayDraw() { animator.SetTrigger(AnimParams.DrawTrig); }
    public virtual void PlayHolster() { animator.SetTrigger(AnimParams.HolsterTrig); }

    public virtual void PlayHit(Vector3 worldHitPoint, float power = 1f)
    {
        // легкий поштовх у additiveRig.weight або локальний аддитив-контрол
        if (additiveRig)
            additiveRig.weight = Mathf.Clamp01(additiveRig.weight + 0.25f * power);
    }

    public virtual void PlayDeath()
    {
        animator.CrossFadeInFixedTime("Death", 0.1f, 0); // ≥м'€ стейту у Base Layer
    }

    // ==== Animation events relay ====
    public void OnAnimEvent(string evt)
    {
        if (evt == "ReloadCommit") CompleteReload();
    }
}
