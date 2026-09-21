using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class VolleyballCameraController : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _playerPivot;
    [SerializeField] private Transform _ball;
    [SerializeField, Min(0f)] private float _distance = 4.8f;
    [SerializeField, Min(0f)] private float _height = 2.5f;
    [SerializeField, Min(0f)] private float _playerPivotHeight = 1.4f;
    [SerializeField, Min(0f)] private float _mouseSensitivity = 2f;
    [SerializeField, Range(-89f, 89f)] private float _minimumPitch = -10f;
    [SerializeField, Range(-89f, 89f)] private float _maximumPitch = 45f;
    [SerializeField, Range(0f, 1f)] private float _ballLookWeight = 0.3f;
    [SerializeField, Min(0f)] private float _maximumBallAssistDistance = 4f;
    [SerializeField, Min(0f)] private float _positionSmoothTime = 0.1f;
    [SerializeField, Min(0f)] private float _rotationSmoothTime = 0.08f;
    [SerializeField, Range(55f, 75f)] private float _fieldOfView = 64f;
    [SerializeField] private LayerMask _collisionMask = ~0;
    [SerializeField, Min(0f)] private float _collisionRadius = 0.25f;
    [SerializeField, Min(0f)] private float _collisionPadding = 0.1f;
    [SerializeField, Min(0.01f)] private float _impulseDuration = 0.1f;

    private readonly RaycastHit[] _collisionHits = new RaycastHit[12];
    private Camera _camera;
    private Vector3 _positionVelocity;
    private Vector3 _focusPoint;
    private Vector3 _appliedImpulseOffset;
    private float _yaw;
    private float _pitch = 12f;
    private float _impulseStrength;
    private float _impulseEndTime;

    public float Yaw => _yaw;
    public float Pitch => _pitch;
    public float Distance => _distance;
    public float BallAssistWeight => _ballLookWeight;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
    }

    private void Start()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        _yaw = _player.eulerAngles.y;
        _camera.fieldOfView = _fieldOfView;
        _focusPoint = CalculateFocusPoint();
        transform.position = ResolveCollision(CalculateDesiredPosition());
        transform.rotation = Quaternion.LookRotation(
            _focusPoint - transform.position,
            Vector3.up);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (Input.GetMouseButtonDown(0) &&
                 Cursor.lockState != CursorLockMode.Locked)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        _yaw += Input.GetAxis("Mouse X") * _mouseSensitivity;
        _pitch = Mathf.Clamp(
            _pitch - Input.GetAxis("Mouse Y") * _mouseSensitivity,
            Mathf.Min(_minimumPitch, _maximumPitch),
            Mathf.Max(_minimumPitch, _maximumPitch));
    }

    private void LateUpdate()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        transform.position -= _appliedImpulseOffset;
        _appliedImpulseOffset = Vector3.zero;

        float rotationBlend = SmoothBlend(_rotationSmoothTime);
        _focusPoint = Vector3.Lerp(
            _focusPoint,
            CalculateFocusPoint(),
            rotationBlend);

        Vector3 desiredPosition = ResolveCollision(CalculateDesiredPosition());
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref _positionVelocity,
            _positionSmoothTime);

        Quaternion desiredRotation = Quaternion.LookRotation(
            _focusPoint - transform.position,
            Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            desiredRotation,
            rotationBlend);
        _camera.fieldOfView = _fieldOfView;

        ApplyCurrentImpulse();
    }

    public void ApplyImpulse(float strength)
    {
        if (strength <= 0f)
        {
            return;
        }

        _impulseStrength = Mathf.Max(_impulseStrength, strength);
        _impulseEndTime = Time.time + _impulseDuration;
    }

    private Vector3 PivotPosition => _playerPivot != null
        ? _playerPivot.position
        : _player.position + Vector3.up * _playerPivotHeight;

    private Vector3 CalculateDesiredPosition()
    {
        Quaternion yawRotation = Quaternion.Euler(0f, _yaw, 0f);
        Vector3 forward = yawRotation * Vector3.forward;
        return PivotPosition + Vector3.up * _height - forward * _distance;
    }

    private Vector3 CalculateFocusPoint()
    {
        Vector3 pivot = PivotPosition;
        Vector3 manualDirection = Quaternion.Euler(_pitch, _yaw, 0f) *
            Vector3.forward;
        Vector3 manualFocus = pivot + manualDirection * 10f;
        Vector3 ballOffset = Vector3.ClampMagnitude(
            _ball.position - pivot,
            _maximumBallAssistDistance);
        Vector3 assistedFocus = manualFocus + ballOffset;
        return Vector3.Lerp(manualFocus, assistedFocus, _ballLookWeight);
    }

    private Vector3 ResolveCollision(Vector3 desiredPosition)
    {
        Vector3 pivot = PivotPosition;
        Vector3 offset = desiredPosition - pivot;
        float distance = offset.magnitude;
        if (distance <= 0.001f)
        {
            return desiredPosition;
        }

        int hitCount = Physics.SphereCastNonAlloc(
            pivot,
            _collisionRadius,
            offset.normalized,
            _collisionHits,
            distance,
            _collisionMask,
            QueryTriggerInteraction.Ignore);
        float closestDistance = distance;
        for (int index = 0; index < hitCount; index++)
        {
            RaycastHit hit = _collisionHits[index];
            if (hit.collider == null ||
                hit.collider.transform.root == _player.root)
            {
                continue;
            }

            closestDistance = Mathf.Min(closestDistance, hit.distance);
        }

        return pivot + offset.normalized * Mathf.Max(
            0f,
            closestDistance - _collisionPadding);
    }

    private void ApplyCurrentImpulse()
    {
        if (Time.time >= _impulseEndTime || _impulseStrength <= 0f)
        {
            _impulseStrength = 0f;
            return;
        }

        float remaining = Mathf.Clamp01(
            (_impulseEndTime - Time.time) / _impulseDuration);
        _appliedImpulseOffset = Random.insideUnitSphere *
            _impulseStrength * remaining;
        transform.position += _appliedImpulseOffset;
    }

    private float SmoothBlend(float smoothTime)
    {
        return smoothTime <= 0f
            ? 1f
            : 1f - Mathf.Exp(-Time.deltaTime / smoothTime);
    }

    private bool HasRequiredReferences()
    {
        return _player != null && _ball != null && _camera != null;
    }

    private void OnDrawGizmosSelected()
    {
        if (_player == null || _ball == null)
        {
            return;
        }

        Vector3 pivot = PivotPosition;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(pivot, 0.2f);
        Gizmos.color = Color.white;
        Gizmos.DrawLine(pivot, CalculateDesiredPosition());
    }
}
