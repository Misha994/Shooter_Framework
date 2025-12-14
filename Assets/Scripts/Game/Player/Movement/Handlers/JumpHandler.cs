using UnityEngine;

public class JumpHandler
{
    private readonly float _jumpForce;
    private readonly float _coyoteTime;
    private readonly float _jumpBufferTime;

    private float _coyoteTimer;
    private float _jumpBufferTimer;

    public JumpHandler(float jumpForce, float coyoteTime = 0.15f, float jumpBufferTime = 0.2f)
    {
        _jumpForce = jumpForce;
        _coyoteTime = coyoteTime;
        _jumpBufferTime = jumpBufferTime;
    }

    public void UpdateTimers(bool isGrounded, bool jumpPressed)
    {
        _coyoteTimer = isGrounded ? _coyoteTime : Mathf.Max(0, _coyoteTimer - Time.deltaTime);
        _jumpBufferTimer = jumpPressed ? _jumpBufferTime : Mathf.Max(0, _jumpBufferTimer - Time.deltaTime);
    }

    public bool ShouldJump() => _coyoteTimer > 0 && _jumpBufferTimer > 0;

    public float GetJumpVelocity() => _jumpForce;

    public void ResetJumpBuffer() => _jumpBufferTimer = 0;
}
