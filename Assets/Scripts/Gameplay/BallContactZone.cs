using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public sealed class BallContactZone : MonoBehaviour
{
    [SerializeField] private KeyCode _interactionKey = KeyCode.E;
    [SerializeField] private Vector3 _localImpulseDirection = new Vector3(0f, 0.75f, 1f);
    [SerializeField, Min(0f)] private float _impulseForce = 3f;

    private VolleyballBall _ballInRange;

    private void Update()
    {
        if (_ballInRange == null || !Input.GetKeyDown(_interactionKey))
        {
            return;
        }

        Vector3 impulseDirection = transform.TransformDirection(_localImpulseDirection);
        _ballInRange.ApplyImpulse(impulseDirection, _impulseForce);
    }

    private void OnTriggerEnter(Collider other)
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
}
