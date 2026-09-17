using UnityEngine;

public enum CourtSide
{
    Player,
    Opponent
}

[RequireComponent(typeof(Collider))]
public sealed class CourtGroundZone : MonoBehaviour
{
    [SerializeField] private CourtSide _courtSide;
    [SerializeField] private RallyEndDetector _rallyEndDetector;

    public CourtSide Side => _courtSide;

    private void Awake()
    {
        if (_rallyEndDetector == null)
        {
            _rallyEndDetector = GetComponentInParent<RallyEndDetector>();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_rallyEndDetector != null &&
            collision.collider.TryGetComponent(out VolleyballBall ball))
        {
            _rallyEndDetector.ReportBallLanded(_courtSide);
        }
    }
}
