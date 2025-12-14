public class AudioPlaySignal
{
    public AudioClipId ClipId;
    public AudioChannelType Channel;
    public bool Loop;

    public AudioPlaySignal(AudioClipId clipId, AudioChannelType channel, bool loop = false)
    {
        ClipId = clipId;
        Channel = channel;
        Loop = loop;
    }
}
