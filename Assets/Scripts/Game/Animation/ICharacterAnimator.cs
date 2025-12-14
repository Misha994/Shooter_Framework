using UnityEngine;

public interface ICharacterAnimator
{
    void SetupWeapon(WeaponAnimationSet set);
    void SetLocomotion(float speed, float strafeX, float strafeY, bool grounded);
    void SetAim(bool isAiming);

    void PlayFire();
    void PlayReload();      // старт анімації (commit робимо через sink або нормалізований час)
    void CompleteReload();  // коли clip доходить до commit (або таймером)
    void PlayDraw();
    void PlayHolster();

    void PlayHit(Vector3 worldHitPoint, float power = 1f);
    void PlayDeath();
}
