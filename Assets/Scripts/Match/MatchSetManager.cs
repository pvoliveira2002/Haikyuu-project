using UnityEngine;

public sealed class MatchSetManager : MonoBehaviour
{
    [SerializeField] private ScoreManager _scoreManager;
    [SerializeField, Min(1)] private int _normalSetPoints = 25;
    [SerializeField, Min(1)] private int _decidingSetPoints = 15;
    [SerializeField, Min(1)] private int _winBy = 2;
    [SerializeField, Min(1)] private int _setsToWin = 2;

    public int PlayerSets { get; private set; }
    public int OpponentSets { get; private set; }
    public int CurrentSetNumber { get; private set; } = 1;
    public bool MatchOver { get; private set; }
    public CourtSide MatchWinner { get; private set; }
    public bool DidSetEndOnLastPoint { get; private set; }
    public CourtSide FirstServerForCurrentSet =>
        CurrentSetNumber % 2 == 1 ? CourtSide.Player : CourtSide.Opponent;

    private void OnEnable()
    {
        if (_scoreManager != null)
        {
            _scoreManager.PointAdded += HandlePointAdded;
        }
    }

    private void OnDisable()
    {
        if (_scoreManager != null)
        {
            _scoreManager.PointAdded -= HandlePointAdded;
        }
    }

    public void ResetMatch()
    {
        PlayerSets = 0;
        OpponentSets = 0;
        CurrentSetNumber = 1;
        MatchOver = false;
        DidSetEndOnLastPoint = false;
        _scoreManager?.ResetScore();
    }

    private void HandlePointAdded(CourtSide winner)
    {
        DidSetEndOnLastPoint = false;

        int target = IsDecidingSet() ? _decidingSetPoints : _normalSetPoints;
        int leaderScore = winner == CourtSide.Player
            ? _scoreManager.PlayerScore
            : _scoreManager.OpponentScore;
        int otherScore = winner == CourtSide.Player
            ? _scoreManager.OpponentScore
            : _scoreManager.PlayerScore;

        if (leaderScore < target || leaderScore - otherScore < _winBy)
        {
            return;
        }

        DidSetEndOnLastPoint = true;
        if (winner == CourtSide.Player)
        {
            PlayerSets++;
        }
        else
        {
            OpponentSets++;
        }

        Debug.Log($"Set Winner: {winner}");
        Debug.Log($"Sets: {PlayerSets} x {OpponentSets}");

        if (PlayerSets >= _setsToWin || OpponentSets >= _setsToWin)
        {
            MatchOver = true;
            MatchWinner = winner;
            Debug.Log($"MATCH WINNER: {MatchWinner}");
            return;
        }

        CurrentSetNumber++;
        _scoreManager.ResetScore();
    }

    private bool IsDecidingSet()
    {
        return PlayerSets == _setsToWin - 1 && OpponentSets == _setsToWin - 1;
    }
}
