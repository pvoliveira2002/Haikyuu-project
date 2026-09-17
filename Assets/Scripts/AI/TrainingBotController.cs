using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public sealed class TrainingBotController : MonoBehaviour
{
    [SerializeField] private VolleyballBall _ball;
    [SerializeField] private Transform _returnTarget;
    [SerializeField] private SphereCollider _contactZone;
    [SerializeField, Min(0f)] private float _moveSpeed = 4.5f;
    [SerializeField, Min(0f)] private float _returnForce = 1.8f;
    [SerializeField, Min(0f)] private float _verticalBias = 0.55f;
    [SerializeField, Min(0f)] private float _contactRadius = 1.5f;
    [SerializeField, Min(0f)] private float _returnCooldown = 0.4f;
    [SerializeField] private Vector2 _horizontalLimits = new Vector2(-4f, 4f);
    [SerializeField] private Vector2 _depthLimits = new Vector2(1.25f, 8f);

    private float _nextReturnTime;

    private void Awake()
    {
        if (_contactZone == null)
        {
            _contactZone = GetComponent<SphereCollider>();
        }

        _contactZone.isTrigger = true;
        _contactZone.radius = _contactRadius;
    }

    private void Update()
    {
        if (_ball == null || _ball.transform.position.z <= 0f)
        {
            return;
        }

        Vector3 ballPosition = _ball.transform.position;
        Vector3 targetPosition = new Vector3(
            Mathf.Clamp(ballPosition.x, _horizontalLimits.x, _horizontalLimits.y),
            transform.position.y,
            Mathf.Clamp(ballPosition.z, _depthLimits.x, _depthLimits.y));

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            _moveSpeed * Time.deltaTime);
    }

    private void OnTriggerStay(Collider other)
    {
        if (_ball == null ||
            _returnTarget == null ||
            Time.time < _nextReturnTime ||
            _ball.transform.position.z <= 0f ||
            !other.TryGetComponent(out VolleyballBall detectedBall) ||
            detectedBall != _ball)
        {
            return;
        }

        Vector3 horizontalDirection = _returnTarget.position - _ball.transform.position;
        horizontalDirection.y = 0f;

        if (horizontalDirection.sqrMagnitude <= 0f)
        {
            return;
        }

        Vector3 returnDirection = (
            horizontalDirection.normalized + Vector3.up * _verticalBias).normalized;

        _ball.ResetVelocity();
        _ball.ApplyImpulse(returnDirection, _returnForce);
        _ball.GetComponent<BallTouchTracker>()?.RegisterTouch(CourtSide.Opponent);
        _nextReturnTime = Time.time + _returnCooldown;
    }
}
