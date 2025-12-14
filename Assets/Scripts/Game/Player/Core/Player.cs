using Game.Core.Interfaces;
using UnityEngine;
using Zenject;

public class Player : MonoBehaviour
{
    public ICameraTargetProvider CameraTarget { get; private set; }

    [Inject]
    public void Construct(ICameraTargetProvider target)
    {
        CameraTarget = target;
    }
}
