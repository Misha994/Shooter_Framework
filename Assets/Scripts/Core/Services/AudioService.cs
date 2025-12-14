using System.Collections.Generic;
using UnityEngine;
using Zenject;
using DG.Tweening;

public class AudioService : IAudioService
{
    private readonly Dictionary<AudioChannelType, AudioChannel> _channels = new();
    private readonly AudioConfig _config;
    private readonly SignalBus _signalBus;
    private readonly SFXAudioPool _sfxPool;

    [Inject]
    public AudioService(AudioConfig config, SignalBus signalBus, SFXAudioPool sfxPool)
    {
        _config = config;
        _signalBus = signalBus;
        _sfxPool = sfxPool;

        // Створюємо канали
        foreach (AudioChannelType type in System.Enum.GetValues(typeof(AudioChannelType)))
        {
            var go = new GameObject($"{type}Channel");
            Object.DontDestroyOnLoad(go);
            var source = go.AddComponent<AudioSource>();
            _channels[type] = new AudioChannel(source);
        }

        // Підписки
        _signalBus.Subscribe<AudioPlaySignal>(OnAudioPlay);
        _signalBus.Subscribe<AudioFadeSignal>(OnAudioFade);
        _signalBus.Subscribe<AudioPlay3DSignal>(OnPlay3DRequested);
    }

    // ==== IAudioService API ====

    public void Play(AudioClipId clipId, AudioChannelType channelType, bool loop = false)
    {
        var clip = _config.GetClip(clipId);
        if (clip == null)
        {
            Debug.LogWarning($"[AudioService] Clip not found: {clipId}");
            return;
        }

        if (_channels.TryGetValue(channelType, out var channel))
        {
            channel.Play(clip, loop);
        }
    }

    public void Stop(AudioChannelType type)
    {
        if (_channels.TryGetValue(type, out var channel))
            channel.Stop();
    }

    public void SetVolume(AudioChannelType type, float volume)
    {
        if (_channels.TryGetValue(type, out var channel))
            channel.SetVolume(volume);
    }

    public float GetVolume(AudioChannelType type)
    {
        return _channels.TryGetValue(type, out var channel) ? channel.GetVolume() : 0f;
    }

    public void Play3D(AudioClip clip, Vector3 position, float volume = 1f)
    {
        if (clip == null) return;

        if (_sfxPool != null)
        {
            _sfxPool.PlayAtPosition(clip, position, volume);
        }
        else
        {
            // fallback: одиничний AudioSource
            var go = new GameObject("SFX3D_Temp");
            go.transform.position = position;
            var src = go.AddComponent<AudioSource>();
            src.spatialBlend = 1f;
            src.clip = clip;
            src.volume = volume;
            src.Play();
            Object.Destroy(go, clip.length + 0.25f);
        }
    }

    // ==== Signals ====

    private void OnAudioPlay(AudioPlaySignal signal)
    {
        Play(signal.ClipId, signal.Channel, signal.Loop);
    }

    private void OnAudioFade(AudioFadeSignal signal)
    {
        if (!_channels.TryGetValue(signal.Channel, out var channel))
            return;

        float start = channel.GetVolume();
        float end = Mathf.Clamp01(signal.TargetVolume);

        // Tween гучності каналу
        DOTween.Kill(channel); // щоб не наслаювались твіни
        DOTween.To(() => start, v => { start = v; channel.SetVolume(v); }, end, Mathf.Max(0f, signal.Duration))
               .SetTarget(channel);
    }

    private void OnPlay3DRequested(AudioPlay3DSignal signal)
    {
        var clip = _config.GetClip(signal.ClipId);
        if (clip == null) return;

        // Через пул — з урахуванням просторових параметрів
        if (_sfxPool != null)
        {
            var src = _sfxPool.GetAvailableSource();
            src.clip = clip;
            src.volume = signal.Volume;
            src.transform.position = signal.Position;
            src.spatialBlend = signal.SpatialBlend;
            src.maxDistance = signal.MaxDistance;
            src.loop = signal.Loop;
            src.Play();
        }
        else
        {
            Play3D(clip, signal.Position, signal.Volume);
        }
    }
}
