using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public sealed class AIOpponentActions : MonoBehaviour
{
    [SerializeField] private AIOpponentDecision _decision;
    [SerializeField] private RallyEndDetector _rallyEndDetector;
    [SerializeField] private VolleyballBall _ball;
    [SerializeField] private SphereCollider _contactZone;
    [SerializeField] private Transform _returnTarget;
    [SerializeField] private Transform _player;
    [SerializeField] private Transform _netTopReference;
    [SerializeField] private BallTouchTracker _touchTracker;
    [SerializeField] private AIOpponentController _controller;
    [SerializeField] private TeamMember _teamMember;
    [SerializeField] private BallResponsibilityResolver _responsibilityResolver;
    [SerializeField, Min(0f)] private float _receiveMinimumHeight = 0.25f;
    [SerializeField, Min(0f)] private float _receiveMaximumHeight = 2f;
    [SerializeField, Range(-1f, 1f)] private float _receiveMinimumForwardDot = -0.35f;
    [SerializeField, Min(0f)] private float _emergencyContactRadius = 0.85f;
    [SerializeField, Min(0f)] private float _emergencyCloseDistance = 0.65f;
    [SerializeField, Min(0f)] private float _attackForce = 2.1f;
    [SerializeField, Min(0f)] private float _attackVerticalBias = 0.35f;
    [SerializeField, Min(0f)] private float _attackDownwardBias = 0.05f;
    [SerializeField, Min(0f)] private float _netClearance = 0.6f;
    [SerializeField, Min(0f)] private float _contactCooldown = 0.35f;
    [SerializeField, Min(0f)] private float _targetVariation = 0.75f;
    [SerializeField] private Vector2 _safeReturnX = new Vector2(-3f, 3f);
    [SerializeField] private Vector2 _safeReturnZ = new Vector2(-7f, -3.5f);
    [SerializeField, Min(0.1f)] private float _minimumReceiveFlightTime = 1.2f;
    [SerializeField, Min(0.1f)] private float _maximumReceiveFlightTime = 2f;
    [SerializeField, Min(0.1f)] private float _playerMoveSpeed = 5f;
    [SerializeField, Min(0f)] private float _playerReactionAllowance = 0.2f;
    [SerializeField, Min(0f)] private float _landingHeight = 0.21f;

    private float _nextContactTime;
    private bool _rallyActive = true;
    private bool _ballInsideContactZone;
    private bool _contactConsumed;
    private Vector3 _currentContactTarget;
    private Vector3 _previousBallPosition;
    private bool _hasPreviousBallPosition;
    private bool _rejectionLoggedForApproach;

    public bool IsBallInsideContactZone => _ballInsideContactZone;
    public float CurrentBallDistance { get; private set; }
    public bool IsHeightValid { get; private set; }
    public bool IsAngleValid { get; private set; }
    public bool IsCooldownReady => Time.time >= _nextContactTime;
    public bool IsEmergencyFallbackActive { get; private set; }

    private void Awake()
    {
        if (_contactZone == null)
        {
            _contactZone = GetComponent<SphereCollider>();
        }

        _contactZone.isTrigger = true;
    }

    private void Update()
    {
        if (_ball == null || _contactZone == null)
        {
            return;
        }

        EvaluatePhysicalContact(out bool physicallyReachable, out bool sweptContact);
        if (physicallyReachable)
        {
            TryExecuteAction(sweptContact);
        }
        else if (CurrentBallDistance > GetContactRadius() + 0.25f)
        {
            _contactConsumed = false;
            _rejectionLoggedForApproach = false;
        }

        _previousBallPosition = _ball.transform.position;
        _hasPreviousBallPosition = true;
    }

    private void OnEnable()
    {
        if (_rallyEndDetector != null)
        {
            _rallyEndDetector.RallyEnded += HandleRallyEnded;
            _rallyEndDetector.RallyReset += HandleRallyReset;
            _rallyActive = !_rallyEndDetector.IsRallyEnded;
        }
    }

    private void OnDisable()
    {
        if (_rallyEndDetector != null)
        {
            _rallyEndDetector.RallyEnded -= HandleRallyEnded;
            _rallyEndDetector.RallyReset -= HandleRallyReset;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsTrackedBall(other))
        {
            return;
        }

        _ballInsideContactZone = true;
        _contactConsumed = false;
        EvaluatePhysicalContact(out _, out bool sweptContact);
        TryExecuteAction(sweptContact);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsTrackedBall(other))
        {
            return;
        }

        _ballInsideContactZone = true;
        EvaluatePhysicalContact(out _, out bool sweptContact);
        TryExecuteAction(sweptContact);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsTrackedBall(other))
        {
            return;
        }

        _ballInsideContactZone = false;
    }

    private void TryExecuteAction(bool sweptContact)
    {
        if (_decision == null || _ball == null || _returnTarget == null)
        {
            return;
        }

        if (_teamMember != null &&
            _responsibilityResolver != null &&
            !_responsibilityResolver.IsResponsible(_teamMember))
        {
            return;
        }

        bool defensiveBall = IsDefensiveBall();
        bool physicallyReachable = _ballInsideContactZone ||
            IsEmergencyFallbackActive || sweptContact;
        bool closeContact = CurrentBallDistance <= _emergencyCloseDistance;
        bool angleAccepted = IsAngleValid || closeContact;
        string rejectionReason = GetRejectionReason(
            defensiveBall,
            physicallyReachable,
            angleAccepted);
        if (rejectionReason != null)
        {
            LogRejectedOnce(rejectionReason);
            return;
        }

        if (defensiveBall)
        {
            if (ExecuteReceive())
            {
                Debug.Log(
                    $"AI CONTACT ACCEPTED | Distance={CurrentBallDistance:F2} | " +
                    $"Trigger={_ballInsideContactZone} | " +
                    $"EmergencyFallback={IsEmergencyFallbackActive || sweptContact} | " +
                    $"Decision={_decision.CurrentAction}",
                    this);
            }
        }
        else if (_decision.CurrentAction == AIAction.Attack)
        {
            ExecuteAttack();
        }
        else if (_decision.CurrentAction == AIAction.Receive ||
                 IsDefensiveFallbackValid())
        {
            ExecuteReceive();
        }
    }

    private void EvaluatePhysicalContact(
        out bool physicallyReachable,
        out bool sweptContact)
    {
        Vector3 contactCenter = GetContactCenter();
        Vector3 ballPosition = _ball.transform.position;
        CurrentBallDistance = Vector3.Distance(contactCenter, ballPosition);
        float relativeHeight = ballPosition.y - transform.position.y + 1f;
        IsHeightValid = relativeHeight >= _receiveMinimumHeight &&
            relativeHeight <= _receiveMaximumHeight;

        Vector3 horizontalOffset = Vector3.ProjectOnPlane(
            ballPosition - transform.position,
            Vector3.up);
        IsAngleValid = horizontalOffset.sqrMagnitude <= 0.0001f ||
            Vector3.Dot(transform.forward, horizontalOffset.normalized) >=
            _receiveMinimumForwardDot;
        IsEmergencyFallbackActive = CurrentBallDistance <= _emergencyContactRadius;
        sweptContact = _hasPreviousBallPosition &&
            DistanceToSegment(
                contactCenter,
                _previousBallPosition,
                ballPosition) <= _emergencyContactRadius;
        physicallyReachable = _ballInsideContactZone ||
            IsEmergencyFallbackActive || sweptContact;
    }

    private string GetRejectionReason(
        bool defensiveBall,
        bool physicallyReachable,
        bool angleAccepted)
    {
        if (!_rallyActive) return "RallyInactive";
        if (!defensiveBall) return "NotIncoming";
        if (!physicallyReachable) return "OutOfReach";
        if (!IsHeightValid) return "Height";
        if (!angleAccepted) return "Angle";
        if (!IsCooldownReady) return "Cooldown";
        if (_contactConsumed) return "Consumed";
        return null;
    }

    private bool IsDefensiveBall()
    {
        CourtSide side = _teamMember != null
            ? _teamMember.TeamSide
            : CourtSide.Opponent;
        return side == CourtSide.Player
            ? _ball.Velocity.z < 0f || _ball.transform.position.z <= 0f
            : _ball.Velocity.z > 0f || _ball.transform.position.z >= 0f;
    }

    private void LogRejectedOnce(string reason)
    {
        if (_rejectionLoggedForApproach || CurrentBallDistance > GetContactRadius() + 0.25f)
        {
            return;
        }

        _rejectionLoggedForApproach = true;
        Debug.Log(
            $"AI CONTACT REJECTED | Distance={CurrentBallDistance:F2} | " +
            $"InsideTrigger={_ballInsideContactZone} | " +
            $"EmergencyRange={IsEmergencyFallbackActive} | " +
            $"HeightValid={IsHeightValid} | AngleValid={IsAngleValid} | " +
            $"CooldownReady={IsCooldownReady} | RallyActive={_rallyActive} | " +
            $"Decision={_decision.CurrentAction} | " +
            $"CanReach={(_controller != null && _controller.CanReachCurrentTarget)} | " +
            $"Reason={reason}",
            this);
    }

    private Vector3 GetContactCenter()
    {
        return _contactZone.transform.TransformPoint(_contactZone.center);
    }

    private float GetContactRadius()
    {
        return _contactZone.radius * Mathf.Max(
            _contactZone.transform.lossyScale.x,
            _contactZone.transform.lossyScale.y,
            _contactZone.transform.lossyScale.z);
    }

    private static float DistanceToSegment(
        Vector3 point,
        Vector3 segmentStart,
        Vector3 segmentEnd)
    {
        Vector3 segment = segmentEnd - segmentStart;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= 0.0001f)
        {
            return Vector3.Distance(point, segmentStart);
        }

        float t = Mathf.Clamp01(
            Vector3.Dot(point - segmentStart, segment) / lengthSquared);
        return Vector3.Distance(point, segmentStart + segment * t);
    }

    private bool ExecuteReceive()
    {
        SelectContactTarget();
        if (!TryBuildPlayableReceive(
                _currentContactTarget,
                out Vector3 direction,
                out float force,
                out float flightTime))
        {
            return false;
        }

        ApplyBallAction(
            direction,
            force,
            "AI Receive",
            _currentContactTarget,
            flightTime);
        return true;
    }

    private void ExecuteAttack()
    {
        Vector3 direction = GetHorizontalDirectionToTarget();
        if (direction.sqrMagnitude <= 0f)
        {
            return;
        }

        Vector3 attackDirection = BuildArcDirection(
            direction,
            _attackVerticalBias,
            _attackDownwardBias,
            _attackForce,
            out float adjustedForce);

        ApplyBallAction(
            attackDirection,
            adjustedForce,
            "AI Attack",
            _currentContactTarget,
            -1f);
    }

    private Vector3 GetHorizontalDirectionToTarget()
    {
        SelectContactTarget();
        Vector3 direction = _currentContactTarget - _ball.transform.position;
        direction.y = 0f;
        return direction.normalized;
    }

    private void SelectContactTarget()
    {
        Vector2 variation = Random.insideUnitCircle * _targetVariation;
        _currentContactTarget = _returnTarget.position +
            new Vector3(variation.x, 0f, variation.y);
        _currentContactTarget.x = Mathf.Clamp(
            _currentContactTarget.x,
            Mathf.Min(_safeReturnX.x, _safeReturnX.y),
            Mathf.Max(_safeReturnX.x, _safeReturnX.y));
        _currentContactTarget.z = Mathf.Clamp(
            _currentContactTarget.z,
            Mathf.Min(_safeReturnZ.x, _safeReturnZ.y),
            Mathf.Max(_safeReturnZ.x, _safeReturnZ.y));
        _currentContactTarget.y = _landingHeight;
    }

    private bool TryBuildPlayableReceive(
        Vector3 target,
        out Vector3 direction,
        out float force,
        out float flightTime)
    {
        Vector3 start = _ball.transform.position;
        float playerDistance = _player != null
            ? Vector3.Distance(
                new Vector3(_player.position.x, 0f, _player.position.z),
                new Vector3(target.x, 0f, target.z))
            : 0f;
        float playerTravelTime = _playerMoveSpeed > 0f
            ? playerDistance / _playerMoveSpeed
            : _maximumReceiveFlightTime;
        float minimumTime = Mathf.Min(
            _minimumReceiveFlightTime,
            _maximumReceiveFlightTime);
        float maximumTime = Mathf.Max(
            _minimumReceiveFlightTime,
            _maximumReceiveFlightTime);
        float desiredTime = Mathf.Clamp(
            playerTravelTime + _playerReactionAllowance,
            minimumTime,
            maximumTime);

        Vector3 launchVelocity = Vector3.zero;
        flightTime = desiredTime;
        bool clearsNet = false;
        for (float candidate = desiredTime;
             candidate <= maximumTime + 0.001f;
             candidate += 0.05f)
        {
            launchVelocity = CalculateLaunchVelocity(start, target, candidate);
            flightTime = candidate;
            if (ClearsNet(start, target, launchVelocity, candidate))
            {
                clearsNet = true;
                break;
            }
        }

        if (!clearsNet || launchVelocity.sqrMagnitude <= 0.0001f)
        {
            direction = Vector3.zero;
            force = 0f;
            return false;
        }

        direction = launchVelocity.normalized;
        force = launchVelocity.magnitude * _ball.Mass;
        return true;
    }

    private static Vector3 CalculateLaunchVelocity(
        Vector3 start,
        Vector3 target,
        float flightTime)
    {
        Vector3 displacement = target - start;
        Vector3 horizontalVelocity =
            new Vector3(displacement.x, 0f, displacement.z) / flightTime;
        float verticalVelocity =
            (displacement.y - 0.5f * Physics.gravity.y * flightTime * flightTime) /
            flightTime;
        return horizontalVelocity + Vector3.up * verticalVelocity;
    }

    private bool ClearsNet(
        Vector3 start,
        Vector3 target,
        Vector3 launchVelocity,
        float flightTime)
    {
        if (_netTopReference == null)
        {
            return true;
        }

        float zDistance = target.z - start.z;
        if (Mathf.Abs(zDistance) <= 0.001f)
        {
            return false;
        }

        float fraction =
            (_netTopReference.position.z - start.z) / zDistance;
        if (fraction <= 0f || fraction >= 1f)
        {
            return true;
        }

        float timeAtNet = fraction * flightTime;
        float heightAtNet = start.y +
            launchVelocity.y * timeAtNet +
            0.5f * Physics.gravity.y * timeAtNet * timeAtNet;
        return heightAtNet >= _netTopReference.position.y + _netClearance;
    }

    private bool IsDefensiveFallbackValid()
    {
        return _decision.CurrentAction == AIAction.PrepareReceive &&
               _ball.Velocity.y <= 0f;
    }

    private Vector3 BuildArcDirection(
        Vector3 horizontalDirection,
        float verticalBias,
        float downwardBias,
        float baseForce,
        out float adjustedForce)
    {
        float requiredVerticalRatio = 0f;
        adjustedForce = baseForce;

        if (_netTopReference != null && _ball.Mass > 0f)
        {
            float distanceToNet = Mathf.Max(
                Mathf.Abs(
                    (_netTopReference.position.z -
                     _ball.transform.position.z) /
                    Mathf.Max(Mathf.Abs(horizontalDirection.z), 0.01f)),
                0.01f);
            float desiredNetHeight = _netTopReference.position.y + _netClearance;
            float requiredRise = Mathf.Max(
                0f,
                desiredNetHeight - _ball.transform.position.y);
            float directDistance = Mathf.Sqrt(
                distanceToNet * distanceToNet + requiredRise * requiredRise);

            float minimumSpeed = Mathf.Sqrt(
                Mathf.Abs(Physics.gravity.y) *
                (requiredRise + directDistance));
            float minimumForce = minimumSpeed * _ball.Mass * 1.05f;
            adjustedForce = Mathf.Max(baseForce, minimumForce);

            float launchSpeed = adjustedForce / _ball.Mass;
            float gravityTerm =
                0.5f * Mathf.Abs(Physics.gravity.y) *
                distanceToNet * distanceToNet /
                (launchSpeed * launchSpeed);
            float discriminant =
                distanceToNet * distanceToNet -
                4f * gravityTerm * (requiredRise + gravityTerm);

            if (discriminant >= 0f)
            {
                requiredVerticalRatio =
                    (distanceToNet - Mathf.Sqrt(discriminant)) /
                    (2f * gravityTerm);
            }
        }

        float verticalComponent =
            Mathf.Max(verticalBias - downwardBias, requiredVerticalRatio);

        return (
            horizontalDirection + Vector3.up * verticalComponent).normalized;
    }

    private void ApplyBallAction(
        Vector3 direction,
        float force,
        string actionName,
        Vector3 intendedLanding,
        float intendedFlightTime)
    {
        _ball.ResetVelocity();
        _ball.ApplyImpulse(direction, force);
        ActionFeedbackController.PlayFeedback(
            actionName == "AI Attack"
                ? ActionFeedbackType.AIAttack
                : ActionFeedbackType.AIReceive,
            _ball.transform.position);
        _touchTracker?.RegisterTouch(
            _teamMember != null
                ? _teamMember.TeamSide
                : CourtSide.Opponent);
        _nextContactTime = Time.time + _contactCooldown;
        _contactConsumed = true;
        _rejectionLoggedForApproach = false;
        _decision.NotifyActionCompleted();

        if (intendedFlightTime <= 0f &&
            TryPredictLanding(out Vector3 predictedLanding, out float predictedTime))
        {
            intendedLanding = predictedLanding;
            intendedFlightTime = predictedTime;
        }

        float playerDistance = _player != null
            ? Vector3.Distance(
                new Vector3(_player.position.x, 0f, _player.position.z),
                new Vector3(intendedLanding.x, 0f, intendedLanding.z))
            : -1f;
        float playerTravelTime = _playerMoveSpeed > 0f && playerDistance >= 0f
            ? playerDistance / _playerMoveSpeed
            : -1f;
        bool reachable = intendedFlightTime > 0f &&
            playerTravelTime >= 0f &&
            playerTravelTime <= intendedFlightTime;
        Debug.Log(
            $"AI RETURN | Action: {actionName} | BallSpeed: {_ball.Velocity.magnitude:F2} | " +
            $"PredictedLanding: {intendedLanding} | TimeToLanding: {intendedFlightTime:F2} | " +
            $"PlayerDistanceToLanding: {playerDistance:F2} | " +
            $"EstimatedPlayerTravelTime: {playerTravelTime:F2} | Reachable: {reachable}");
    }

    private bool TryPredictLanding(
        out Vector3 landing,
        out float flightTime)
    {
        Vector3 start = _ball.transform.position;
        Vector3 velocity = _ball.Velocity;
        float a = 0.5f * Physics.gravity.y;
        float b = velocity.y;
        float c = start.y - _landingHeight;
        float discriminant = b * b - 4f * a * c;
        if (discriminant < 0f || Mathf.Abs(a) <= 0.0001f)
        {
            landing = Vector3.zero;
            flightTime = -1f;
            return false;
        }

        float root = Mathf.Sqrt(discriminant);
        float first = (-b + root) / (2f * a);
        float second = (-b - root) / (2f * a);
        flightTime = Mathf.Max(first, second);
        if (flightTime <= 0f)
        {
            landing = Vector3.zero;
            return false;
        }

        landing = start + velocity * flightTime +
            0.5f * Physics.gravity * flightTime * flightTime;
        landing.y = _landingHeight;
        return true;
    }

    private bool IsTrackedBall(Collider other)
    {
        return _ball != null &&
               other.TryGetComponent(out VolleyballBall detectedBall) &&
               detectedBall == _ball;
    }

    private void HandleRallyEnded(CourtSide winner)
    {
        _rallyActive = false;
        _contactConsumed = true;
    }

    private void HandleRallyReset()
    {
        _rallyActive = true;
        _ballInsideContactZone = false;
        _contactConsumed = false;
        _nextContactTime = 0f;
        _hasPreviousBallPosition = false;
        _rejectionLoggedForApproach = false;
        IsEmergencyFallbackActive = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (_returnTarget != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_returnTarget.position, 0.25f);
        }

        if (_currentContactTarget != Vector3.zero)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(_currentContactTarget, 0.2f);
        }

        if (_netTopReference != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(
                _netTopReference.position + Vector3.up * _netClearance,
                0.2f);
        }
    }
}
