using System.Collections.Generic;
using UnityEngine;

public class SFXAudioPool : MonoBehaviour
{
    private List<AudioSource> _pool = new List<AudioSource>();
    private int _capacity = 20;

    private void Awake()
    {
        for (int i = 0; i < _capacity; i++)
        {
            var go = new GameObject($"AudioSource_{i}");
            go.transform.parent = transform;
            var source = go.AddComponent<AudioSource>();
            source.spatialBlend = 1f; // 3D sound
            source.playOnAwake = false;
            _pool.Add(source);
        }
    }

    public AudioSource GetAvailableSource()
    {
        foreach (var s in _pool)
        {
            if (!s.isPlaying)
                return s;
        }

        // If all busy — create new (optional, or just reuse oldest)
        var go = new GameObject("AudioSource_Extra");
        go.transform.parent = transform;
        var source = go.AddComponent<AudioSource>();
        source.spatialBlend = 1f;
        source.playOnAwake = false;
        _pool.Add(source);
        return source;
    }

    public void PlayAtPosition(AudioClip clip, Vector3 position, float volume = 1f)
    {
        var source = GetAvailableSource();
        source.clip = clip;
        source.volume = volume;
        source.transform.position = position;
        source.Play();
    }
}
