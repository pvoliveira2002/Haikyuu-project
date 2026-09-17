using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public sealed class PlayerJump : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _jumpHeight = 1.2f;
    [SerializeField] private float _gravity = -20f;
    [SerializeField] private float _groundedVelocity = -2f;

    private CharacterController _characterController;
    private float _verticalVelocity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        bool isGrounded = _characterController.isGrounded;

        if (isGrounded && _verticalVelocity < 0f)
        {
            _verticalVelocity = _groundedVelocity;
        }

        if (isGrounded && Input.GetKeyDown(KeyCode.Space))
        {
            _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        }

        _verticalVelocity += _gravity * Time.deltaTime;
        _characterController.Move(Vector3.up * _verticalVelocity * Time.deltaTime);
    }
}
