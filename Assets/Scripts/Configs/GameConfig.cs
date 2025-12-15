// GameConfig.cs
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

	[Header("Camera · Look (base)")]
	[Tooltip("Базова чутливість огляду (hip). Використовується як множник для mouseSensitivityHip/ADS.")]
	[Min(0.01f)] public float LookSensitivity = 1.2f;

	[Range(-89f, 0f)] public float MinPitch = -80f;
	[Range(0f, 89f)] public float MaxPitch = 80f;

	[Header("Mouse / Aim multipliers")]
	[Range(0.1f, 5f)]
	public float mouseSensitivityHip = 1.0f;   // HIP чутливість

	[Range(0.1f, 5f)]
	public float mouseSensitivityADS = 0.25f;  // Дуже тонкий ADS

	// --- опціонально, якщо захочеш тягнути ці значення в HeadDrivenBodyAligner через DI ---
	[Header("Body follow · Arma-style")]
	[Tooltip("Вільний кут між корпусом і камерою в HIP.")]
	[Range(0f, 90f)] public float hipFreeYaw = 40f;

	[Tooltip("Вільний кут між корпусом і камерою в ADS.")]
	[Range(0f, 45f)] public float adsFreeYaw = 5f;

	[Tooltip("Жорстка межа, після якої тіло агресивно доганяє камеру.")]
	[Range(45f, 180f)] public float hardYaw = 120f;

	[Header("Body follow · speeds (deg/sec)")]
	public float hipFollowSpeed = 150f;
	public float adsFollowSpeed = 120f;
	public float catchUpSpeed = 360f;

	[Header("Body follow · recenter & smoothing")]
	public bool autoRecenterBody = true;
	public float bodyRecenterDelay = 0.35f;
	public float bodyRecenterSpeed = 90f;
	[Tooltip("Час згладжування кута туловища (чим менше – тим різкіше).")]
	[Min(0.01f)] public float bodyYawSmoothTime = 0.07f;
}
