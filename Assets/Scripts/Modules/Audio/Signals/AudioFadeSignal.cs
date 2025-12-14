public class AudioFadeSignal
{
    public AudioChannelType Channel;
    public float TargetVolume;
    public float Duration;

    public AudioFadeSignal(AudioChannelType channel, float targetVolume, float duration = 1f)
    {
        Channel = channel;
        TargetVolume = targetVolume;
        Duration = duration;
    }
}
