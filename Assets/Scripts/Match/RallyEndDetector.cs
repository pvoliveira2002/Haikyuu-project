using System;
using UnityEngine;

public enum RallyEndReason
{
    None,
    BallGroundedPlayerSide,
    BallGroundedOpponentSide,
    OutOfBoundsPlayerLastTouch,
    OutOfBoundsOpponentLastTouch
}

public sealed class RallyEndDetector : MonoBehaviour
{
    [SerializeField] private ScoreManager _scoreManager;

    public bool IsRallyEnded { get; private set; }
    public CourtSide LandedSide { get; private set; }
    public CourtSide Winner { get; private set; }
    public RallyEndReason EndReason { get; private set; }
    public event Action<CourtSide> RallyEnded;
    public event Action RallyReset;

    public void ReportBallLanded(CourtSide landedSide)
    {
        if (IsRallyEnded)
        {
            return;
        }

        LandedSide = landedSide;
        CourtSide winner = landedSide == CourtSide.Player
            ? CourtSide.Opponent
            : CourtSide.Player;

        EndReason = landedSide == CourtSide.Player
            ? RallyEndReason.BallGroundedPlayerSide
            : RallyEndReason.BallGroundedOpponentSide;
        EndRally(winner, $"Rally ended - Ball landed on {LandedSide} side - Winner: {winner}");
    }

    public void ReportBallOut(CourtSide lastTouch)
    {
        CourtSide winner = lastTouch == CourtSide.Player
            ? CourtSide.Opponent
            : CourtSide.Player;

        EndReason = lastTouch == CourtSide.Player
            ? RallyEndReason.OutOfBoundsPlayerLastTouch
            : RallyEndReason.OutOfBoundsOpponentLastTouch;
        EndRally(winner, $"Rally ended - Ball out after {lastTouch} touch - Winner: {winner}");
    }

    public void ResetRally()
    {
        IsRallyEnded = false;
        EndReason = RallyEndReason.None;
        RallyReset?.Invoke();
    }

    private void EndRally(CourtSide winner, string message)
    {
        if (IsRallyEnded)
        {
            return;
        }

        IsRallyEnded = true;
        Winner = winner;

        Debug.Log(message);
        _scoreManager?.AddPoint(Winner);
        RallyEnded?.Invoke(Winner);
    }
}
