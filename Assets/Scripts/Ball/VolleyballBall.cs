using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public sealed class VolleyballBall : MonoBehaviour
{
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField, Min(0.01f)] private float _mass = 0.27f;
    [SerializeField, Min(0f)] private float _linearDamping = 0.08f;
    [SerializeField, Min(0f)] private float _angularDamping = 0.08f;
    [SerializeField, Min(0f)] private float _maximumSpeed = 20f;

    public Vector3 Velocity => _rigidbody != null
        ? _rigidbody.linearVelocity
        : Vector3.zero;
    public float Mass => _rigidbody != null ? _rigidbody.mass : 0f;
    public float MaximumSpeed => _maximumSpeed;

    private void Awake()
    {
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        _rigidbody.mass = _mass;
        _rigidbody.linearDamping = _linearDamping;
        _rigidbody.angularDamping = _angularDamping;
        _rigidbody.useGravity = true;
        _rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
        _rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    private void FixedUpdate()
    {
        if (_rigidbody.linearVelocity.sqrMagnitude > _maximumSpeed * _maximumSpeed)
        {
            _rigidbody.linearVelocity = _rigidbody.linearVelocity.normalized * _maximumSpeed;
        }
    }

    public void ApplyImpulse(Vector3 direction, float force)
    {
        if (direction.sqrMagnitude <= 0f || force <= 0f)
        {
            return;
        }

        _rigidbody.AddForce(direction.normalized * force, ForceMode.Impulse);
    }

    public void ResetVelocity()
    {
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
    }

    public void ResetPosition(Vector3 position)
    {
        _rigidbody.position = position;
        ResetVelocity();
    }
}
