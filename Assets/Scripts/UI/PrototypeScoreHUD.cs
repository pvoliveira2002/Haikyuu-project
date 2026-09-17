using UnityEngine;

public sealed class PrototypeScoreHUD : MonoBehaviour
{
    [SerializeField] private ScoreManager _scoreManager;
    [SerializeField] private MatchSetManager _matchSetManager;
    [SerializeField] private ServePossessionController _servePossession;

    private void OnGUI()
    {
        if (_scoreManager == null || _matchSetManager == null || _servePossession == null)
        {
            return;
        }

        const float width = 320f;
        const float height = 105f;
        Rect area = new Rect((Screen.width - width) * 0.5f, 12f, width, height);

        string status = _matchSetManager.MatchOver
            ? $"MATCH WINNER: {_matchSetManager.MatchWinner}"
            : $"SAQUE: {_servePossession.CurrentServer}";
        string text =
            $"PLAYER       {_scoreManager.PlayerScore}  x  {_scoreManager.OpponentScore}       OPPONENT\n" +
            $"SETS           {_matchSetManager.PlayerSets}  x  {_matchSetManager.OpponentSets}\n" +
            status;

        GUI.Box(area, text);
    }
}
