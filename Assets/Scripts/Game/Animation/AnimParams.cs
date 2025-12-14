using UnityEngine;

public static class AnimParams
{
    public static readonly int Speed = Animator.StringToHash("Speed");
    public static readonly int StrafeX = Animator.StringToHash("StrafeX");
    public static readonly int StrafeY = Animator.StringToHash("StrafeY");
    public static readonly int Grounded = Animator.StringToHash("Grounded");
    public static readonly int IsAiming = Animator.StringToHash("IsAiming");

    public static readonly int FireTrig = Animator.StringToHash("Fire");
    public static readonly int ReloadTrig = Animator.StringToHash("Reload");
    public static readonly int DrawTrig = Animator.StringToHash("Draw");
    public static readonly int HolsterTrig = Animator.StringToHash("Holster");
}
