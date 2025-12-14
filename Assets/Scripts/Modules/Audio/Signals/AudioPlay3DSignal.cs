using UnityEngine;

public class AudioPlay3DSignal
{
    public AudioClipId ClipId;
    public Vector3 Position;
    public float Volume;
    public float SpatialBlend;
    public float MaxDistance;
    public bool Loop;

    public AudioPlay3DSignal(AudioClipId clipId, Vector3 position, float volume = 1f, float spatialBlend = 1f, float maxDistance = 30f, bool loop = false)
    {
        ClipId = clipId;
        Position = position;
        Volume = volume;
        SpatialBlend = spatialBlend;
        MaxDistance = maxDistance;
        Loop = loop;
    }
}
