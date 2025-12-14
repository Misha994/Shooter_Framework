using UnityEngine;

public class WeaponSoundPlayer : IWeaponSoundPlayer
{
    private readonly IAudioService audio;
    private readonly AudioClip shoot;
    private readonly AudioClip reload;
    private readonly Transform positionSource; // <-- ÆÈÂÀ ïîçèö³ÿ çáðî¿

    public WeaponSoundPlayer(IAudioService audioService, AudioClip shootSfx, AudioClip reloadSfx, Transform positionSource)
    {
        audio = audioService;
        shoot = shootSfx;
        reload = reloadSfx;
        this.positionSource = positionSource;
    }

    public void PlayShootSound()
    {
        if (shoot != null && positionSource != null)
            audio?.Play3D(shoot, positionSource.position);
    }

    public void PlayReloadSound()
    {
        if (reload != null && positionSource != null)
            audio?.Play3D(reload, positionSource.position);
    }
}
