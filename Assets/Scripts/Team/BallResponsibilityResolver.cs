using UnityEngine;

public sealed class BallResponsibilityResolver : MonoBehaviour
{
    [SerializeField] private BallTrajectoryPredictor _trajectoryPredictor;
    [SerializeField] private RallyEndDetector _rallyEndDetector;
    [SerializeField] private CourtMovementBounds _movementBounds;
    [SerializeField] private TeamMember[] _playerTeam;
    [SerializeField] private TeamMember[] _opponentTeam;
    [SerializeField, Min(0f)] private float _responsibilitySwitchMargin = 0.5f;

    public TeamMember PlayerResponsible { get; private set; }
    public TeamMember OpponentResponsible { get; private set; }

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

    private void Update()
    {
        if (_trajectoryPredictor == null ||
            _movementBounds == null ||
            !_trajectoryPredictor.HasPrediction ||
            (_rallyEndDetector != null && _rallyEndDetector.IsRallyEnded))
        {
            ClearResponsibilities();
            return;
        }

        Vector3 landing = _trajectoryPredictor.PredictedLandingPoint;
        if (_movementBounds.Contains(landing, CourtSide.Player))
        {
            PlayerResponsible = ResolveTeam(
                _playerTeam,
                PlayerResponsible,
                landing);
            OpponentResponsible = SetResponsible(_opponentTeam, null);
        }
        else if (_movementBounds.Contains(landing, CourtSide.Opponent))
        {
            OpponentResponsible = ResolveTeam(
                _opponentTeam,
                OpponentResponsible,
                landing);
            PlayerResponsible = SetResponsible(_playerTeam, null);
        }
        else
        {
            ClearResponsibilities();
        }
    }

    public bool IsResponsible(TeamMember member)
    {
        return member != null && member.IsResponsible;
    }

    public TeamMember GetResponsible(CourtSide side)
    {
        return side == CourtSide.Player
            ? PlayerResponsible
            : OpponentResponsible;
    }

    private TeamMember ResolveTeam(
        TeamMember[] team,
        TeamMember current,
        Vector3 landing)
    {
        TeamMember best = FindClosest(team, landing);
        if (best == null)
        {
            return SetResponsible(team, null);
        }

        if (current != null && Contains(team, current) && current != best)
        {
            float currentDistance = HorizontalDistance(current.transform.position, landing);
            float bestDistance = HorizontalDistance(best.transform.position, landing);
            if (bestDistance + _responsibilitySwitchMargin >= currentDistance)
            {
                best = current;
            }
        }

        return SetResponsible(team, best);
    }

    private static TeamMember FindClosest(TeamMember[] team, Vector3 landing)
    {
        TeamMember closest = null;
        float closestDistance = float.PositiveInfinity;
        if (team == null)
        {
            return null;
        }

        foreach (TeamMember member in team)
        {
            if (member == null || !member.isActiveAndEnabled)
            {
                continue;
            }

            float distance = HorizontalDistance(member.transform.position, landing);
            if (distance < closestDistance)
            {
                closest = member;
                closestDistance = distance;
            }
        }

        return closest;
    }

    private static TeamMember SetResponsible(TeamMember[] team, TeamMember selected)
    {
        if (team != null)
        {
            foreach (TeamMember member in team)
            {
                member?.SetResponsible(member == selected);
            }
        }

        return selected;
    }

    private static bool Contains(TeamMember[] team, TeamMember member)
    {
        if (team == null)
        {
            return false;
        }

        foreach (TeamMember candidate in team)
        {
            if (candidate == member)
            {
                return true;
            }
        }

        return false;
    }

    private static float HorizontalDistance(Vector3 first, Vector3 second)
    {
        first.y = 0f;
        second.y = 0f;
        return Vector3.Distance(first, second);
    }

    private void ClearResponsibilities()
    {
        PlayerResponsible = SetResponsible(_playerTeam, null);
        OpponentResponsible = SetResponsible(_opponentTeam, null);
    }

    private void HandleRallyEnded(CourtSide winner)
    {
        ClearResponsibilities();
    }

    private void HandleRallyReset()
    {
        ClearResponsibilities();
    }

    private void OnDrawGizmosSelected()
    {
        if (_trajectoryPredictor == null || !_trajectoryPredictor.HasPrediction)
        {
            return;
        }

        TeamMember responsible = PlayerResponsible != null
            ? PlayerResponsible
            : OpponentResponsible;
        if (responsible == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            _trajectoryPredictor.PredictedLandingPoint,
            responsible.transform.position);
    }
}
