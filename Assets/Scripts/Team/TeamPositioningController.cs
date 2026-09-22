using UnityEngine;

public sealed class TeamPositioningController : MonoBehaviour
{
    [SerializeField] private CourtSide _teamSide;
    [SerializeField] private TeamMember[] _teamMembers;
    [SerializeField] private VolleyballBall _ball;
    [SerializeField] private TeamPlayCoordinator _teamPlayCoordinator;
    [SerializeField] private TeamPlayCoordinator _opponentTeamCoordinator;
    [SerializeField] private BallResponsibilityResolver _responsibilityResolver;
    [SerializeField] private CourtMovementBounds _movementBounds;
    [SerializeField] private Transform _teamSetTarget;
    [SerializeField] private Transform _attackReadyPosition;
    [SerializeField, Min(0f)] private float _minimumTeammateSpacing = 1.75f;
    [SerializeField, Range(0f, 1f)] private float _ballSideShift = 0.3f;
    [SerializeField, Min(0f)] private float _defensiveDepthOffset = 0.8f;
    [SerializeField, Min(0f)] private float _attackDefenseDepth = 0.75f;
    [SerializeField, Min(0f)] private float _coverageDepth = 1.4f;

    private readonly Vector3[] _desiredPositions = new Vector3[2];
    private readonly string[] _roles = new string[2];

    public float MinimumTeammateSpacing => _minimumTeammateSpacing;

    public TeamMember GetMember(int index)
    {
        return _teamMembers != null && index >= 0 && index < _teamMembers.Length
            ? _teamMembers[index]
            : null;
    }

    private void Update()
    {
        CalculateDesiredPositions();
    }

    public bool TryGetDesiredPosition(TeamMember member, out Vector3 position)
    {
        int index = GetMemberIndex(member);
        if (index < 0)
        {
            position = Vector3.zero;
            return false;
        }

        position = _desiredPositions[index];
        return true;
    }

    public string GetRole(TeamMember member)
    {
        int index = GetMemberIndex(member);
        return index >= 0 ? _roles[index] : "None";
    }

    public float GetDistanceToTeammate(TeamMember member)
    {
        TeamMember teammate = GetTeammate(member);
        if (member == null || teammate == null)
        {
            return 0f;
        }

        Vector3 offset = member.transform.position - teammate.transform.position;
        offset.y = 0f;
        return offset.magnitude;
    }

    private void CalculateDesiredPositions()
    {
        if (_teamMembers == null)
        {
            return;
        }

        for (int index = 0; index < _teamMembers.Length && index < 2; index++)
        {
            TeamMember member = _teamMembers[index];
            if (member == null)
            {
                continue;
            }

            _desiredPositions[index] = GetBaseDesiredPosition(member, index);
            _roles[index] = GetMemberRole(member);
        }

        ApplySpacing();
    }

    private Vector3 GetBaseDesiredPosition(TeamMember member, int index)
    {
        Vector3 home = member.HomePosition != null
            ? member.HomePosition.position
            : member.transform.position;
        TeamPlayState state = _teamPlayCoordinator != null
            ? _teamPlayCoordinator.State
            : TeamPlayState.Defending;
        bool responsible = _responsibilityResolver != null &&
            _responsibilityResolver.IsResponsible(member);

        if (responsible)
        {
            return member.transform.position;
        }

        if (state == TeamPlayState.Receiving && _teamSetTarget != null &&
            member == _teamPlayCoordinator.SupportPlayer)
        {
            return Clamp(_teamSetTarget.position);
        }

        if (state == TeamPlayState.Preparing &&
            member == _teamPlayCoordinator.Receiver)
        {
            return Clamp(
                _teamPlayCoordinator.GetSetTarget(
                    _teamPlayCoordinator.Setter));
        }

        if (state == TeamPlayState.Attacking &&
            member == _teamPlayCoordinator.Setter)
        {
            return Clamp(CalculateCoveragePosition());
        }

        float directionToNet = _teamSide == CourtSide.Player ? 1f : -1f;
        float lateralShift = _ball != null
            ? Mathf.Clamp(_ball.transform.position.x * _ballSideShift, -1.25f, 1.25f)
            : 0f;
        float depthOffset = index == 0
            ? _defensiveDepthOffset
            : -_defensiveDepthOffset;
        if (_opponentTeamCoordinator != null &&
            _opponentTeamCoordinator.State == TeamPlayState.Attacking &&
            index != 0)
        {
            depthOffset -= _attackDefenseDepth;
        }

        home.x += lateralShift;
        home.z += directionToNet * depthOffset;
        return Clamp(home);
    }

    private Vector3 CalculateCoveragePosition()
    {
        Vector3 anchor = _attackReadyPosition != null
            ? _attackReadyPosition.position
            : Vector3.zero;
        float awayFromNet = _teamSide == CourtSide.Player ? -1f : 1f;
        anchor.z += awayFromNet * _coverageDepth;
        if (_ball != null)
        {
            anchor.x = Mathf.Lerp(anchor.x, _ball.transform.position.x, 0.35f);
        }

        return anchor;
    }

    private void ApplySpacing()
    {
        if (_teamMembers == null || _teamMembers.Length < 2 ||
            _teamMembers[0] == null || _teamMembers[1] == null)
        {
            return;
        }

        Vector3 separation = _desiredPositions[1] - _desiredPositions[0];
        separation.y = 0f;
        float distance = separation.magnitude;
        if (distance >= _minimumTeammateSpacing)
        {
            return;
        }

        TeamMember responsible = _responsibilityResolver != null
            ? _responsibilityResolver.GetResponsible(_teamSide)
            : null;
        int movableIndex = responsible == _teamMembers[0] ? 1 : 0;
        int anchorIndex = movableIndex == 0 ? 1 : 0;
        float direction = _teamMembers[movableIndex].PlayerIndex <=
            _teamMembers[anchorIndex].PlayerIndex ? -1f : 1f;
        _desiredPositions[movableIndex].x =
            _desiredPositions[anchorIndex].x + direction * _minimumTeammateSpacing;
        _desiredPositions[movableIndex] = Clamp(_desiredPositions[movableIndex]);
    }

    private string GetMemberRole(TeamMember member)
    {
        if (_responsibilityResolver != null &&
            _responsibilityResolver.IsResponsible(member))
        {
            return _teamPlayCoordinator != null &&
                   _teamPlayCoordinator.State == TeamPlayState.Attacking
                ? "Attacker"
                : "Responsible";
        }

        if (_teamPlayCoordinator != null &&
            _teamPlayCoordinator.State == TeamPlayState.Attacking &&
            member == _teamPlayCoordinator.Setter)
        {
            return "Cover";
        }

        return "Support";
    }

    private int GetMemberIndex(TeamMember member)
    {
        if (_teamMembers == null || member == null)
        {
            return -1;
        }

        for (int index = 0; index < _teamMembers.Length && index < 2; index++)
        {
            if (_teamMembers[index] == member)
            {
                return index;
            }
        }

        return -1;
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

    private Vector3 Clamp(Vector3 position)
    {
        return _movementBounds != null
            ? _movementBounds.ClampPosition(position, _teamSide)
            : position;
    }

    private void OnDrawGizmosSelected()
    {
        if (_teamMembers != null)
        {
            for (int index = 0; index < _teamMembers.Length && index < 2; index++)
            {
                TeamMember member = _teamMembers[index];
                if (member == null)
                {
                    continue;
                }

                if (member.HomePosition != null)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawWireSphere(member.HomePosition.position, 0.25f);
                }

                Gizmos.color = member.IsResponsible ? Color.yellow : Color.cyan;
                Gizmos.DrawLine(member.transform.position, _desiredPositions[index]);
                Gizmos.DrawWireSphere(_desiredPositions[index], 0.3f);

                if (member.IsResponsible && _ball != null)
                {
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawLine(member.transform.position, _ball.transform.position);
                }
            }
        }

        if (_attackReadyPosition != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_attackReadyPosition.position, 0.3f);
        }

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(CalculateCoveragePosition(), 0.3f);
    }
}
