using UnityEngine;

public sealed class AIOpponentController : MonoBehaviour
{
    [SerializeField] private BallTrajectoryPredictor _trajectoryPredictor;
    [SerializeField] private AIOpponentDecision _decision;
    [SerializeField] private RallyEndDetector _rallyEndDetector;
    [SerializeField] private Transform _homePosition;
    [SerializeField] private Collider _allowedCourtArea;
    [SerializeField] private CourtMovementBounds _movementBounds;
    [SerializeField] private CourtSide _courtSide = CourtSide.Opponent;
    [SerializeField, Min(0f)] private float _moveSpeed = 5.5f;
    [SerializeField, Min(0f)] private float _acceleration = 22f;
    [SerializeField, Min(0f)] private float _deceleration = 20f;
    [SerializeField, Min(0f)] private float _positionTolerance = 0.3f;
    [SerializeField, Min(0f)] private float _urgentPositionTolerance = 0.4f;
    [SerializeField, Min(1f)] private float _reachabilityMargin = 1.1f;
    [SerializeField, Min(0f)] private float _positioningOffset = 0.8f;
    [SerializeField, Min(0f)] private float _courtPadding = 0.5f;

    private Vector3 _movementTarget;
    private Vector3 _horizontalVelocity;
    private bool _wasFastIncoming;

    public bool CanReachCurrentTarget { get; private set; }
    public float DistanceToLanding { get; private set; }
    public float EstimatedTravelTime { get; private set; }
    public string DefensiveUrgency { get; private set; } = "Normal";
    public Vector3 MovementTarget => _movementTarget;

    private void Update()
    {
        if (_trajectoryPredictor == null ||
            _homePosition == null ||
            _allowedCourtArea == null)
        {
            return;
        }

        _movementTarget = ChooseMovementTarget();
        UpdateDefensiveUrgency();
        UpdateReachability();
        LogFastDefenseTransition();
        MoveTowardsTarget();
    }

    private Vector3 ChooseMovementTarget()
    {
        Vector3 target = _homePosition.position;

        if (ShouldUsePredictedPosition() &&
            (_rallyEndDetector == null || !_rallyEndDetector.IsRallyEnded) &&
            _trajectoryPredictor.HasPrediction &&
            IsInsideAllowedArea(_trajectoryPredictor.PredictedLandingPoint))
        {
            Vector3 landingPoint = _trajectoryPredictor.PredictedLandingPoint;
            Vector3 preparationDirection = _homePosition.position - landingPoint;
            preparationDirection.y = 0f;

            if (preparationDirection.sqrMagnitude > 0f)
            {
                target = landingPoint +
                    preparationDirection.normalized * _positioningOffset;
            }
            else
            {
                target = landingPoint;
            }
        }

        return ClampToAllowedArea(target);
    }

    private bool ShouldUsePredictedPosition()
    {
        if (_decision == null)
        {
            return true;
        }

        return _trajectoryPredictor.IsFastIncomingBall ||
               _decision.CurrentAction == AIAction.PrepareReceive ||
               _decision.CurrentAction == AIAction.Receive ||
               _decision.CurrentAction == AIAction.PrepareAttack ||
               _decision.CurrentAction == AIAction.Attack;
    }

    private void MoveTowardsTarget()
    {
        Vector3 currentPosition = transform.position;
        _movementTarget.y = currentPosition.y;
        Vector3 movement = _movementTarget - currentPosition;
        float distance = movement.magnitude;
        float activeTolerance = _trajectoryPredictor.IsFastIncomingBall
            ? _urgentPositionTolerance
            : _positionTolerance;
        float stoppingSpeed = Mathf.Sqrt(
            2f * _deceleration * Mathf.Max(distance - activeTolerance, 0f));
        float desiredSpeed = Mathf.Min(_moveSpeed, stoppingSpeed);
        Vector3 targetVelocity = distance > activeTolerance
            ? movement.normalized * desiredSpeed
            : Vector3.zero;
        float changeRate = targetVelocity.sqrMagnitude > 0f
            ? _acceleration
            : _deceleration;

        _horizontalVelocity = Vector3.MoveTowards(
            _horizontalVelocity,
            targetVelocity,
            changeRate * Time.deltaTime);
        transform.position = ClampToAllowedArea(
            currentPosition + _horizontalVelocity * Time.deltaTime);

        if (_horizontalVelocity.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(
                _horizontalVelocity.normalized,
                Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                1f - Mathf.Exp(-12f * Time.deltaTime));
        }
    }

    private void UpdateReachability()
    {
        if (!_trajectoryPredictor.HasPrediction ||
            _trajectoryPredictor.TimeToLanding <= 0f)
        {
            CanReachCurrentTarget = false;
            DistanceToLanding = 0f;
            EstimatedTravelTime = 0f;
            return;
        }

        Vector3 offset = _movementTarget - transform.position;
        offset.y = 0f;
        DistanceToLanding = offset.magnitude;
        EstimatedTravelTime = DistanceToLanding / Mathf.Max(_moveSpeed, 0.01f);
        CanReachCurrentTarget =
            EstimatedTravelTime <= _trajectoryPredictor.TimeToLanding * _reachabilityMargin;
    }

    private void UpdateDefensiveUrgency()
    {
        if (!_trajectoryPredictor.HasPrediction)
        {
            DefensiveUrgency = "Normal";
            return;
        }

        float landingTime = _trajectoryPredictor.TimeToLanding;
        DefensiveUrgency = landingTime > 1.2f
            ? "Normal"
            : landingTime >= 0.7f ? "Urgent" : "Maximum";
    }

    private void LogFastDefenseTransition()
    {
        bool fastIncoming = _trajectoryPredictor.IsFastIncomingBall;
        if (fastIncoming && !_wasFastIncoming)
        {
            Debug.Log(
                $"AI DEFENSE | Speed={_trajectoryPredictor.IncomingSpeed:F2} | " +
                $"TimeToLanding={_trajectoryPredictor.TimeToLanding:F2} | " +
                $"Distance={DistanceToLanding:F2} | " +
                $"TravelTime={EstimatedTravelTime:F2} | " +
                $"Reachable={CanReachCurrentTarget}",
                this);
        }

        _wasFastIncoming = fastIncoming;
    }

    private bool IsInsideAllowedArea(Vector3 point)
    {
        Bounds bounds = _allowedCourtArea.bounds;
        bool insideLegacyArea = point.x >= bounds.min.x && point.x <= bounds.max.x &&
            point.z >= bounds.min.z && point.z <= bounds.max.z;
        return insideLegacyArea &&
            (_movementBounds == null || _movementBounds.Contains(point, _courtSide));
    }

    private Vector3 ClampToAllowedArea(Vector3 point)
    {
        Bounds bounds = _allowedCourtArea.bounds;
        Vector3 clamped = new Vector3(
            Mathf.Clamp(point.x, bounds.min.x + _courtPadding, bounds.max.x - _courtPadding),
            transform.position.y,
            Mathf.Clamp(point.z, bounds.min.z + _courtPadding, bounds.max.z - _courtPadding));
        return _movementBounds != null
            ? _movementBounds.ClampPosition(clamped, _courtSide)
            : clamped;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(_movementTarget, 0.3f);

        if (_homePosition != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(_homePosition.position, 0.35f);
        }
    }
}
