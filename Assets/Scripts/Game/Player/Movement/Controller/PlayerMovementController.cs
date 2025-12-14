using UnityEngine;
using Zenject;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovementController : MonoBehaviour
{
	[Inject] private IInputService _input;
	[Inject] private GameConfig _cfg;

	[Header("Refs")]
	[SerializeField] private Transform cameraTransform;

	private CharacterController _controller;
	private float _yVelocity;
	private Vector3 _horizontalVelocity;

	public bool IsGrounded => _controller.isGrounded;
	public float VerticalVelocity => _yVelocity;

	private void Awake()
	{
		_controller = GetComponent<CharacterController>();
		// cameraTransform автоматично заповнить AssignMainCameraToMovement
	}

	// ВАЖЛИВО: рух переносимо в LateUpdate — після того, як Cinemachine оновила камеру
	private void LateUpdate()
	{
		Vector2 move = _input.GetMoveAxis();
		bool isSprinting = _input.IsSprintHeld();

		ApplyGravity();
		Move(move, isSprinting);
		MoveCharacter();

		if (_input.IsJumpPressed() && IsGrounded)
		{
			Jump();
		}
	}

	public void Move(Vector2 input, bool isSprinting)
	{
		Vector3 direction = GetInputDirection(input);
		float speed = isSprinting ? _cfg.SprintSpeed : _cfg.MoveSpeed;
		_horizontalVelocity = direction * speed;
	}

	public void ApplyAirControl(Vector2 input)
	{
		if (input == Vector2.zero) return;

		Vector3 inputDir = GetInputDirection(input);
		float currentSpeed = _horizontalVelocity.magnitude;
		if (currentSpeed < 0.1f)
			currentSpeed = _cfg.MoveSpeed;

		_horizontalVelocity = Vector3.Lerp(
			_horizontalVelocity,
			inputDir * currentSpeed,
			_cfg.AirControlFactor * Time.deltaTime
		);
	}

	public void ApplyGravity()
	{
		if (IsGrounded && _yVelocity < 0f)
			_yVelocity = -2f;
		else
			_yVelocity -= _cfg.Gravity * Time.deltaTime;
	}

	public void Jump()
	{
		_yVelocity = _cfg.JumpForce;
	}

	public void MoveCharacter()
	{
		Vector3 velocity = _horizontalVelocity + Vector3.up * _yVelocity;
		_controller.Move(velocity * Time.deltaTime);
	}

	private Vector3 GetInputDirection(Vector2 input)
	{
		Vector3 forward = cameraTransform ? cameraTransform.forward : Vector3.forward;
		Vector3 right = cameraTransform ? cameraTransform.right : Vector3.right;
		forward.y = right.y = 0f;
		return (forward.normalized * input.y + right.normalized * input.x).normalized;
	}
}
