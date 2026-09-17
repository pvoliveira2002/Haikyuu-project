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
        if (!_contactZone.IsValidContact(
                ball,
                _minimumContactHeight,
                _maximumContactHeight,
                -0.5f) ||
            !_contactZone.TryConsumeContact(ball))
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
