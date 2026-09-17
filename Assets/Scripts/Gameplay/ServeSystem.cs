using UnityEngine;

public sealed class ServeSystem : MonoBehaviour
{
    private const float PlayerBaseline = -9f;
    [SerializeField] private VolleyballBall _ball;
    [SerializeField] private Transform _servePoint;
    [SerializeField] private Transform _serveTarget;
    [SerializeField] private ServePossessionController _servePossession;
    [SerializeField] private KeyCode _serveKey = KeyCode.G;
    [SerializeField, Min(0f)] private float _serveForce = 3.3f;
    [SerializeField, Min(0f)] private float _verticalBias = 0.7f;
    [SerializeField, Min(0f)] private float _maximumServeDistance = 3.5f;

    private bool _serveAvailable = true;

    private void Update()
    {
        if (!_serveAvailable ||
            _servePossession == null ||
            !_servePossession.CanPlayerServe ||
            !Input.GetKeyDown(_serveKey) ||
            !CanServeFromCurrentPosition())
        {
            return;
        }

        Vector3 horizontalDirection = _serveTarget.position - _servePoint.position;
        horizontalDirection.y = 0f;

        if (horizontalDirection.sqrMagnitude <= 0f)
        {
            return;
        }

        _ball.ResetPosition(_servePoint.position);

        Vector3 serveDirection =
            horizontalDirection.normalized + Vector3.up * _verticalBias;

        _ball.ApplyImpulse(serveDirection, _serveForce);
        ActionFeedbackController.PlayFeedback(
            ActionFeedbackType.Serve,
            _ball.transform.position);
        _servePossession.NotifyPlayerServed();
        _serveAvailable = false;
    }

    private bool CanServeFromCurrentPosition()
    {
        if (_ball == null || _servePoint == null || _serveTarget == null)
        {
            return false;
        }

        Vector2 playerPosition = new Vector2(transform.position.x, transform.position.z);
        Vector2 servePosition = new Vector2(_servePoint.position.x, _servePoint.position.z);

        return transform.position.z <= PlayerBaseline + 1.5f &&
               Vector2.Distance(playerPosition, servePosition) <= _maximumServeDistance;
    }

    public void RearmServe()
    {
        _serveAvailable = true;
    }
}
