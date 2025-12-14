using UnityEngine;

[CreateAssetMenu(fileName = "GameConfig", menuName = "Configs/GameConfig")]
public class GameConfig : ScriptableObject
{
    [Header("Player · Movement")]
    [Min(0f)] public float MoveSpeed = 5f;
    [Min(0f)] public float SprintSpeed = 8f;
    [Min(0f)] public float AirControlFactor = 0.15f;

    [Header("Player · Jump & Gravity")]
    [Min(0f)] public float JumpForce = 6f;
    [Min(0f)] public float Gravity = 20f;

    [Header("Player · Stats/UI")]
    [Min(1)] public int MaxHealth = 100;

    [Header("Camera · Look")]
    [Tooltip("Базова чутливість огляду (hip). ADS множиться з WeaponConfig.adsSensitivityMultiplier")]
    [Min(0.01f)] public float LookSensitivity = 2f;
    [Range(-89, 0)] public float MinPitch = -80f;
    [Range(0, 89)] public float MaxPitch = 80f;
}
