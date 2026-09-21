using UnityEngine;

public sealed class CourtMovementBounds : MonoBehaviour
{
    [SerializeField] private Vector2 _horizontalLimits = new Vector2(-4.25f, 4.25f);
    [SerializeField] private Vector2 _playerDepthLimits = new Vector2(-8.5f, -0.35f);
    [SerializeField] private Vector2 _opponentDepthLimits = new Vector2(0.35f, 8.5f);

    private bool _playerWasBlocked;
    private bool _opponentWasBlocked;

    public float NetBuffer => Mathf.Min(
        Mathf.Abs(_playerDepthLimits.y),
        Mathf.Abs(_opponentDepthLimits.x));

    public Vector3 ClampPosition(Vector3 position, CourtSide side)
    {
        Vector2 depthLimits = GetDepthLimits(side);
        Vector3 clamped = new Vector3(
            Mathf.Clamp(position.x, _horizontalLimits.x, _horizontalLimits.y),
            position.y,
            Mathf.Clamp(position.z, depthLimits.x, depthLimits.y));
        bool wasBlocked = (clamped - position).sqrMagnitude > 0.0001f;
        if (side == CourtSide.Player)
        {
            _playerWasBlocked = wasBlocked;
        }
        else
        {
            _opponentWasBlocked = wasBlocked;
        }

        return clamped;
    }

    public bool Contains(Vector3 position, CourtSide side)
    {
        Vector2 depthLimits = GetDepthLimits(side);
        return position.x >= _horizontalLimits.x && position.x <= _horizontalLimits.y &&
               position.z >= depthLimits.x && position.z <= depthLimits.y;
    }

    public bool WasBlocked(CourtSide side)
    {
        return side == CourtSide.Player ? _playerWasBlocked : _opponentWasBlocked;
    }

    public string Describe(CourtSide side)
    {
        Vector2 depthLimits = GetDepthLimits(side);
        return $"X[{_horizontalLimits.x:F2},{_horizontalLimits.y:F2}] " +
               $"Z[{depthLimits.x:F2},{depthLimits.y:F2}]";
    }

    private Vector2 GetDepthLimits(CourtSide side)
    {
        return side == CourtSide.Player
            ? _playerDepthLimits
            : _opponentDepthLimits;
    }

    private void OnDrawGizmosSelected()
    {
        DrawSide(_playerDepthLimits, Color.cyan);
        DrawSide(_opponentDepthLimits, new Color(1f, 0.5f, 0f));
    }

    private void DrawSide(Vector2 depthLimits, Color color)
    {
        float width = _horizontalLimits.y - _horizontalLimits.x;
        float depth = depthLimits.y - depthLimits.x;
        Gizmos.color = color;
        Gizmos.DrawWireCube(
            new Vector3(
                (_horizontalLimits.x + _horizontalLimits.y) * 0.5f,
                0.05f,
                (depthLimits.x + depthLimits.y) * 0.5f),
            new Vector3(width, 0.1f, depth));
    }
}
