using UnityEngine;

public sealed class SetSystem : MonoBehaviour
{
    [SerializeField] private BallContactZone _contactZone;
    [SerializeField] private KeyCode _setKey = KeyCode.Q;
    [SerializeField, Min(0f)] private float _inputBuffer = 0.15f;
    [SerializeField, Min(0f)] private float _setForce = 2.35f;
    [SerializeField, Min(0f)] private float _verticalComponent = 1.2f;
    [SerializeField, Min(0f)] private float _forwardComponent = 0.18f;
    [SerializeField] private Transform _spikePreparationTarget;
    [SerializeField, Min(0.1f)] private float _setFlightTime = 1f;
    [SerializeField, Min(0f)] private float _minimumContactHeight = 0.65f;
    [SerializeField, Min(0f)] private float _maximumContactHeight = 2.5f;
    [SerializeField, Range(-1f, 1f)] private float _minimumForwardDot = -0.2f;

    private float _bufferEndTime = float.NegativeInfinity;
    private bool _hasPendingInput;
    private bool _lastInsideZone;
    private bool _lastHeightValid;
    private bool _lastAngleValid;

    public string AvailabilityStatus { get; private set; } = "BUFFER";

    private void Awake()
    {
        if (_contactZone == null)
        {
            _contactZone = GetComponentInChildren<BallContactZone>();
        }
    }

    private void Update()
    {
        if (_contactZone == null)
        {
            return;
        }

        if (Input.GetKeyDown(_setKey) &&
            !Input.GetKeyDown(KeyCode.F) &&
            !Input.GetKeyDown(KeyCode.C))
        {
            _bufferEndTime = Time.time + _inputBuffer;
            _hasPendingInput = true;
            _lastInsideZone = false;
            _lastHeightValid = false;
            _lastAngleValid = false;
        }

        if (Time.time > _bufferEndTime)
        {
            if (_hasPendingInput)
            {
                LogRejected();
                _hasPendingInput = false;
            }

            AvailabilityStatus = "BUFFER";
            return;
        }

        VolleyballBall ball = _contactZone.BallInRange;
        _contactZone.EvaluateContact(
            ball,
            _minimumContactHeight,
            _maximumContactHeight,
            _minimumForwardDot,
            out bool insideZone,
            out bool heightValid,
            out bool angleValid);
        _lastInsideZone = insideZone;
        _lastHeightValid = heightValid;
        _lastAngleValid = angleValid;
        if (!insideZone)
        {
            AvailabilityStatus = "OUT OF ZONE";
            return;
        }

        if (!heightValid)
        {
            AvailabilityStatus = "HEIGHT";
            return;
        }

        if (!angleValid)
        {
            AvailabilityStatus = "ANGLE";
            return;
        }

        if (_contactZone.ContactCooldownRemaining > 0f ||
            !_contactZone.TryConsumeContact(ball))
        {
            AvailabilityStatus = "COOLDOWN";
            return;
        }

        Vector3 setVelocity = CalculateSetVelocity(ball);

        ball.ResetVelocity();
        ball.ApplyImpulse(setVelocity, setVelocity.magnitude * ball.Mass);
        ball.GetComponent<BallTouchTracker>()?.RegisterTouch(CourtSide.Player);
        ActionFeedbackController.PlayFeedback(
            ActionFeedbackType.Set,
            ball.transform.position);
        AvailabilityStatus = "READY";
        _bufferEndTime = float.NegativeInfinity;
        _hasPendingInput = false;
        Debug.Log("SET ACCEPTED", this);
    }

    private Vector3 CalculateSetVelocity(VolleyballBall ball)
    {
        if (_spikePreparationTarget == null)
        {
            Vector3 fallbackDirection =
                Vector3.up * _verticalComponent + transform.forward * _forwardComponent;
            return fallbackDirection.normalized * (_setForce / ball.Mass);
        }

        Vector3 target = _spikePreparationTarget.position;
        target.x = Mathf.Clamp(target.x, -4f, 4f);
        target.z = Mathf.Clamp(target.z, -8f, -1.25f);
        Vector3 displacement = target - ball.transform.position;
        Vector3 velocity = displacement / _setFlightTime;
        velocity.y += -0.5f * Physics.gravity.y * _setFlightTime;
        return velocity;
    }

    private void LogRejected()
    {
        bool cooldownReady = _contactZone.ContactCooldownRemaining <= 0f;
        Debug.Log(
            $"SET REJECTED | Zone={_lastInsideZone} | Height={_lastHeightValid} | " +
            $"Angle={_lastAngleValid} | Cooldown={cooldownReady} | Buffer=False",
            this);
    }

    private void OnDrawGizmosSelected()
    {
        _contactZone?.DrawHeightRange(
            _minimumContactHeight,
            _maximumContactHeight,
            Color.cyan);
    }
}
