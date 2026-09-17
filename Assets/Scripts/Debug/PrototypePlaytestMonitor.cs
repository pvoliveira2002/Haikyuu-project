using UnityEngine;

public sealed class PrototypePlaytestMonitor : MonoBehaviour
{
    [SerializeField] private RallyEndDetector _rallyEndDetector;
    [SerializeField] private VolleyballBall _ball;
    [SerializeField, Range(0.5f, 1f)] private float _speedWarningRatio = 0.95f;
    [SerializeField, Min(0f)] private float _duplicateContactThreshold = 0.08f;

    private float _lastContactTime = float.NegativeInfinity;
    private CourtSide _lastContactSide;
    private bool _hasLastContact;
    private bool _speedWarningIssued;

    public int RallyTouches { get; private set; }
    public int LongestRally { get; private set; }
    public string LastAction { get; private set; } = "None";
    public RallyEndReason LastRallyEndReason { get; private set; }

    private void OnEnable()
    {
        ActionFeedbackController.FeedbackPlayed += HandleFeedback;

        if (_rallyEndDetector != null)
        {
            _rallyEndDetector.RallyEnded += HandleRallyEnded;
            _rallyEndDetector.RallyReset += HandleRallyReset;
        }
    }

    private void OnDisable()
    {
        ActionFeedbackController.FeedbackPlayed -= HandleFeedback;

        if (_rallyEndDetector != null)
        {
            _rallyEndDetector.RallyEnded -= HandleRallyEnded;
            _rallyEndDetector.RallyReset -= HandleRallyReset;
        }
    }

    private void Update()
    {
        if (_ball == null || _ball.MaximumSpeed <= 0f)
        {
            return;
        }

        float speed = _ball.Velocity.magnitude;
        if (!_speedWarningIssued && speed > _ball.MaximumSpeed * _speedWarningRatio)
        {
            Debug.LogWarning(
                $"PLAYTEST | Ball speed near limit: {speed:F1}/{_ball.MaximumSpeed:F1} m/s");
            _speedWarningIssued = true;
        }
        else if (speed < _ball.MaximumSpeed * 0.85f)
        {
            _speedWarningIssued = false;
        }
    }

    private void HandleFeedback(ActionFeedbackType type, Vector3 position)
    {
        CourtSide side = IsAIAction(type) ? CourtSide.Opponent : CourtSide.Player;
        if (_hasLastContact && side == _lastContactSide &&
            Time.time - _lastContactTime < _duplicateContactThreshold)
        {
            Debug.LogWarning(
                $"PLAYTEST | Rapid contacts: {side} in {Time.time - _lastContactTime:F3}s");
        }

        RallyTouches++;
        LastAction = type.ToString();
        _lastContactSide = side;
        _lastContactTime = Time.time;
        _hasLastContact = true;
    }

    private void HandleRallyEnded(CourtSide winner)
    {
        LongestRally = Mathf.Max(LongestRally, RallyTouches);
        LastRallyEndReason = _rallyEndDetector.EndReason;
        float speed = _ball != null ? _ball.Velocity.magnitude : 0f;
        Debug.Log(
            $"RALLY END | Winner: {winner} | Contacts: {RallyTouches} | " +
            $"Reason: {LastRallyEndReason} | Ball Speed: {speed:F1}");
    }

    private void HandleRallyReset()
    {
        RallyTouches = 0;
        _hasLastContact = false;
        LastAction = "None";
    }

    private static bool IsAIAction(ActionFeedbackType type)
    {
        return type == ActionFeedbackType.AIReceive ||
               type == ActionFeedbackType.AIAttack ||
               type == ActionFeedbackType.AIServe;
    }
}
