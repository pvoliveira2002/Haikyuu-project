using System;
using UnityEngine;

public sealed class ScoreManager : MonoBehaviour
{
    public int PlayerScore { get; private set; }
    public int OpponentScore { get; private set; }
    public event Action<CourtSide> PointAdded;

    public void AddPoint(CourtSide winner)
    {
        if (winner == CourtSide.Player)
        {
            PlayerScore++;
        }
        else
        {
            OpponentScore++;
        }

        Debug.Log($"Point: {winner} | Score: Player {PlayerScore} x {OpponentScore} Opponent");
        PointAdded?.Invoke(winner);
    }

    public void ResetScore()
    {
        PlayerScore = 0;
        OpponentScore = 0;

        Debug.Log("Score reset: Player 0 x 0 Opponent");
    }
}
