using System.Collections;
using UnityEngine;

public sealed class ServePossessionController : MonoBehaviour
{
    [SerializeField] private RallyEndDetector _rallyEndDetector;
    [SerializeField] private MatchSetManager _matchSetManager;
    [SerializeField] private VolleyballBall _ball;
    [SerializeField] private BallTouchTracker _touchTracker;
    [SerializeField] private TeamMember _playerServer;
    [SerializeField] private TeamMember _opponentServer;
    [SerializeField] private ServeSystem _playerServeSystem;
    [SerializeField] private Transform _playerServePoint;
    [SerializeField] private Transform _opponentServePoint;
    [SerializeField] private Transform _opponentServeTarget;
    [SerializeField, Min(0f)] private float _opponentServeDelay = 0.75f;
    [SerializeField, Min(0f)] private float _opponentServeForce = 3.3f;
    [SerializeField, Min(0f)] private float _opponentServeVerticalBias = 0.7f;

    private Coroutine _opponentServeRoutine;

    public CourtSide CurrentServer { get; private set; } = CourtSide.Player;
    public bool CanPlayerServe { get; private set; } = true;
    public Vector3 NextServePosition => CurrentServer == CourtSide.Player
        ? _playerServePoint.position
        : _opponentServePoint.position;

    private void OnEnable()
    {
        if (_rallyEndDetector != null)
        {
            _rallyEndDetector.RallyEnded += HandleRallyEnded;
        }
    }

    private void OnDisable()
    {
        if (_rallyEndDetector != null)
        {
            _rallyEndDetector.RallyEnded -= HandleRallyEnded;
        }

        if (_opponentServeRoutine != null)
        {
            StopCoroutine(_opponentServeRoutine);
        }
    }

    public void PrepareNextServe()
    {
        CanPlayerServe = false;

        if (_matchSetManager != null && _matchSetManager.MatchOver)
        {
            return;
        }

        if (CurrentServer == CourtSide.Player)
        {
            _playerServeSystem?.RearmServe();
            CanPlayerServe = true;
            return;
        }

        _opponentServeRoutine = StartCoroutine(PerformOpponentServe());
    }

    public void NotifyPlayerServed()
    {
        CanPlayerServe = false;
        _touchTracker?.RegisterServe(CourtSide.Player, _playerServer);
    }

    private void HandleRallyEnded(CourtSide winner)
    {
        CanPlayerServe = false;

        if (_matchSetManager != null && _matchSetManager.MatchOver)
        {
            return;
        }

        CurrentServer = _matchSetManager != null && _matchSetManager.DidSetEndOnLastPoint
            ? _matchSetManager.FirstServerForCurrentSet
            : winner;

        Debug.Log($"Next Server: {CurrentServer}");
    }

    private IEnumerator PerformOpponentServe()
    {
        yield return new WaitForSeconds(_opponentServeDelay);

        if (_matchSetManager != null && _matchSetManager.MatchOver)
        {
            yield break;
        }

        _ball.ResetPosition(_opponentServePoint.position);

        Vector3 horizontalDirection = _opponentServeTarget.position - _ball.transform.position;
        horizontalDirection.y = 0f;
        Vector3 serveDirection = (
            horizontalDirection.normalized + Vector3.up * _opponentServeVerticalBias).normalized;

        _ball.ApplyImpulse(serveDirection, _opponentServeForce);
        ActionFeedbackController.PlayFeedback(
            ActionFeedbackType.AIServe,
            _ball.transform.position);
        _touchTracker?.RegisterServe(CourtSide.Opponent, _opponentServer);
        _opponentServeRoutine = null;
    }
}
