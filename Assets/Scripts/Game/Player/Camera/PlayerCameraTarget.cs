using UnityEngine;
using Game.Core.Interfaces;

public class PlayerCameraTarget : MonoBehaviour, ICameraTargetProvider
{
    [SerializeField] private Transform cameraTarget;

    public Transform CameraTarget => cameraTarget;
}
