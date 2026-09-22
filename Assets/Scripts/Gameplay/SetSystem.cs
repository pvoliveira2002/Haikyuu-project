using UnityEngine;

public sealed class SetSystem : MonoBehaviour
{
    [SerializeField] private BallContactZone _contactZone;
    [SerializeField] private KeyCode _setKey = KeyCode.Q;
    [SerializeField, Min(0f)] private float _inputBuffer = 0.15f;
    [SerializeField, Min(0f)] private float _setForce = 2.35f;
    [SerializeField, Min(0f)] private float _verticalComponent = 1.2f;
    [SerializeField, Min(0f)] private float _forwardComponent = 0.18f;
    [SerializeField] private Transform _spikePreparationTarget;
    [SerializeField] private Transform _netReference;
    [SerializeField] private CourtSide _courtSide = CourtSide.Player;
    [SerializeField, Min(0.8f)] private float _attackDistanceFromNet = 1.4f;
    [SerializeField, Min(0f)] private float _setTargetHeight = 2.25f;
    [SerializeField, Min(0f)] private float _setApexHeight = 3.3f;
    [SerializeField, Min(0f)] private float _lateralTargetInfluence = 1f;
    [SerializeField, Min(0f)] private float _minimumContactHeight = 0.65f;
    [SerializeField, Min(0f)] private float _maximumContactHeight = 2.5f;
    [SerializeField, Range(-1f, 1f)] private float _minimumForwardDot = -0.2f;
    [SerializeField] private TeamMember _teamMember;
    [SerializeField] private TeamPlayCoordinator _teamPlayCoordinator;

    private float _bufferEndTime = float.NegativeInfinity;
    private bool _hasPendingInput;
    private bool _lastInsideZone;
    private bool _lastHeightValid;
    private bool _lastAngleValid;

    public string AvailabilityStatus { get; private set; } = "BUFFER";
    public Vector3 LastTarget { get; private set; }
    public float LastDistanceToNet { get; private set; }
    public float SetApexHeight => _setApexHeight;

    private void Awake()
    {
        if (_contactZone == null)
        {
            _contactZone = GetComponentInChildren<BallContactZone>();
        }
    }

    private void Update()
    {
        if (_contactZone == null)
        {
            return;
        }

        if (Input.GetKeyDown(_setKey) &&
            !Input.GetKeyDown(KeyCode.F) &&
            !Input.GetKeyDown(KeyCode.C))
        {
            _bufferEndTime = Time.time + _inputBuffer;
            _hasPendingInput = true;
            _lastInsideZone = false;
            _lastHeightValid = false;
            _lastAngleValid = false;
        }

        if (Time.time > _bufferEndTime)
        {
            if (_hasPendingInput)
            {
                LogRejected();
                _hasPendingInput = false;
            }

            AvailabilityStatus = "BUFFER";
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

        Vector3 setVelocity = CalculateSetVelocity(ball);

        ball.ResetVelocity();
        ball.ApplyImpulse(setVelocity, setVelocity.magnitude * ball.Mass);
        ball.GetComponent<BallTouchTracker>()?.RegisterTouch(CourtSide.Player);
        _teamPlayCoordinator?.NotifyContact(_teamMember, TeamPlayAction.Set);
        ActionFeedbackController.PlayFeedback(
            ActionFeedbackType.Set,
            ball.transform.position);
        AvailabilityStatus = "READY";
        _bufferEndTime = float.NegativeInfinity;
        _hasPendingInput = false;
        Debug.Log("SET ACCEPTED", this);
    }

    private Vector3 CalculateSetVelocity(VolleyballBall ball)
    {
        if (_teamPlayCoordinator != null &&
            _teamPlayCoordinator.GetPlannedAction(_teamMember) == TeamPlayAction.Set)
        {
            Vector3 coordinatedTarget = _teamPlayCoordinator.GetSetTarget(_teamMember);
            return CalculateVelocityThroughApex(
                ball.transform.position,
                coordinatedTarget);
        }

        if (_spikePreparationTarget == null)
        {
            Vector3 fallbackDirection =
                Vector3.up * _verticalComponent + transform.forward * _forwardComponent;
            return fallbackDirection.normalized * (_setForce / ball.Mass);
        }

        float netZ = _netReference != null ? _netReference.position.z : 0f;
        float sideDirection = _courtSide == CourtSide.Player ? -1f : 1f;
        Vector3 target = new Vector3(
            Mathf.Clamp(
                transform.position.x + transform.forward.x * _lateralTargetInfluence,
                -3.9f,
                3.9f),
            _setTargetHeight,
            netZ + sideDirection * _attackDistanceFromNet);
        LastTarget = target;
        LastDistanceToNet = Mathf.Abs(target.z - netZ);
        _spikePreparationTarget.position = target;

        return CalculateVelocityThroughApex(ball.transform.position, target);
    }

    private Vector3 CalculateVelocityThroughApex(Vector3 origin, Vector3 target)
    {
        float gravity = -Physics.gravity.y;
        float apexHeight = Mathf.Max(
            _setApexHeight,
            origin.y + 0.4f,
            target.y + 0.4f);
        float upwardSpeed = Mathf.Sqrt(
            2f * gravity * Mathf.Max(0f, apexHeight - origin.y));
        float riseTime = upwardSpeed / gravity;
        float fallTime = Mathf.Sqrt(
            2f * Mathf.Max(0f, apexHeight - target.y) / gravity);
        float flightTime = Mathf.Max(0.35f, riseTime + fallTime);
        Vector3 velocity = (target - origin) / flightTime;
        velocity.y = upwardSpeed;
        return velocity;
    }

    private void LogRejected()
    {
        bool cooldownReady = _contactZone.ContactCooldownRemaining <= 0f;
        Debug.Log(
            $"SET REJECTED | Zone={_lastInsideZone} | Height={_lastHeightValid} | " +
            $"Angle={_lastAngleValid} | Cooldown={cooldownReady} | Buffer=False",
            this);
    }

    private void OnDrawGizmosSelected()
    {
        _contactZone?.DrawHeightRange(
            _minimumContactHeight,
            _maximumContactHeight,
            Color.cyan);
    }
}
