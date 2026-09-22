using UnityEngine;

public sealed class TeamMember : MonoBehaviour
{
    [SerializeField] private CourtSide _teamSide;
    [SerializeField, Min(0)] private int _playerIndex;
    [SerializeField] private bool _isHuman;
    [SerializeField] private Transform _homePosition;

    public CourtSide TeamSide => _teamSide;
    public int PlayerIndex => _playerIndex;
    public bool IsHuman => _isHuman;
    public Transform HomePosition => _homePosition;
    public bool IsResponsible { get; private set; }
    public string DisplayName => gameObject.name;

    public void SetResponsible(bool responsible)
    {
        IsResponsible = responsible;
    }
}
