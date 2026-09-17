using System.Collections;
using UnityEngine;

public sealed class RallyResetController : MonoBehaviour
{
    [SerializeField] private RallyEndDetector _rallyEndDetector;
    [SerializeField] private MatchSetManager _matchSetManager;
    [SerializeField] private ServePossessionController _servePossession;
    [SerializeField] private VolleyballBall _ball;
    [SerializeField] private BallTouchTracker _touchTracker;
    [SerializeField, Min(0f)] private float _rallyResetDelay = 1.5f;

    private Coroutine _resetRoutine;

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
    }

    private void HandleRallyEnded(CourtSide winner)
    {
        if (_resetRoutine == null)
        {
            _resetRoutine = StartCoroutine(ResetRallyAfterDelay());
        }
    }

    private IEnumerator ResetRallyAfterDelay()
    {
        yield return new WaitForSeconds(_rallyResetDelay);

        if (_matchSetManager != null && _matchSetManager.MatchOver)
        {
            _ball.ResetVelocity();
            _resetRoutine = null;
            yield break;
        }

        _ball.ResetPosition(_servePossession.NextServePosition);
        _touchTracker?.Clear();
        _rallyEndDetector.ResetRally();
        _servePossession.PrepareNextServe();
        _resetRoutine = null;
    }
}
