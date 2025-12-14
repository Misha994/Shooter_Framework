using System;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Zenject.SpaceFighter
{
    public class EnemyStateFollow : IEnemyState
    {
        readonly EnemyRotationHandler _rotationHandler;
        readonly EnemyCommonSettings _commonSettings;
        readonly Settings _settings;
        readonly EnemyTunables _tunables;
        readonly EnemyStateManager _stateManager;
        readonly EnemyView _view;

        bool _strafeRight;
        float _lastStrafeChangeTime;

        public EnemyStateFollow(
            EnemyView view,
            EnemyStateManager stateManager,
            EnemyTunables tunables,
            Settings settings,
            EnemyCommonSettings commonSettings,
            EnemyRotationHandler rotationHandler)
        {
            _rotationHandler = rotationHandler;
            _commonSettings = commonSettings;
            _settings = settings;
            _tunables = tunables;
            _stateManager = stateManager;
            _view = view;
        }

        public void EnterState()
        {
            _strafeRight = Random.Range(0, 1) == 0;
            _lastStrafeChangeTime = Time.realtimeSinceStartup;
        }

        public void ExitState()
        {
        }

        public void Update()
        {

            // Strafe back and forth over the given interval
            // This helps avoiding being too easy a target
            if (Time.realtimeSinceStartup - _lastStrafeChangeTime > _settings.StrafeChangeInterval)
            {
                _lastStrafeChangeTime = Time.realtimeSinceStartup;
                _strafeRight = !_strafeRight;
            }
        }

        public void FixedUpdate()
        {
            Strafe();
        }

        void Strafe()
        {
            // Strafe to avoid getting hit too easily
            if (_strafeRight)
            {
                _view.AddForce(_view.RightDir * _settings.StrafeMultiplier * _tunables.Speed);
            }
            else
            {
                _view.AddForce(-_view.RightDir * _settings.StrafeMultiplier * _tunables.Speed);
            }
        }


        [Serializable]
        public class Settings
        {
            public float StrafeMultiplier;
            public float StrafeChangeInterval;
            public float TeleportNewDistance;
        }
    }
}

