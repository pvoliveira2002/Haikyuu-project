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
    [SerializeField] private AIOpponentActions _aiActions;
    [SerializeField] private BallTrajectoryPredictor _trajectoryPredictor;
    [SerializeField] private VolleyballCameraController _cameraController;
    [SerializeField] private ReceiveSystem _receiveSystem;
    [SerializeField] private SetSystem _setSystem;
    [SerializeField] private SpikeSystem _spikeSystem;
    [SerializeField] private CourtMovementBounds _movementBounds;
    [SerializeField] private PlayerAnimatedVisual _playerAnimatedVisual;
    [SerializeField] private BallResponsibilityResolver _responsibilityResolver;
    [SerializeField] private TeamPlayCoordinator _playerTeamCoordinator;
    [SerializeField] private TeamPlayCoordinator _opponentTeamCoordinator;
    [SerializeField] private TeamPositioningController _playerTeamPositioning;
    [SerializeField] private TeamPositioningController _opponentTeamPositioning;
    [SerializeField] private MatchCoinTossController _matchCoinToss;
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
            $"MATCH | State {_matchCoinToss.State}  Serving Team {_servePossession.CurrentServer}  " +
            $"Coin Toss {(_matchCoinToss.HasCoinTossResult ? _matchCoinToss.CoinTossWinner.ToString() : "Pending")}\n" +
            $"Player | Grounded {_playerJump.IsGrounded}  Speed {playerVelocity.magnitude:F1}  " +
            $"Last {_playtestMonitor.LastAction}\n" +
            $"Ball | Speed {_ball.Velocity.magnitude:F1}  Height {_ball.transform.position.y:F1}  " +
            $"Last Touch {lastTouch}\n" +
            $"RULES | Player Touches {_touchTracker.PlayerTeamTouches}/3  " +
            $"Last Player {DescribeTouchPlayer(CourtSide.Player)}  " +
            $"Last Action {DescribeTouchAction(CourtSide.Player)}\n" +
            $"RULES | Opponent Touches {_touchTracker.OpponentTeamTouches}/3  " +
            $"Last Player {DescribeTouchPlayer(CourtSide.Opponent)}  " +
            $"Last Action {DescribeTouchAction(CourtSide.Opponent)}\n" +
            $"RULES | Last Touch Team {lastTouch}  " +
            $"Last Rally Reason {_rallyEndDetector.EndReason}  " +
            $"Point Awarded To {(_rallyEndDetector.IsRallyEnded ? _rallyEndDetector.Winner.ToString() : "None")}\n" +
            $"AI | {_aiDecision.CurrentAction}  Distance {aiDistance:F1}  " +
            $"Landing {timeToLanding}  Reach {_aiController.CanReachCurrentTarget}  " +
            $"Reaction {_aiReactionTime:F2}s\n" +
            $"AI DEFENSE | IncomingSpeed {_trajectoryPredictor.IncomingSpeed:F2}  " +
            $"TimeToLanding {_trajectoryPredictor.TimeToLanding:F2}  " +
            $"Distance {_aiController.DistanceToLanding:F2}  " +
            $"TravelTime {_aiController.EstimatedTravelTime:F2}\n" +
            $"Urgency {_aiController.DefensiveUrgency}  " +
            $"ReactionDelay {_aiDecision.CurrentReactionDelay:F2}  " +
            $"Target {_aiController.MovementTarget:F2}  " +
            $"CanReach {_aiController.CanReachCurrentTarget}\n" +
            $"AI CONTACT | Inside Zone {_aiActions.IsBallInsideContactZone}  " +
            $"Distance {_aiActions.CurrentBallDistance:F2}  " +
            $"Height Valid {_aiActions.IsHeightValid}  " +
            $"Angle Valid {_aiActions.IsAngleValid}\n" +
            $"Cooldown {_aiActions.IsCooldownReady}  " +
            $"Decision {_aiDecision.CurrentAction}  " +
            $"Emergency Fallback {_aiActions.IsEmergencyFallbackActive}\n" +
            $"AI SPIKE | Role {_aiActions.AIRole}  State {_aiDecision.CurrentAction}  " +
            $"AttackReady {_aiActions.AttackReady}  CanSpike {_aiActions.CanSpike}  " +
            $"BallHeight {_aiActions.BallHeight:F2}  " +
            $"DistanceToBall {_aiActions.CurrentBallDistance:F2}\n" +
            $"SpikeTarget {_aiActions.SpikeTarget:F2}  " +
            $"LastAIAction {_aiActions.LastAIAction}\n" +
            $"Rally | Touches {_playtestMonitor.RallyTouches}  " +
            $"Longest {_playtestMonitor.LongestRally}  End {_playtestMonitor.LastRallyEndReason}\n" +
            $"CAMERA | Yaw {_cameraController.Yaw:F1}  Pitch {_cameraController.Pitch:F1}  " +
            $"Distance {_cameraController.Distance:F1}  BallAssist {_cameraController.BallAssistWeight:F2}  " +
            $"PlayerCameraRelativeMovement true\n" +
            $"ACTION | Receive: {_receiveSystem.CurrentMode}  " +
            $"Set: {_setSystem.AvailabilityStatus}  " +
            $"Spike: {_spikeSystem.AvailabilityStatus}\n" +
            $"SET TARGET {_setSystem.LastTarget:F2}  " +
            $"DIST TO NET {_setSystem.LastDistanceToNet:F2}  " +
            $"APEX {_setSystem.SetApexHeight:F2}\n" +
            $"SPIKE TARGET {_spikeSystem.LastTarget:F2}  " +
            $"QUALITY {_spikeSystem.ContactQuality}  " +
            $"LANDING {_spikeSystem.PredictedLanding:F2}  " +
            $"IN BOUNDS {_spikeSystem.TargetInBounds}\n" +
            $"TEAM SIDE Player {_movementBounds.Describe(CourtSide.Player)}  " +
            $"NET BLOCKED {_movementBounds.WasBlocked(CourtSide.Player)}\n" +
            $"TEAM SIDE Opponent {_movementBounds.Describe(CourtSide.Opponent)}  " +
            $"NET BLOCKED {_movementBounds.WasBlocked(CourtSide.Opponent)}\n" +
            $"ANIMATION | State {_playerAnimatedVisual.CurrentAnimationState}  " +
            $"Speed {_playerAnimatedVisual.AnimationSpeed:F2}  " +
            $"Grounded {_playerAnimatedVisual.AnimationGrounded}  " +
            $"VerticalVelocity {_playerAnimatedVisual.AnimationVerticalVelocity:F2}\n" +
            $"PLAYER TEAM | {DescribeResponsible(CourtSide.Player)}\n" +
            $"{DescribeTeamPlay(_playerTeamCoordinator)}\n" +
            $"{DescribePositioning(_playerTeamPositioning)}\n" +
            $"OPPONENT TEAM | {DescribeResponsible(CourtSide.Opponent)}\n" +
            $"{DescribeTeamPlay(_opponentTeamCoordinator)}\n" +
            $"{DescribePositioning(_opponentTeamPositioning)}";

        GUI.Box(new Rect(12f, 12f, 950f, 650f), text);
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
               _aiActions != null &&
               _trajectoryPredictor != null &&
               _cameraController != null &&
               _receiveSystem != null &&
               _setSystem != null &&
               _spikeSystem != null &&
               _movementBounds != null &&
               _playerAnimatedVisual != null &&
               _responsibilityResolver != null &&
               _playerTeamCoordinator != null &&
               _opponentTeamCoordinator != null &&
               _playerTeamPositioning != null &&
               _opponentTeamPositioning != null &&
               _matchCoinToss != null;
    }

    private string DescribeResponsible(CourtSide side)
    {
        TeamMember member = _responsibilityResolver.GetResponsible(side);
        if (member == null)
        {
            return "Responsible None";
        }

        string home = member.HomePosition != null
            ? member.HomePosition.name
            : "None";
        return $"Responsible {member.DisplayName}  Home {home}  " +
               $"{(member.IsHuman ? "Human" : "AI")}";
    }

    private string DescribeTouchPlayer(CourtSide side)
    {
        TeamMember player = _touchTracker.GetLastPlayer(side);
        return player != null
            ? player.DisplayName
            : "None";
    }

    private string DescribeTouchAction(CourtSide side)
    {
        return _touchTracker.GetLastAction(side).ToString();
    }

    private static string DescribeTeamPlay(TeamPlayCoordinator coordinator)
    {
        string receiver = coordinator.Receiver != null
            ? coordinator.Receiver.DisplayName
            : "None";
        string setter = coordinator.Setter != null
            ? coordinator.Setter.DisplayName
            : "None";
        string attacker = coordinator.Attacker != null
            ? coordinator.Attacker.DisplayName
            : "None";
        string nextResponsible = coordinator.NextResponsible != null
            ? coordinator.NextResponsible.DisplayName
            : "None";
        string lastTouchBy = coordinator.LastTouchBy != null
            ? coordinator.LastTouchBy.DisplayName
            : "None";
        return $"State {coordinator.State}  Receiver {receiver}  " +
               $"Setter {setter}  Attacker {attacker}  " +
               $"Touches {coordinator.TeamTouchCount}\n" +
               $"NextResponsible {nextResponsible}  LastPlayer {lastTouchBy}  " +
               $"LastAction {coordinator.LastAction}  Next {coordinator.NextAction}\n" +
               $"SET AI | Setter {setter}  Attacker {attacker}  " +
               $"SelectedSetZone {coordinator.SelectedSetZone}  " +
               $"SetTarget {coordinator.SelectedSetTarget:F1}  " +
               $"AttackerPosition {coordinator.SetTargetAttackerPosition:F1}  " +
               $"Reason {coordinator.SetTargetReason}";
    }

    private static string DescribePositioning(
        TeamPositioningController positioning)
    {
        TeamMember first = positioning.GetMember(0);
        TeamMember second = positioning.GetMember(1);
        return $"Positioning | {DescribeMemberPosition(positioning, first)} | " +
               DescribeMemberPosition(positioning, second);
    }

    private static string DescribeMemberPosition(
        TeamPositioningController positioning,
        TeamMember member)
    {
        if (member == null ||
            !positioning.TryGetDesiredPosition(member, out Vector3 desired))
        {
            return "None";
        }

        string home = member.HomePosition != null
            ? member.HomePosition.position.ToString("F1")
            : "None";
        return $"{member.DisplayName} Role {positioning.GetRole(member)} " +
               $"Home {home} Desired {desired:F1} " +
               $"Spacing {positioning.GetDistanceToTeammate(member):F1}";
    }
}
