using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerJump : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _jumpHeight = 1.2f;
    [SerializeField, Min(0f)] private float _gravityMultiplier = 1.7f;
    [SerializeField, Min(1f)] private float _fallMultiplier = 1.3f;
    [SerializeField, Min(0f)] private float _coyoteTime = 0.08f;
    [SerializeField, Min(0f)] private float _jumpBufferTime = 0.1f;

    private CharacterController _characterController;
    private float _verticalVelocity;
    private float _lastGroundedTime = float.NegativeInfinity;
    private float _lastJumpPressedTime = float.NegativeInfinity;

    private const float GroundedVelocity = -2f;

    public bool IsGrounded =>
        _characterController != null && _characterController.isGrounded;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        bool isGrounded = _characterController.isGrounded;

        if (isGrounded)
        {
            _lastGroundedTime = Time.time;

            if (_verticalVelocity < 0f)
            {
                _verticalVelocity = GroundedVelocity;
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            _lastJumpPressedTime = Time.time;
        }

        bool hasBufferedJump =
            Time.time - _lastJumpPressedTime <= _jumpBufferTime;
        bool canUseCoyoteTime =
            Time.time - _lastGroundedTime <= _coyoteTime;

        if (hasBufferedJump && canUseCoyoteTime)
        {
            float upwardGravity = Physics.gravity.y * _gravityMultiplier;
            _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * upwardGravity);
            _lastJumpPressedTime = float.NegativeInfinity;
            _lastGroundedTime = float.NegativeInfinity;
        }

        float gravityScale = _verticalVelocity < 0f
            ? _gravityMultiplier * _fallMultiplier
            : _gravityMultiplier;
        _verticalVelocity += Physics.gravity.y * gravityScale * Time.deltaTime;
        _characterController.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
    }
}
