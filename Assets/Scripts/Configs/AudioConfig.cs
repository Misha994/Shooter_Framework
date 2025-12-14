using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Configs/AudioConfig")]
public class AudioConfig : ScriptableObject
{
    [SerializeField] private List<AudioEntry> clips;

    private Dictionary<AudioClipId, AudioClip> _clipMap;

    private void OnEnable()
    {
        _clipMap = new Dictionary<AudioClipId, AudioClip>();
        foreach (var entry in clips)
        {
            if (!_clipMap.ContainsKey(entry.id))
                _clipMap[entry.id] = entry.clip;
        }
    }

    public AudioClip GetClip(AudioClipId id)
    {
        return _clipMap.TryGetValue(id, out var clip) ? clip : null;
    }

    [System.Serializable]
    public class AudioEntry
    {
        public AudioClipId id;
        public AudioClip clip;
    }
}
