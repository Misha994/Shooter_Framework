using UnityEngine;

public interface IAudioService
{
    void Play(AudioClipId clipName, AudioChannelType channelType, bool loop = false);
    void Stop(AudioChannelType channelType);
    void SetVolume(AudioChannelType channelType, float volume);
    float GetVolume(AudioChannelType channelType);
    void Play3D(AudioClip clip, Vector3 position, float volume = 1f);
}
