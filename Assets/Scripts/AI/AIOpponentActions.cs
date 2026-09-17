using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public sealed class AIOpponentActions : MonoBehaviour
{
    [SerializeField] private AIOpponentDecision _decision;
    [SerializeField] private RallyEndDetector _rallyEndDetector;
    [SerializeField] private VolleyballBall _ball;
    [SerializeField] private SphereCollider _contactZone;
    [SerializeField] private Transform _returnTarget;
    [SerializeField] private Transform _netTopReference;
    [SerializeField] private BallTouchTracker _touchTracker;
    [SerializeField, Min(0f)] private float _receiveForce = 1.6f;
    [SerializeField, Min(0f)] private float _receiveVerticalBias = 0.9f;
    [SerializeField, Min(0f)] private float _attackForce = 2.1f;
    [SerializeField, Min(0f)] private float _attackVerticalBias = 0.35f;
    [SerializeField, Min(0f)] private float _attackDownwardBias = 0.05f;
    [SerializeField, Min(0f)] private float _netClearance = 0.6f;
    [SerializeField, Min(0f)] private float _contactCooldown = 0.35f;
    [SerializeField, Min(0f)] private float _targetVariation = 0.75f;

    private float _nextContactTime;
    private bool _rallyActive = true;
    private bool _ballInsideContactZone;
    private bool _contactConsumed;
    private Vector3 _currentContactTarget;

    private void Awake()
    {
        if (_contactZone == null)
        {
            _contactZone = GetComponent<SphereCollider>();
        }

        _contactZone.isTrigger = true;
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
        TryExecuteAction();
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsTrackedBall(other))
        {
            return;
        }

        _ballInsideContactZone = true;
        TryExecuteAction();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsTrackedBall(other))
        {
            return;
        }

        _ballInsideContactZone = false;
        _contactConsumed = false;
    }

    private void TryExecuteAction()
    {
        if (!_rallyActive ||
            !_ballInsideContactZone ||
            _contactConsumed ||
            _decision == null ||
            _ball == null ||
            _returnTarget == null ||
            Time.time < _nextContactTime)
        {
            return;
        }

        if (_decision.CurrentAction == AIAction.Attack)
        {
            ExecuteAttack();
        }
        else if (_decision.CurrentAction == AIAction.Receive ||
                 IsDefensiveFallbackValid())
        {
            ExecuteReceive();
        }
    }

    private void ExecuteReceive()
    {
        Vector3 direction = GetHorizontalDirectionToTarget();
        if (direction.sqrMagnitude <= 0f)
        {
            return;
        }

        Vector3 receiveDirection = BuildArcDirection(
            direction,
            _receiveVerticalBias,
            0f,
            _receiveForce,
            out float adjustedForce);

        ApplyBallAction(
            receiveDirection,
            adjustedForce,
            "AI Receive");
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
            "AI Attack");
    }

    private Vector3 GetHorizontalDirectionToTarget()
    {
        Vector2 variation = Random.insideUnitCircle * _targetVariation;
        _currentContactTarget = _returnTarget.position +
            new Vector3(variation.x, 0f, variation.y);
        Vector3 direction = _currentContactTarget - _ball.transform.position;
        direction.y = 0f;
        return direction.normalized;
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

    private void ApplyBallAction(Vector3 direction, float force, string debugMessage)
    {
        _ball.ResetVelocity();
        _ball.ApplyImpulse(direction, force);
        ActionFeedbackController.PlayFeedback(
            debugMessage == "AI Attack"
                ? ActionFeedbackType.AIAttack
                : ActionFeedbackType.AIReceive,
            _ball.transform.position);
        _touchTracker?.RegisterTouch(CourtSide.Opponent);
        _nextContactTime = Time.time + _contactCooldown;
        _contactConsumed = true;
        _decision.NotifyActionCompleted();
        Debug.Log(debugMessage);
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
