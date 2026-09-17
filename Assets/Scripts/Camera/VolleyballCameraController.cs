using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class VolleyballCameraController : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _ball;
    [SerializeField] private Transform _aiOpponent;
    [SerializeField] private Transform _courtCenter;
    [SerializeField, Min(0f)] private float _cameraHeight = 9.5f;
    [SerializeField, Min(0f)] private float _cameraDistance = 11f;
    [SerializeField, Range(0f, 1f)] private float _ballWeight = 0.35f;
    [SerializeField, Range(0f, 1f)] private float _lateralFollow = 0.4f;
    [SerializeField, Range(0f, 1f)] private float _verticalBallInfluence = 0.2f;
    [SerializeField, Min(0f)] private float _positionSmoothTime = 0.2f;
    [SerializeField, Min(0f)] private float _rotationSmoothTime = 0.15f;
    [SerializeField, Range(1f, 179f)] private float _minimumFov = 48f;
    [SerializeField, Range(1f, 179f)] private float _maximumFov = 54f;
    [SerializeField, Min(0f)] private float _fovSmoothSpeed = 6f;
    [SerializeField, Min(0.01f)] private float _impulseDuration = 0.1f;

    private const float OpponentWeight = 0.1f;
    private static readonly Vector2 LateralLimits = new Vector2(-4f, 4f);
    private static readonly Vector2 DepthLimits = new Vector2(-15f, -9.5f);
    private static readonly Vector2 HeightLimits = new Vector2(8f, 12f);
    private static readonly Vector2 FocusDepthLimits = new Vector2(-5f, 5f);

    private Camera _camera;
    private Vector3 _positionVelocity;
    private Vector3 _focusPoint;
    private Vector3 _appliedImpulseOffset;
    private float _impulseStrength;
    private float _impulseEndTime;

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

        _focusPoint = CalculateFocusPoint();
        transform.position = CalculateDesiredPosition();
        transform.rotation = Quaternion.LookRotation(_focusPoint - transform.position);
        _camera.fieldOfView = CalculateTargetFov();
    }

    private void LateUpdate()
    {
        if (!HasRequiredReferences())
        {
            return;
        }

        transform.position -= _appliedImpulseOffset;
        _appliedImpulseOffset = Vector3.zero;

        Vector3 desiredFocus = CalculateFocusPoint();
        float focusBlend = SmoothBlend(_rotationSmoothTime);
        _focusPoint = Vector3.Lerp(_focusPoint, desiredFocus, focusBlend);

        Vector3 desiredPosition = CalculateDesiredPosition();
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
            focusBlend);

        float targetFov = CalculateTargetFov();
        float fovBlend = 1f - Mathf.Exp(-_fovSmoothSpeed * Time.deltaTime);
        _camera.fieldOfView = Mathf.Lerp(
            _camera.fieldOfView,
            targetFov,
            fovBlend);

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

    private Vector3 CalculateFocusPoint()
    {
        Vector3 focus = Vector3.Lerp(_player.position, _ball.position, _ballWeight);
        if (_aiOpponent != null)
        {
            focus = Vector3.Lerp(focus, _aiOpponent.position, OpponentWeight);
        }

        float highBallOffset = Mathf.Clamp(
            (_ball.position.y - 2f) * _verticalBallInfluence,
            0f,
            2f);
        focus.x = Mathf.Clamp(focus.x, LateralLimits.x, LateralLimits.y);
        focus.y = _courtCenter.position.y + 1.5f + highBallOffset;
        focus.z = Mathf.Clamp(
            focus.z,
            _courtCenter.position.z + FocusDepthLimits.x,
            _courtCenter.position.z + FocusDepthLimits.y);
        return focus;
    }

    private Vector3 CalculateDesiredPosition()
    {
        float lateralOffset = (_focusPoint.x - _courtCenter.position.x) * _lateralFollow;
        float longitudinalOffset = (_player.position.z - _courtCenter.position.z) * 0.2f;
        float highBallOffset = Mathf.Clamp(
            (_ball.position.y - 2f) * _verticalBallInfluence,
            0f,
            2f);

        return new Vector3(
            _courtCenter.position.x + Mathf.Clamp(
                lateralOffset,
                LateralLimits.x,
                LateralLimits.y),
            Mathf.Clamp(
                _courtCenter.position.y + _cameraHeight + highBallOffset,
                HeightLimits.x,
                HeightLimits.y),
            _courtCenter.position.z + Mathf.Clamp(
                -_cameraDistance + longitudinalOffset,
                DepthLimits.x,
                DepthLimits.y));
    }

    private float CalculateTargetFov()
    {
        float playerBallDistance = Vector3.Distance(_player.position, _ball.position);
        float distanceFactor = Mathf.InverseLerp(2f, 15f, playerBallDistance);
        return Mathf.Lerp(_minimumFov, _maximumFov, distanceFactor);
    }

    private float SmoothBlend(float smoothTime)
    {
        return smoothTime <= 0f
            ? 1f
            : 1f - Mathf.Exp(-Time.deltaTime / smoothTime);
    }

    private bool HasRequiredReferences()
    {
        return _player != null &&
               _ball != null &&
               _courtCenter != null &&
               _camera != null;
    }

    private void OnDrawGizmosSelected()
    {
        if (_player == null || _ball == null || _courtCenter == null)
        {
            return;
        }

        Vector3 focus = Application.isPlaying ? _focusPoint : CalculateFocusPoint();
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(focus, 0.3f);
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(CalculateDesiredPosition(), 0.35f);
        Gizmos.color = Color.white;
        Gizmos.DrawLine(CalculateDesiredPosition(), focus);
    }
}
