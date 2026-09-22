using UnityEngine;

public sealed class SpikeSystem : MonoBehaviour
{
    [SerializeField] private BallContactZone _contactZone;
    [SerializeField] private PlayerJump _playerJump;
    [SerializeField] private SpikeTargetResolver _targetResolver;
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
    [SerializeField, Min(0.1f)] private float _minimumFlightTime = 0.55f;
    [SerializeField, Min(0.1f)] private float _maximumFlightTime = 1.1f;
    [SerializeField, Min(0f)] private float _landingHeight = 0.21f;
    [SerializeField] private TeamMember _teamMember;
    [SerializeField] private TeamPlayCoordinator _teamPlayCoordinator;

    private float _bufferEndTime = float.NegativeInfinity;
    private float _spikeWindowEndTime = float.NegativeInfinity;
    private bool _hasPendingInput;
    private bool _actionWindowOpened;
    private bool _lastInsideZone;
    private bool _lastHeightValid;
    private bool _lastAngleValid;

    public string AvailabilityStatus { get; private set; } = "GROUNDED";
    public Vector3 LastTarget => _targetResolver != null
        ? _targetResolver.LastTarget
        : Vector3.zero;
    public Vector3 PredictedLanding => _targetResolver != null
        ? _targetResolver.PredictedLanding
        : Vector3.zero;
    public string ContactQuality => _targetResolver != null
        ? _targetResolver.LastContactCategory
        : "N/A";
    public bool TargetInBounds =>
        _targetResolver != null && _targetResolver.LastTargetInBounds;

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

        if (_targetResolver == null)
        {
            _targetResolver = GetComponent<SpikeTargetResolver>();
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

        Vector3 spikeVelocity = CalculateSpikeVelocity(ball);

        BallTouchTracker touchTracker = ball.GetComponent<BallTouchTracker>();
        if (touchTracker != null &&
            !touchTracker.RegisterValidTouch(
                CourtSide.Player,
                _teamMember,
                BallTouchAction.Spike))
        {
            _hasPendingInput = false;
            _actionWindowOpened = false;
            return;
        }

        ball.ResetVelocity();
        float impulse = Mathf.Min(spikeVelocity.magnitude * ball.Mass, _spikeForce);
        ball.ApplyImpulse(spikeVelocity, impulse);
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

    private Vector3 CalculateSpikeVelocity(VolleyballBall ball)
    {
        Vector3 horizontal = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        if (horizontal.sqrMagnitude <= 0.0001f)
        {
            horizontal = Vector3.forward;
        }

        horizontal.Normalize();
        horizontal.z = Mathf.Max(horizontal.z, _minimumOpponentDirection);
        horizontal.Normalize();

        float quality = CalculateContactQuality(ball, horizontal);
        Vector3 target = _targetResolver != null
            ? _targetResolver.ResolveTarget(
                ball.transform.position,
                horizontal,
                Input.GetAxisRaw("Horizontal"),
                quality,
                _landingHeight)
            : ball.transform.position + horizontal * 6f;
        target.y = _landingHeight;

        float flightTime = _minimumFlightTime;
        Vector3 velocity = CalculateBallisticVelocity(
            ball.transform.position,
            target,
            flightTime);
        velocity.y -= _downwardBias;
        while (flightTime < _maximumFlightTime &&
            !ClearsNet(ball.transform.position, velocity, target))
        {
            flightTime = Mathf.Min(flightTime + 0.025f, _maximumFlightTime);
            velocity = CalculateBallisticVelocity(
                ball.transform.position,
                target,
                flightTime);
            velocity.y -= _downwardBias;
        }

        return velocity;
    }

    private float CalculateContactQuality(VolleyballBall ball, Vector3 horizontal)
    {
        float playerBase = transform.position.y - 1f;
        float height = ball.transform.position.y - playerBase;
        float idealHeight = Mathf.Lerp(
            _minimumContactHeight,
            _maximumContactHeight,
            0.65f);
        float heightRange = Mathf.Max(
            0.1f,
            (_maximumContactHeight - _minimumContactHeight) * 0.5f);
        float heightQuality = 1f - Mathf.Clamp01(
            Mathf.Abs(height - idealHeight) / heightRange);
        Vector3 ballOffset = Vector3.ProjectOnPlane(
            ball.transform.position - transform.position,
            Vector3.up);
        float forwardQuality = ballOffset.sqrMagnitude <= 0.0001f
            ? 1f
            : Mathf.InverseLerp(
                _minimumForwardDot,
                1f,
                Vector3.Dot(horizontal, ballOffset.normalized));
        return Mathf.Clamp01(heightQuality * 0.65f + forwardQuality * 0.35f);
    }

    private static Vector3 CalculateBallisticVelocity(
        Vector3 origin,
        Vector3 target,
        float flightTime)
    {
        Vector3 velocity = (target - origin) / flightTime;
        velocity.y += -0.5f * Physics.gravity.y * flightTime;
        return velocity;
    }

    private bool ClearsNet(Vector3 origin, Vector3 velocity, Vector3 target)
    {
        if (_netTopReference == null ||
            origin.z >= _netTopReference.position.z ||
            target.z <= _netTopReference.position.z)
        {
            return true;
        }

        float netTime =
            (_netTopReference.position.z - origin.z) / velocity.z;
        if (netTime <= 0f)
        {
            return false;
        }

        float heightAtNet = origin.y + velocity.y * netTime +
            0.5f * Physics.gravity.y * netTime * netTime;
        return heightAtNet >= _netTopReference.position.y + _netClearance;
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
