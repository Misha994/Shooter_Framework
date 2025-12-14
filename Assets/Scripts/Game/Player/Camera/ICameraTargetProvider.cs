using UnityEngine;

namespace Game.Core.Interfaces
{
    public interface ICameraTargetProvider
    {
        Transform CameraTarget { get; }
    }
}
