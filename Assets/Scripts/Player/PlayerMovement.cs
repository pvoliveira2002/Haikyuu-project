using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _moveSpeed = 5f;
    [SerializeField, Min(0f)] private float _runSpeed = 8f;
    [SerializeField, Min(0f)] private float _rotationSpeed = 720f;

    private CharacterController _characterController;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");
        Vector3 moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;

        if (moveDirection.sqrMagnitude <= 0f)
        {
            return;
        }

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? _runSpeed : _moveSpeed;
        _characterController.Move(moveDirection * currentSpeed * Time.deltaTime);

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            _rotationSpeed * Time.deltaTime);
    }
}
