using UnityEngine;

public enum BallTouchAction
{
    None,
    Receive,
    Set,
    Spike,
    Block,
    Serve,
    Other
}

public sealed class BallTouchTracker : MonoBehaviour
{
    [SerializeField] private RallyEndDetector _rallyEndDetector;
    [SerializeField] private TeamPlayCoordinator _playerTeamCoordinator;
    [SerializeField] private TeamPlayCoordinator _opponentTeamCoordinator;

    public bool HasLastTouch { get; private set; }
    public CourtSide LastTouch { get; private set; }
    public TeamMember LastPlayer { get; private set; }
    public BallTouchAction LastAction { get; private set; }
    public int PlayerTeamTouches { get; private set; }
    public int OpponentTeamTouches { get; private set; }
    public TeamMember PlayerLastPlayer { get; private set; }
    public TeamMember OpponentLastPlayer { get; private set; }
    public BallTouchAction PlayerLastAction { get; private set; }
    public BallTouchAction OpponentLastAction { get; private set; }

    private void OnEnable()
    {
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

    public bool RegisterValidTouch(
        CourtSide side,
        TeamMember player,
        BallTouchAction action)
    {
        if (action == BallTouchAction.None ||
            (_rallyEndDetector != null && _rallyEndDetector.IsRallyEnded))
        {
            return false;
        }

        if (action == BallTouchAction.Serve)
        {
            RegisterServe(side, player);
            return true;
        }

        if (HasLastTouch && LastTouch == side &&
            player != null && player == LastPlayer)
        {
            ReportFault(side, RallyEndReason.DoubleContact);
            return false;
        }

        int nextCount = HasLastTouch && LastTouch == side
            ? GetTeamTouches(side) + 1
            : 1;
        if (nextCount > 3)
        {
            ReportFault(side, RallyEndReason.FourTouches);
            return false;
        }

        if (!HasLastTouch || LastTouch != side)
        {
            SetTeamTouches(Opposite(side), 0);
        }

        SetTeamTouches(side, nextCount);
        LastTouch = side;
        LastPlayer = player;
        LastAction = action;
        SetTeamLastContact(side, player, action);
        HasLastTouch = true;
        NotifyTeamPlay(side, player, action);
        Debug.Log(
            $"TOUCH | Team={side} | " +
            $"Player={(player != null ? player.DisplayName : "Unknown")} | " +
            $"Action={action} | Count={nextCount}",
            this);
        return true;
    }

    public void RegisterServe(CourtSide side, TeamMember server)
    {
        LastTouch = side;
        LastPlayer = server;
        LastAction = BallTouchAction.Serve;
        SetTeamLastContact(side, server, BallTouchAction.Serve);
        HasLastTouch = true;
        PlayerTeamTouches = 0;
        OpponentTeamTouches = 0;
        Debug.Log(
            $"TOUCH | Team={side} | " +
            $"Player={(server != null ? server.DisplayName : "Unknown")} | " +
            "Action=Serve | Count=0",
            this);
    }

    public int GetTeamTouches(CourtSide side)
    {
        return side == CourtSide.Player
            ? PlayerTeamTouches
            : OpponentTeamTouches;
    }

    public void Clear()
    {
        HasLastTouch = false;
        LastPlayer = null;
        LastAction = BallTouchAction.None;
        PlayerTeamTouches = 0;
        OpponentTeamTouches = 0;
        PlayerLastPlayer = null;
        OpponentLastPlayer = null;
        PlayerLastAction = BallTouchAction.None;
        OpponentLastAction = BallTouchAction.None;
    }

    private void ReportFault(CourtSide side, RallyEndReason reason)
    {
        CourtSide winner = Opposite(side);
        Debug.Log(
            $"FAULT | Team={side} | Reason={reason} | Point={winner}",
            this);
        _rallyEndDetector?.ReportFault(side, reason);
    }

    private void SetTeamTouches(CourtSide side, int value)
    {
        if (side == CourtSide.Player)
        {
            PlayerTeamTouches = value;
        }
        else
        {
            OpponentTeamTouches = value;
        }
    }

    public TeamMember GetLastPlayer(CourtSide side)
    {
        return side == CourtSide.Player
            ? PlayerLastPlayer
            : OpponentLastPlayer;
    }

    public BallTouchAction GetLastAction(CourtSide side)
    {
        return side == CourtSide.Player
            ? PlayerLastAction
            : OpponentLastAction;
    }

    private void SetTeamLastContact(
        CourtSide side,
        TeamMember player,
        BallTouchAction action)
    {
        if (side == CourtSide.Player)
        {
            PlayerLastPlayer = player;
            PlayerLastAction = action;
        }
        else
        {
            OpponentLastPlayer = player;
            OpponentLastAction = action;
        }
    }

    private void NotifyTeamPlay(
        CourtSide side,
        TeamMember player,
        BallTouchAction action)
    {
        TeamPlayCoordinator coordinator = side == CourtSide.Player
            ? _playerTeamCoordinator
            : _opponentTeamCoordinator;
        if (coordinator == null || player == null)
        {
            return;
        }

        TeamPlayAction teamAction;
        switch (action)
        {
            case BallTouchAction.Receive:
                teamAction = TeamPlayAction.Receive;
                break;
            case BallTouchAction.Set:
                teamAction = TeamPlayAction.Set;
                break;
            case BallTouchAction.Spike:
                teamAction = TeamPlayAction.Attack;
                break;
            default:
                teamAction = TeamPlayAction.SafeReturn;
                break;
        }

        coordinator.NotifyContact(player, teamAction);
    }

    private static CourtSide Opposite(CourtSide side)
    {
        return side == CourtSide.Player
            ? CourtSide.Opponent
            : CourtSide.Player;
    }

    private void HandleRallyEnded(CourtSide winner)
    {
        Clear();
    }

    private void HandleRallyReset()
    {
        Clear();
    }
}
