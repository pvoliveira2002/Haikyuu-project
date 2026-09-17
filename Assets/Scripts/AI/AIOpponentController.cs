using UnityEngine;

public sealed class AIOpponentController : MonoBehaviour
{
    [SerializeField] private BallTrajectoryPredictor _trajectoryPredictor;
    [SerializeField] private AIOpponentDecision _decision;
    [SerializeField] private RallyEndDetector _rallyEndDetector;
    [SerializeField] private Transform _homePosition;
    [SerializeField] private Collider _allowedCourtArea;
    [SerializeField, Min(0f)] private float _moveSpeed = 5f;
    [SerializeField, Min(0f)] private float _acceleration = 16f;
    [SerializeField, Min(0f)] private float _deceleration = 20f;
    [SerializeField, Min(0f)] private float _positionTolerance = 0.3f;
    [SerializeField, Min(0f)] private float _positioningOffset = 0.8f;
    [SerializeField, Min(0f)] private float _courtPadding = 0.5f;

    private Vector3 _movementTarget;
    private Vector3 _horizontalVelocity;

    public bool CanReachCurrentTarget { get; private set; }

    private void Update()
    {
        if (_trajectoryPredictor == null ||
            _homePosition == null ||
            _allowedCourtArea == null)
        {
            return;
        }

        _movementTarget = ChooseMovementTarget();
        UpdateReachability();
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

        return _decision.CurrentAction == AIAction.PrepareReceive ||
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
        float stoppingSpeed = Mathf.Sqrt(
            2f * _deceleration * Mathf.Max(distance - _positionTolerance, 0f));
        float desiredSpeed = Mathf.Min(_moveSpeed, stoppingSpeed);
        Vector3 targetVelocity = distance > _positionTolerance
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
            return;
        }

        Vector3 offset = _movementTarget - transform.position;
        offset.y = 0f;
        float requiredTime = offset.magnitude / Mathf.Max(_moveSpeed, 0.01f);
        CanReachCurrentTarget = requiredTime <= _trajectoryPredictor.TimeToLanding;
    }

    private bool IsInsideAllowedArea(Vector3 point)
    {
        Bounds bounds = _allowedCourtArea.bounds;
        return point.x >= bounds.min.x && point.x <= bounds.max.x &&
               point.z >= bounds.min.z && point.z <= bounds.max.z;
    }

    private Vector3 ClampToAllowedArea(Vector3 point)
    {
        Bounds bounds = _allowedCourtArea.bounds;
        return new Vector3(
            Mathf.Clamp(point.x, bounds.min.x + _courtPadding, bounds.max.x - _courtPadding),
            transform.position.y,
            Mathf.Clamp(point.z, bounds.min.z + _courtPadding, bounds.max.z - _courtPadding));
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
