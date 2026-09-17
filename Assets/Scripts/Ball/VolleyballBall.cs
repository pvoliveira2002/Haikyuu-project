using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public sealed class VolleyballBall : MonoBehaviour
{
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField, Min(0f)] private float _maximumSpeed = 25f;

    private void Awake()
    {
        if (_rigidbody == null)
        {
            _rigidbody = GetComponent<Rigidbody>();
        }
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
}
