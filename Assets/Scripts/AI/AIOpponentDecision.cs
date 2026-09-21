using UnityEngine;

public enum AIAction
{
    None,
    Wait,
    ReturnHome,
    PrepareReceive,
    Receive,
    PrepareAttack,
    Attack
}

public sealed class AIOpponentDecision : MonoBehaviour
{
    [SerializeField] private VolleyballBall _ball;
    [SerializeField] private BallTrajectoryPredictor _trajectoryPredictor;
    [SerializeField] private RallyEndDetector _rallyEndDetector;
    [SerializeField] private MatchSetManager _matchSetManager;
    [SerializeField] private Collider _allowedCourtArea;
    [SerializeField, Min(0f)] private float _receiveMinHeight = 0.3f;
    [SerializeField, Min(0f)] private float _receiveMaxHeight = 1.9f;
    [SerializeField, Min(0f)] private float _receiveDistance = 1.2f;
    [SerializeField, Min(0f)] private float _attackMinHeight = 1.6f;
    [SerializeField, Min(0f)] private float _attackMaxHeight = 3f;
    [SerializeField, Min(0f)] private float _attackDistance = 1f;
    [SerializeField, Range(0f, 1f)] private float _attackChance = 0.35f;
    [SerializeField, Min(0f)] private float _reactionTime = 0.18f;
    [SerializeField, Min(0f)] private float _fastBallReactionTime = 0.1f;
    [SerializeField] private AIAction _currentAction = AIAction.Wait;

    private AIAction _pendingAction;
    private float _pendingActionTime;
    private bool _attackChoiceMade;
    private bool _attackSelected;

    public AIAction CurrentAction => _currentAction;
    public float CurrentReactionDelay =>
        _trajectoryPredictor != null && _trajectoryPredictor.IsFastIncomingBall
            ? _fastBallReactionTime
            : _reactionTime;

    private void OnEnable()
    {
        _pendingAction = _currentAction;

        if (_rallyEndDetector != null)
        {
            _rallyEndDetector.RallyEnded += HandleRallyEnded;
            _rallyEndDetector.RallyReset += HandleRallyReset;
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

    private void Update()
    {
        AIAction desiredAction = DetermineAction();

        if (desiredAction == _currentAction)
        {
            _pendingAction = _currentAction;
            return;
        }

        if (desiredAction != _pendingAction)
        {
            _pendingAction = desiredAction;
            _pendingActionTime = Time.time + CurrentReactionDelay;
            return;
        }

        if (Time.time < _pendingActionTime)
        {
            return;
        }

        _currentAction = _pendingAction;
        Debug.Log($"AI Decision: {_currentAction}");
    }

    private AIAction DetermineAction()
    {
        if ((_matchSetManager != null && _matchSetManager.MatchOver) ||
            (_rallyEndDetector != null && _rallyEndDetector.IsRallyEnded))
        {
            return AIAction.Wait;
        }

        if (_ball == null ||
            _trajectoryPredictor == null ||
            _allowedCourtArea == null ||
            !_trajectoryPredictor.HasPrediction ||
            !IsInsideAllowedArea(_trajectoryPredictor.PredictedLandingPoint))
        {
            return AIAction.ReturnHome;
        }

        Vector3 ballPosition = _ball.transform.position;
        Vector3 horizontalOffset = ballPosition - transform.position;
        horizontalOffset.y = 0f;
        float horizontalDistance = horizontalOffset.magnitude;
        bool ballOnAISide = IsInsideAllowedArea(ballPosition);
        float verticalSpeed = _ball.Velocity.y;

        if (_trajectoryPredictor.IsFastIncomingBall)
        {
            bool canReceiveNow = ballOnAISide &&
                IsWithinHeight(ballPosition.y, _receiveMinHeight, _receiveMaxHeight) &&
                horizontalDistance <= _receiveDistance &&
                verticalSpeed <= 0.5f;
            return canReceiveNow ? AIAction.Receive : AIAction.PrepareReceive;
        }

        bool attackOpportunity = ballOnAISide &&
            IsWithinHeight(ballPosition.y, _attackMinHeight, _attackMaxHeight) &&
            horizontalDistance <= _attackDistance &&
            verticalSpeed <= 1f;

        if (attackOpportunity && !_attackChoiceMade)
        {
            _attackChoiceMade = true;
            _attackSelected = Random.value <= _attackChance;
        }
        else if (!attackOpportunity &&
                 !IsWithinHeight(ballPosition.y, _attackMinHeight, _attackMaxHeight))
        {
            _attackChoiceMade = false;
        }

        if (attackOpportunity && _attackSelected)
        {
            return AIAction.Attack;
        }

        if (ballOnAISide &&
            IsWithinHeight(ballPosition.y, _attackMinHeight, _attackMaxHeight) &&
            horizontalDistance <= _attackDistance * 1.5f)
        {
            return AIAction.PrepareAttack;
        }

        if (ballOnAISide &&
            IsWithinHeight(ballPosition.y, _receiveMinHeight, _receiveMaxHeight) &&
            horizontalDistance <= _receiveDistance &&
            verticalSpeed <= 0.5f)
        {
            return AIAction.Receive;
        }

        return AIAction.PrepareReceive;
    }

    private bool IsInsideAllowedArea(Vector3 point)
    {
        Bounds bounds = _allowedCourtArea.bounds;
        return point.x >= bounds.min.x && point.x <= bounds.max.x &&
               point.z >= bounds.min.z && point.z <= bounds.max.z;
    }

    private static bool IsWithinHeight(float height, float minimum, float maximum)
    {
        return height >= minimum && height <= maximum;
    }

    private void HandleRallyEnded(CourtSide winner)
    {
        SetActionImmediately(AIAction.Wait);
    }

    private void HandleRallyReset()
    {
        SetActionImmediately(AIAction.ReturnHome);
    }

    private void SetActionImmediately(AIAction action)
    {
        _currentAction = action;
        _pendingAction = action;
        _pendingActionTime = 0f;
    }

    public void NotifyActionCompleted()
    {
        _attackChoiceMade = false;
        _attackSelected = false;
        SetActionImmediately(AIAction.ReturnHome);
    }
}
