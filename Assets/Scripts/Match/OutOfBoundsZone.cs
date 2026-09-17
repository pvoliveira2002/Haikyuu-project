using UnityEngine;

[RequireComponent(typeof(Collider))]
public sealed class OutOfBoundsZone : MonoBehaviour
{
    [SerializeField] private RallyEndDetector _rallyEndDetector;
    [SerializeField] private BallTouchTracker _touchTracker;

    private void OnCollisionEnter(Collision collision)
    {
        if (_rallyEndDetector == null ||
            _touchTracker == null ||
            !_touchTracker.HasLastTouch ||
            !collision.collider.TryGetComponent(out VolleyballBall ball))
        {
            return;
        }

        _rallyEndDetector.ReportBallOut(_touchTracker.LastTouch);
    }
}
