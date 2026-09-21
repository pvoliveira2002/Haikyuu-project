using UnityEngine;

public sealed class ReceiveSystem : MonoBehaviour
{
    [SerializeField] private BallContactZone _contactZone;
    [SerializeField] private KeyCode _receiveKey = KeyCode.E;
    [SerializeField, Min(0f)] private float _receiveForce = 2.5f;
    [SerializeField, Min(0f)] private float _verticalComponent = 1.15f;
    [SerializeField, Min(0f)] private float _horizontalMultiplier = 0.65f;
    [SerializeField, Min(0f)] private float _minimumContactHeight = 0.3f;
    [SerializeField, Min(0f)] private float _maximumContactHeight = 1.7f;
    [SerializeField, Range(-1f, 1f)] private float _minimumForwardDot = -0.25f;

    private void Awake()
    {
        if (_contactZone == null)
        {
            _contactZone = GetComponentInChildren<BallContactZone>();
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(_receiveKey) ||
            Input.GetKeyDown(KeyCode.Q) ||
            Input.GetKeyDown(KeyCode.F) ||
            Input.GetKeyDown(KeyCode.C) ||
            _contactZone == null)
        {
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
        bool cooldownReady = _contactZone.ContactCooldownRemaining <= 0f;
        bool accepted = insideZone &&
            heightValid &&
            angleValid &&
            cooldownReady &&
            _contactZone.TryConsumeContact(ball);

        Debug.Log(
            $"PLAYER CONTACT | InsideZone: {insideZone} | HeightValid: {heightValid} | " +
            $"AngleValid: {angleValid} | CooldownReady: {cooldownReady} | " +
            $"ContactAccepted: {accepted}");

        if (!accepted)
        {
            return;
        }

        Vector3 receiveDirection =
            transform.forward * _horizontalMultiplier + Vector3.up * _verticalComponent;

        ball.ResetVelocity();
        ball.ApplyImpulse(receiveDirection, _receiveForce);
        ball.GetComponent<BallTouchTracker>()?.RegisterTouch(CourtSide.Player);
        ActionFeedbackController.PlayFeedback(
            ActionFeedbackType.Receive,
            ball.transform.position);
    }

    private void OnDrawGizmosSelected()
    {
        _contactZone?.DrawHeightRange(
            _minimumContactHeight,
            _maximumContactHeight,
            Color.green);
    }
}
