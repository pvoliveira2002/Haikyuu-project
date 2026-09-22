using UnityEngine;

public enum TeamPlayState
{
    Defending,
    Receiving,
    Preparing,
    Attacking,
    BallSent
}

public enum TeamPlayAction
{
    None,
    Receive,
    Set,
    Attack,
    SafeReturn
}

public enum TeamSetZone
{
    Left,
    Center,
    Right
}

public sealed class TeamPlayCoordinator : MonoBehaviour
{
    [SerializeField] private CourtSide _teamSide;
    [SerializeField] private TeamMember[] _teamMembers;
    [SerializeField] private BallResponsibilityResolver _responsibilityResolver;
    [SerializeField] private RallyEndDetector _rallyEndDetector;
    [SerializeField] private VolleyballBall _ball;
    [SerializeField] private Transform _teamSetTarget;
    [SerializeField] private Transform _attackReadyPosition;
    [SerializeField, Min(0f)] private float _setZoneLateralOffset = 2.5f;
    [SerializeField, Min(0f)] private float _centerZoneHalfWidth = 0.75f;
    [SerializeField, Min(0f)] private float _courtHalfWidth = 4.5f;
    [SerializeField, Min(0f)] private float _courtHalfLength = 9f;
    [SerializeField, Min(0f)] private float _courtEdgeMargin = 0.6f;
    [SerializeField, Min(0f)] private float _netSafetyBuffer = 0.75f;

    public TeamPlayState State { get; private set; } = TeamPlayState.Defending;
    public TeamMember Receiver { get; private set; }
    public TeamMember SupportPlayer { get; private set; }
    public int TeamTouchCount { get; private set; }
    public CourtSide TeamSide => _teamSide;
    public string NextAction => GetExpectedActionName();
    public TeamMember Setter => SupportPlayer;
    public TeamMember Attacker => Receiver;
    public TeamMember NextResponsible { get; private set; }
    public TeamMember LastTouchBy { get; private set; }
    public TeamPlayAction LastAction { get; private set; }
    public TeamSetZone SelectedSetZone { get; private set; } = TeamSetZone.Center;
    public Vector3 SelectedSetTarget { get; private set; }
    public Vector3 SetTargetAttackerPosition { get; private set; }
    public string SetTargetReason { get; private set; } = "SafeTarget";

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

        _responsibilityResolver?.ReleaseResponsibilityLock(_teamSide);
    }

    private void Update()
    {
        if (_ball == null || _responsibilityResolver == null)
        {
            return;
        }

        if (IsBallOnOpponentSide())
        {
            if (TeamTouchCount > 0 || State != TeamPlayState.Defending)
            {
                State = TeamPlayState.BallSent;
                TeamTouchCount = 0;
                _responsibilityResolver.ReleaseResponsibilityLock(_teamSide);
            }

            return;
        }

        if (State == TeamPlayState.BallSent)
        {
            ResetSequence();
        }

        if (State != TeamPlayState.Defending)
        {
            return;
        }

        TeamMember selected = _responsibilityResolver.GetResponsible(_teamSide);
        if (selected != null)
        {
            BeginReceive(selected);
        }
    }

    public TeamPlayAction GetPlannedAction(TeamMember member)
    {
        if (member == null)
        {
            return TeamPlayAction.None;
        }

        if (State == TeamPlayState.Receiving && member == Receiver)
        {
            return TeamPlayAction.Receive;
        }

        if (State == TeamPlayState.Preparing && member == SupportPlayer)
        {
            return TeamPlayAction.Set;
        }

        if (State == TeamPlayState.Attacking && member == Receiver)
        {
            return TeamPlayAction.Attack;
        }

        return TeamPlayAction.None;
    }

    public bool CanRegisterReceive(TeamMember member)
    {
        return member != null &&
               member.TeamSide == _teamSide &&
               (State == TeamPlayState.Receiving ||
                State == TeamPlayState.Defending) &&
               TeamTouchCount == 0;
    }

    public Vector3 GetReceiveTarget(TeamMember member)
    {
        if (_teamSetTarget != null)
        {
            return _teamSetTarget.position;
        }

        return SupportPlayer != null
            ? SupportPlayer.transform.position + Vector3.up * 1.3f
            : member.transform.position + Vector3.up * 1.3f;
    }

    public Vector3 GetSetTarget(TeamMember member)
    {
        TeamMember attacker = Attacker;
        bool hasAttacker = attacker != null && attacker != member;
        Vector3 referencePosition;
        if (hasAttacker)
        {
            referencePosition = attacker.transform.position;
            SetTargetReason = "AttackerPosition";
        }
        else if (_ball != null)
        {
            referencePosition = _ball.transform.position;
            SetTargetReason = "FallbackBallSide";
        }
        else if (SupportPlayer != null)
        {
            referencePosition = SupportPlayer.transform.position;
            SetTargetReason = "FallbackSupportPlayer";
        }
        else
        {
            referencePosition = member != null
                ? member.transform.position
                : Vector3.zero;
            SetTargetReason = "SafeTarget";
        }

        SetTargetAttackerPosition = hasAttacker
            ? attacker.transform.position
            : Vector3.zero;
        SelectedSetZone = SelectSetZone(referencePosition.x);

        Vector3 target = _attackReadyPosition != null
            ? _attackReadyPosition.position
            : referencePosition + Vector3.up * 1.8f;
        target.x = GetZoneCenter(SelectedSetZone);
        SelectedSetTarget = ClampSetTarget(target);
        return SelectedSetTarget;
    }

    public bool TryGetPreparationTarget(TeamMember member, out Vector3 target)
    {
        target = Vector3.zero;
        if (member == null)
        {
            return false;
        }

        if (State == TeamPlayState.Receiving && member == SupportPlayer &&
            _teamSetTarget != null)
        {
            target = _teamSetTarget.position;
            return true;
        }

        if ((State == TeamPlayState.Preparing || State == TeamPlayState.Attacking) &&
            member == Receiver)
        {
            target = GetSetTarget(Setter);
            return true;
        }

        return false;
    }

    public bool IsFallbackAllowed(TeamMember member)
    {
        return member != null &&
               (member == Receiver || member == SupportPlayer) &&
               (State == TeamPlayState.Preparing || State == TeamPlayState.Attacking);
    }

    public void NotifyContact(TeamMember member, TeamPlayAction action)
    {
        if (member == null ||
            member.TeamSide != _teamSide ||
            action == TeamPlayAction.None)
        {
            return;
        }

        if (TeamTouchCount == 0 &&
            (State == TeamPlayState.Receiving ||
             State == TeamPlayState.Defending))
        {
            if (member != Receiver)
            {
                Receiver = member;
                SupportPlayer = GetTeammate(member);
            }

            TeamTouchCount = 1;
            State = TeamPlayState.Preparing;
            LastTouchBy = member;
            LastAction = action;
            NextResponsible = SupportPlayer;
            _responsibilityResolver.SetResponsibilityLock(_teamSide, SupportPlayer);
            LogTeamPlayEvent();
            return;
        }

        if (TeamTouchCount == 1 &&
            State == TeamPlayState.Preparing &&
            member == SupportPlayer)
        {
            TeamTouchCount = 2;
            State = TeamPlayState.Attacking;
            LastTouchBy = member;
            LastAction = action;
            NextResponsible = Receiver;
            _responsibilityResolver.SetResponsibilityLock(_teamSide, Receiver);
            LogTeamPlayEvent();
            return;
        }

        if (TeamTouchCount == 2 &&
            State == TeamPlayState.Attacking &&
            member == Receiver)
        {
            TeamTouchCount = 3;
            State = TeamPlayState.BallSent;
            LastTouchBy = member;
            LastAction = action;
            NextResponsible = null;
            _responsibilityResolver.ReleaseResponsibilityLock(_teamSide);
            LogTeamPlayEvent();
            return;
        }

        if ((action == TeamPlayAction.Attack ||
             action == TeamPlayAction.SafeReturn) &&
            (member == Receiver || member == SupportPlayer))
        {
            TeamTouchCount = Mathf.Min(3, TeamTouchCount + 1);
            State = TeamPlayState.BallSent;
            LastTouchBy = member;
            LastAction = action;
            NextResponsible = null;
            _responsibilityResolver.ReleaseResponsibilityLock(_teamSide);
            LogTeamPlayEvent();
        }
    }

    private void BeginReceive(TeamMember selected)
    {
        Receiver = selected;
        SupportPlayer = GetTeammate(selected);
        TeamTouchCount = 0;
        State = TeamPlayState.Receiving;
        NextResponsible = Receiver;
        _responsibilityResolver.SetResponsibilityLock(_teamSide, Receiver);
    }

    private TeamMember GetTeammate(TeamMember member)
    {
        if (_teamMembers == null)
        {
            return null;
        }

        foreach (TeamMember candidate in _teamMembers)
        {
            if (candidate != null && candidate != member)
            {
                return candidate;
            }
        }

        return null;
    }

    private TeamSetZone SelectSetZone(float worldX)
    {
        if (worldX < -_centerZoneHalfWidth)
        {
            return TeamSetZone.Left;
        }

        return worldX > _centerZoneHalfWidth
            ? TeamSetZone.Right
            : TeamSetZone.Center;
    }

    private float GetZoneCenter(TeamSetZone zone)
    {
        switch (zone)
        {
            case TeamSetZone.Left:
                return -_setZoneLateralOffset;
            case TeamSetZone.Right:
                return _setZoneLateralOffset;
            default:
                return 0f;
        }
    }

    private Vector3 ClampSetTarget(Vector3 target)
    {
        float sideLimit = Mathf.Max(0f, _courtHalfWidth - _courtEdgeMargin);
        float endLimit = Mathf.Max(_netSafetyBuffer, _courtHalfLength - _courtEdgeMargin);
        target.x = Mathf.Clamp(target.x, -sideLimit, sideLimit);
        target.z = _teamSide == CourtSide.Player
            ? Mathf.Clamp(target.z, -endLimit, -_netSafetyBuffer)
            : Mathf.Clamp(target.z, _netSafetyBuffer, endLimit);
        return target;
    }

    private bool IsBallOnOpponentSide()
    {
        const float CenterMargin = 0.15f;
        return _teamSide == CourtSide.Player
            ? _ball.transform.position.z > CenterMargin
            : _ball.transform.position.z < -CenterMargin;
    }

    private string GetExpectedActionName()
    {
        switch (State)
        {
            case TeamPlayState.Receiving:
                return "Receive";
            case TeamPlayState.Preparing:
                return "Set";
            case TeamPlayState.Attacking:
                return "Attack";
            case TeamPlayState.BallSent:
                return "Wait";
            default:
                return "Defend";
        }
    }

    private void ResetSequence()
    {
        State = TeamPlayState.Defending;
        Receiver = null;
        SupportPlayer = null;
        TeamTouchCount = 0;
        NextResponsible = null;
        LastTouchBy = null;
        LastAction = TeamPlayAction.None;
        SelectedSetZone = TeamSetZone.Center;
        SelectedSetTarget = _attackReadyPosition != null
            ? ClampSetTarget(
                new Vector3(
                    0f,
                    _attackReadyPosition.position.y,
                    _attackReadyPosition.position.z))
            : Vector3.zero;
        SetTargetAttackerPosition = Vector3.zero;
        SetTargetReason = "SafeTarget";
        _responsibilityResolver?.ReleaseResponsibilityLock(_teamSide);
    }

    private void LogTeamPlayEvent()
    {
        Debug.Log(
            $"TEAM PLAY | Team={_teamSide} | Touch={TeamTouchCount} | " +
            $"By={(LastTouchBy != null ? LastTouchBy.DisplayName : "None")} | " +
            $"Action={LastAction} | " +
            $"Next={(NextResponsible != null ? NextResponsible.DisplayName : "None")} | " +
            $"State={State}",
            this);
    }

    private void HandleRallyEnded(CourtSide winner)
    {
        ResetSequence();
    }

    private void HandleRallyReset()
    {
        ResetSequence();
    }
}
