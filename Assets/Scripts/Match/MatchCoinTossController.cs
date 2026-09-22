using UnityEngine;

public enum MatchFlowState
{
    WaitingForCoinToss,
    ReadyForServe,
    RallyActive,
    MatchFinished
}

public sealed class MatchCoinTossController : MonoBehaviour
{
    [SerializeField] private MatchSetManager _matchSetManager;
    [SerializeField] private ServePossessionController _servePossession;
    [SerializeField] private RallyEndDetector _rallyEndDetector;

    public MatchFlowState State { get; private set; } =
        MatchFlowState.WaitingForCoinToss;
    public bool HasCoinTossResult { get; private set; }
    public CourtSide CoinTossWinner { get; private set; }

    private void OnEnable()
    {
        if (_servePossession != null)
        {
            _servePossession.ServeStarted += HandleServeStarted;
        }

        if (_rallyEndDetector != null)
        {
            _rallyEndDetector.RallyEnded += HandleRallyEnded;
            _rallyEndDetector.RallyReset += HandleRallyReset;
        }
    }

    private void Start()
    {
        ResolveCoinToss();
    }

    private void OnDisable()
    {
        if (_servePossession != null)
        {
            _servePossession.ServeStarted -= HandleServeStarted;
        }

        if (_rallyEndDetector != null)
        {
            _rallyEndDetector.RallyEnded -= HandleRallyEnded;
            _rallyEndDetector.RallyReset -= HandleRallyReset;
        }
    }

    private void ResolveCoinToss()
    {
        if (HasCoinTossResult)
        {
            return;
        }

        CoinTossWinner = Random.value < 0.5f
            ? CourtSide.Player
            : CourtSide.Opponent;
        HasCoinTossResult = true;
        State = MatchFlowState.ReadyForServe;
        Debug.Log($"COIN TOSS | Winner={CoinTossWinner}", this);
        _servePossession?.InitializeFirstServer(CoinTossWinner);
    }

    private void HandleServeStarted(CourtSide servingSide)
    {
        if (State != MatchFlowState.MatchFinished)
        {
            State = MatchFlowState.RallyActive;
        }
    }

    private void HandleRallyEnded(CourtSide winner)
    {
        State = _matchSetManager != null && _matchSetManager.MatchOver
            ? MatchFlowState.MatchFinished
            : MatchFlowState.ReadyForServe;
    }

    private void HandleRallyReset()
    {
        if (State != MatchFlowState.MatchFinished)
        {
            State = MatchFlowState.ReadyForServe;
        }
    }
}
