using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _moveSpeed = 5f;
    [SerializeField, Min(0f)] private float _runSpeed = 8f;
    [SerializeField, Min(0f)] private float _acceleration = 20f;
    [SerializeField, Min(0f)] private float _deceleration = 25f;
    [SerializeField, Min(0f)] private float _rotationSpeed = 12f;
    [SerializeField, Range(0f, 1f)] private float _airControl = 0.75f;

    private CharacterController _characterController;
    private Vector3 _horizontalVelocity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        Vector3 inputDirection = Vector3.ClampMagnitude(
            new Vector3(horizontalInput, 0f, verticalInput),
            1f);
        float targetSpeed = Input.GetKey(KeyCode.LeftShift)
            ? _runSpeed
            : _moveSpeed;
        Vector3 targetVelocity = inputDirection * targetSpeed;
        float controlMultiplier = _characterController.isGrounded
            ? 1f
            : _airControl;
        float changeRate = inputDirection.sqrMagnitude > 0f
            ? _acceleration
            : _deceleration;

        if (Vector3.Dot(_horizontalVelocity, targetVelocity) < 0f)
        {
            changeRate = _acceleration + _deceleration;
        }

        _horizontalVelocity = Vector3.MoveTowards(
            _horizontalVelocity,
            targetVelocity,
            changeRate * controlMultiplier * Time.deltaTime);

        _characterController.Move(_horizontalVelocity * Time.deltaTime);

        if (inputDirection.sqrMagnitude > 0f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(inputDirection);
            float rotationBlend = 1f - Mathf.Exp(-_rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationBlend);
        }
    }
}
