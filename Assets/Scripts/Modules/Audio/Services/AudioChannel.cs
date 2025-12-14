using UnityEngine;

public class AudioChannel
{
    public AudioSource Source { get; }
    public float Volume { get; set; } = 1f;

    public AudioChannel(AudioSource source)
    {
        Source = source;
    }

    public void Play(AudioClip clip, bool loop = false)
    {
        if (clip == null) return;
        Source.clip = clip;
        Source.loop = loop;
        Source.volume = Volume;
        Source.Play();
    }

    public void Stop()
    {
        Source.Stop();
    }

    public void SetVolume(float volume)
    {
        Volume = volume;
        Source.volume = volume;
    }

    public float GetVolume() => Source.volume;

}
