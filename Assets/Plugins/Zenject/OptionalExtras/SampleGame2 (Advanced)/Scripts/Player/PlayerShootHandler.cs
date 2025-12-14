using System;
using UnityEngine;

namespace Zenject.SpaceFighter
{
    public class PlayerShootHandler : ITickable
    {
        readonly AudioPlayer _audioPlayer;
        readonly Player _player;
        readonly Settings _settings;
        readonly PlayerInputState _inputState;

        float _lastFireTime;

        public PlayerShootHandler(
            PlayerInputState inputState,
            Settings settings,
            Player player,
            AudioPlayer audioPlayer)
        {
            _audioPlayer = audioPlayer;
            _player = player;
            _settings = settings;
            _inputState = inputState;
        }

        public void Tick()
        {
            if (_player.IsDead)
            {
                return;
            }

            if (_inputState.IsFiring && Time.realtimeSinceStartup - _lastFireTime > _settings.MaxShootInterval)
            {
                _lastFireTime = Time.realtimeSinceStartup;
                Fire();
            }
        }

        void Fire()
        {
            _audioPlayer.Play(_settings.Laser, _settings.LaserVolume);
        }

        [Serializable]
        public class Settings
        {
            public AudioClip Laser;
            public float LaserVolume = 1.0f;

            public float BulletLifetime;
            public float BulletSpeed;
            public float MaxShootInterval;
            public float BulletOffsetDistance;
        }
    }
}
