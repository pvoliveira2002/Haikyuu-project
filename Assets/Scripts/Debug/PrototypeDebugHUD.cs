using UnityEngine;

public sealed class PrototypeDebugHUD : MonoBehaviour
{
    [SerializeField] private ScoreManager _scoreManager;
    [SerializeField] private MatchSetManager _matchSetManager;
    [SerializeField] private ServePossessionController _servePossession;
    [SerializeField] private RallyEndDetector _rallyEndDetector;
    [SerializeField] private PrototypePlaytestMonitor _playtestMonitor;
    [SerializeField] private CharacterController _playerController;
    [SerializeField] private PlayerJump _playerJump;
    [SerializeField] private VolleyballBall _ball;
    [SerializeField] private BallTouchTracker _touchTracker;
    [SerializeField] private Transform _aiOpponent;
    [SerializeField] private AIOpponentDecision _aiDecision;
    [SerializeField] private AIOpponentController _aiController;
    [SerializeField] private BallTrajectoryPredictor _trajectoryPredictor;
    [SerializeField] private VolleyballCameraController _cameraController;
    [SerializeField] private float _aiReactionTime = 0.18f;
    [SerializeField] private bool _visible;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            _visible = !_visible;
        }
    }

    private void OnGUI()
    {
        if (!_visible || !HasRequiredReferences())
        {
            return;
        }

        Vector3 playerVelocity = _playerController.velocity;
        playerVelocity.y = 0f;
        float aiDistance = _aiOpponent != null
            ? Vector3.Distance(_aiOpponent.position, _ball.transform.position)
            : 0f;
        string lastTouch = _touchTracker.HasLastTouch
            ? _touchTracker.LastTouch.ToString()
            : "None";
        string timeToLanding = _trajectoryPredictor.HasPrediction
            ? $"{_trajectoryPredictor.TimeToLanding:F2}s"
            : "N/A";

        string text =
            $"PLAYTEST 1x1  (F1 hide)\n" +
            $"Match | Score {_scoreManager.PlayerScore}-{_scoreManager.OpponentScore}  " +
            $"Sets {_matchSetManager.PlayerSets}-{_matchSetManager.OpponentSets}  " +
            $"Server {_servePossession.CurrentServer}  Rally {!_rallyEndDetector.IsRallyEnded}\n" +
            $"Player | Grounded {_playerJump.IsGrounded}  Speed {playerVelocity.magnitude:F1}  " +
            $"Last {_playtestMonitor.LastAction}\n" +
            $"Ball | Speed {_ball.Velocity.magnitude:F1}  Height {_ball.transform.position.y:F1}  " +
            $"Last Touch {lastTouch}\n" +
            $"AI | {_aiDecision.CurrentAction}  Distance {aiDistance:F1}  " +
            $"Landing {timeToLanding}  Reach {_aiController.CanReachCurrentTarget}  " +
            $"Reaction {_aiReactionTime:F2}s\n" +
            $"Rally | Touches {_playtestMonitor.RallyTouches}  " +
            $"Longest {_playtestMonitor.LongestRally}  End {_playtestMonitor.LastRallyEndReason}\n" +
            $"CAMERA | Yaw {_cameraController.Yaw:F1}  Pitch {_cameraController.Pitch:F1}  " +
            $"Distance {_cameraController.Distance:F1}  BallAssist {_cameraController.BallAssistWeight:F2}  " +
            $"PlayerCameraRelativeMovement true";

        GUI.Box(new Rect(12f, 12f, 700f, 170f), text);
    }

    private bool HasRequiredReferences()
    {
        return _scoreManager != null &&
               _matchSetManager != null &&
               _servePossession != null &&
               _rallyEndDetector != null &&
               _playtestMonitor != null &&
               _playerController != null &&
               _playerJump != null &&
               _ball != null &&
               _touchTracker != null &&
               _aiDecision != null &&
               _aiController != null &&
               _trajectoryPredictor != null &&
               _cameraController != null;
    }
}
