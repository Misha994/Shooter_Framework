using UnityEngine;

[CreateAssetMenu(fileName = "WeaponAnimationSet", menuName = "Game/Weapons/Weapon Animation Set")]
public class WeaponAnimationSet : ScriptableObject
{
    [Header("Locomotion (placeholders: PL_* )")]
    public AnimationClip idle;
    public AnimationClip walkFwd;
    public AnimationClip walkBack;
    public AnimationClip strafeL;
    public AnimationClip strafeR;
    public AnimationClip runFwd;
    public AnimationClip runBack;
    public AnimationClip runStrafeL;
    public AnimationClip runStrafeR;

    [Header("Weapon Actions")]
    public AnimationClip reload;        // placeholder: PL_RELOAD
    public AnimationClip adsPoseLoop;   // placeholder: PL_ADS_POSE

    [Header("Optional Additives (upper-body/additive layer)")]
    public AnimationClip fireAdditive;      // placeholder: PL_FIRE_ADD (ξοφ.)
    public AnimationClip handPoseAdditive;  // placeholder: PL_HANDPOSE_ADD (ξοφ.)
}
