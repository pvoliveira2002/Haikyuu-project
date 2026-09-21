using UnityEngine;

public sealed class SpikeSystem : MonoBehaviour
{
    [SerializeField] private BallContactZone _contactZone;
    [SerializeField] private PlayerJump _playerJump;
    [SerializeField] private KeyCode _spikeKey = KeyCode.F;
    [SerializeField, Min(0f)] private float _spikeForce = 4f;
    [SerializeField, Min(0f)] private float _forwardBias = 1f;
    [SerializeField, Min(0f)] private float _downwardBias = 0.12f;
    [SerializeField, Min(0f)] private float _minimumContactHeight = 1.35f;
    [SerializeField, Min(0f)] private float _maximumContactHeight = 3.1f;
    [SerializeField, Min(0f)] private float _spikeWindow = 0.35f;
    [SerializeField, Min(0f)] private float _inputBuffer = 0.2f;
    [SerializeField, Range(-1f, 1f)] private float _minimumForwardDot = 0.08f;
    [SerializeField] private Transform _netTopReference;
    [SerializeField, Min(0f)] private float _netClearance = 0.25f;
    [SerializeField, Range(0f, 1f)] private float _minimumOpponentDirection = 0.35f;

    private float _bufferEndTime = float.NegativeInfinity;
    private float _spikeWindowEndTime = float.NegativeInfinity;
    private bool _hasPendingInput;
    private bool _actionWindowOpened;
    private bool _lastInsideZone;
    private bool _lastHeightValid;
    private bool _lastAngleValid;

    public string AvailabilityStatus { get; private set; } = "GROUNDED";

    private void Awake()
    {
        if (_contactZone == null)
        {
            _contactZone = GetComponentInChildren<BallContactZone>();
        }

        if (_playerJump == null)
        {
            _playerJump = GetComponent<PlayerJump>();
        }
    }

    private void Update()
    {
        if (_contactZone == null || _playerJump == null)
        {
            return;
        }

        if (Input.GetKeyDown(_spikeKey))
        {
            _bufferEndTime = Time.time + _inputBuffer;
            _hasPendingInput = true;
            _actionWindowOpened = false;
            _lastInsideZone = false;
            _lastHeightValid = false;
            _lastAngleValid = false;
        }

        if (_hasPendingInput && !_actionWindowOpened &&
            Time.time <= _bufferEndTime && !_playerJump.IsGrounded)
        {
            _spikeWindowEndTime = Time.time + _spikeWindow;
            _actionWindowOpened = true;
        }

        if (_hasPendingInput && !_actionWindowOpened && Time.time > _bufferEndTime)
        {
            LogRejected(false);
            _hasPendingInput = false;
        }

        if (_hasPendingInput && _actionWindowOpened && _playerJump.IsGrounded)
        {
            LogRejected(false);
            _hasPendingInput = false;
        }
        else if (_hasPendingInput && _actionWindowOpened &&
            Time.time > _spikeWindowEndTime)
        {
            LogRejected(!_playerJump.IsGrounded);
            _hasPendingInput = false;
        }

        if (!_hasPendingInput || !_actionWindowOpened ||
            _playerJump.IsGrounded || Time.time > _spikeWindowEndTime)
        {
            AvailabilityStatus = _playerJump.IsGrounded
                ? "GROUNDED"
                : "WINDOW";
            return;
        }

        VolleyballBall ball = _contactZone.BallInRange;
        _contactZone.EvaluateContact(
            ball,
            _minimumContactHeight,
            _maximumContactHeight,
            _minimumForwardDot,
            out bool insideZone,
            out bool heightValid,
            out bool angleValid);
        _lastInsideZone = insideZone;
        _lastHeightValid = heightValid;
        _lastAngleValid = angleValid;
        if (!insideZone)
        {
            AvailabilityStatus = "OUT OF ZONE";
            return;
        }

        if (!heightValid)
        {
            AvailabilityStatus = "HEIGHT";
            return;
        }

        if (!angleValid)
        {
            AvailabilityStatus = "ANGLE";
            return;
        }

        if (_contactZone.ContactCooldownRemaining > 0f ||
            !_contactZone.TryConsumeContact(ball))
        {
            AvailabilityStatus = "COOLDOWN";
            return;
        }

        Vector3 spikeDirection = CalculateSpikeDirection(ball);

        ball.ResetVelocity();
        ball.ApplyImpulse(spikeDirection, _spikeForce);
        ball.GetComponent<BallTouchTracker>()?.RegisterTouch(CourtSide.Player);
        ActionFeedbackController.PlayFeedback(
            ActionFeedbackType.Spike,
            ball.transform.position);
        AvailabilityStatus = "READY";
        _spikeWindowEndTime = float.NegativeInfinity;
        _bufferEndTime = float.NegativeInfinity;
        _hasPendingInput = false;
        _actionWindowOpened = false;
        Debug.Log("SPIKE ACCEPTED", this);
    }

    private Vector3 CalculateSpikeDirection(VolleyballBall ball)
    {
        Vector3 horizontal = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (horizontal.sqrMagnitude <= 0.0001f)
        {
            horizontal = Vector3.forward;
        }

        horizontal.Normalize();
        horizontal.z = Mathf.Max(horizontal.z, _minimumOpponentDirection);
        horizontal.Normalize();

        float verticalRatio = -_downwardBias;
        if (_netTopReference != null && ball.transform.position.z < _netTopReference.position.z &&
            horizontal.z > 0.01f)
        {
            float distanceToNet =
                (_netTopReference.position.z - ball.transform.position.z) / horizontal.z;
            float requiredRise = Mathf.Max(
                0f,
                _netTopReference.position.y + _netClearance - ball.transform.position.y);
            float launchSpeed = _spikeForce / ball.Mass;
            float gravityTerm = 0.5f * -Physics.gravity.y *
                distanceToNet * distanceToNet / (launchSpeed * launchSpeed);
            float discriminant = distanceToNet * distanceToNet -
                4f * gravityTerm * (requiredRise + gravityTerm);
            if (gravityTerm > 0.0001f && discriminant >= 0f)
            {
                float requiredRatio =
                    (distanceToNet - Mathf.Sqrt(discriminant)) / (2f * gravityTerm);
                verticalRatio = Mathf.Max(verticalRatio, requiredRatio);
            }
        }

        return (horizontal * _forwardBias + Vector3.up * verticalRatio).normalized;
    }

    private void LogRejected(bool airborne)
    {
        bool cooldownReady = _contactZone.ContactCooldownRemaining <= 0f;
        Debug.Log(
            $"SPIKE REJECTED | Airborne={airborne} | Zone={_lastInsideZone} | " +
            $"Height={_lastHeightValid} | Angle={_lastAngleValid} | " +
            $"Window={_actionWindowOpened} | Cooldown={cooldownReady}",
            this);
    }

    private void OnDrawGizmosSelected()
    {
        _contactZone?.DrawHeightRange(
            _minimumContactHeight,
            _maximumContactHeight,
            Color.red);
    }
}
