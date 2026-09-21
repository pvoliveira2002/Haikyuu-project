using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public sealed class BallContactZone : MonoBehaviour
{
    [SerializeField, Min(0f)] private float _contactCooldown = 0.2f;

    private CharacterController _playerController;
    private SphereCollider _contactCollider;
    private Transform _playerRoot;
    private VolleyballBall _ballInRange;
    private float _nextContactTime;

    public VolleyballBall BallInRange => _ballInRange;
    public float ContactCooldownRemaining => Mathf.Max(0f, _nextContactTime - Time.time);

    private void Awake()
    {
        _contactCollider = GetComponent<SphereCollider>();
        _playerController = GetComponentInParent<CharacterController>();
        _playerRoot = _playerController != null
            ? _playerController.transform
            : transform.parent;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out VolleyballBall ball))
        {
            _ballInRange = ball;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent(out VolleyballBall ball))
        {
            _ballInRange = ball;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out VolleyballBall ball) && ball == _ballInRange)
        {
            _ballInRange = null;
        }
    }

    private void OnDisable()
    {
        _ballInRange = null;
    }

    public bool IsValidContact(
        VolleyballBall ball,
        float minimumHeight,
        float maximumHeight,
        float minimumForwardDot)
    {
        EvaluateContact(
            ball,
            minimumHeight,
            maximumHeight,
            minimumForwardDot,
            out bool insideZone,
            out bool heightValid,
            out bool angleValid);
        return insideZone && heightValid && angleValid;
    }

    public void EvaluateContact(
        VolleyballBall ball,
        float minimumHeight,
        float maximumHeight,
        float minimumForwardDot,
        out bool insideZone,
        out bool heightValid,
        out bool angleValid)
    {
        insideZone = ball != null &&
            ball == _ballInRange &&
            _playerRoot != null &&
            IsPhysicallyWithinReach(ball);
        heightValid = false;
        angleValid = false;
        if (!insideZone)
        {
            return;
        }

        Vector3 ballOffset = ball.transform.position - _playerRoot.position;
        float playerBaseHeight = _playerController != null
            ? _playerController.bounds.min.y
            : _playerRoot.position.y;
        float relativeHeight = ball.transform.position.y - playerBaseHeight;
        Vector3 horizontalOffset = new Vector3(ballOffset.x, 0f, ballOffset.z);

        heightValid = relativeHeight >= minimumHeight &&
            relativeHeight <= maximumHeight;

        if (horizontalOffset.sqrMagnitude <= 0.0001f)
        {
            angleValid = true;
            return;
        }

        angleValid = Vector3.Dot(
            _playerRoot.forward,
            horizontalOffset.normalized) >= minimumForwardDot;
    }

    private bool IsPhysicallyWithinReach(VolleyballBall ball)
    {
        if (_contactCollider == null)
        {
            return false;
        }

        SphereCollider ballCollider = ball.GetComponent<SphereCollider>();
        float ballRadius = ballCollider != null
            ? Mathf.Max(
                ballCollider.bounds.extents.x,
                ballCollider.bounds.extents.y,
                ballCollider.bounds.extents.z)
            : 0f;
        float contactRadius = _contactCollider.radius * Mathf.Max(
            transform.lossyScale.x,
            transform.lossyScale.y,
            transform.lossyScale.z);
        Vector3 contactCenter = transform.TransformPoint(_contactCollider.center);
        float totalRadius = contactRadius + ballRadius;

        return (ball.transform.position - contactCenter).sqrMagnitude <=
               totalRadius * totalRadius;
    }

    public bool TryConsumeContact(VolleyballBall ball)
    {
        if (ball == null || Time.time < _nextContactTime)
        {
            return false;
        }

        _nextContactTime = Time.time + _contactCooldown;
        return true;
    }

    public void DrawHeightRange(float minimumHeight, float maximumHeight, Color color)
    {
        SphereCollider contactCollider = GetComponent<SphereCollider>();
        Transform root = transform.parent;
        if (contactCollider == null || root == null)
        {
            return;
        }

        CharacterController playerController = _playerController != null
            ? _playerController
            : GetComponentInParent<CharacterController>();
        float playerBaseHeight = playerController != null
            ? playerController.bounds.min.y
            : root.position.y;
        float height = maximumHeight - minimumHeight;
        Vector3 center = transform.position;
        center.y = playerBaseHeight + minimumHeight + height * 0.5f;

        Gizmos.color = color;
        Gizmos.DrawWireSphere(
            transform.TransformPoint(contactCollider.center),
            contactCollider.radius * Mathf.Max(
                transform.lossyScale.x,
                transform.lossyScale.y,
                transform.lossyScale.z));
        Gizmos.DrawWireCube(
            center,
            new Vector3(contactCollider.radius * 2f, height, contactCollider.radius * 2f));
    }
}
