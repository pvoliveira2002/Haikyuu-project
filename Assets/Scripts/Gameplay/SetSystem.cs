using UnityEngine;

public sealed class SetSystem : MonoBehaviour
{
    [SerializeField] private BallContactZone _contactZone;
    [SerializeField] private KeyCode _setKey = KeyCode.Q;
    [SerializeField, Min(0f)] private float _setForce = 2.35f;
    [SerializeField, Min(0f)] private float _verticalComponent = 1.2f;
    [SerializeField, Min(0f)] private float _forwardComponent = 0.18f;
    [SerializeField, Min(0f)] private float _minimumContactHeight = 0.8f;
    [SerializeField, Min(0f)] private float _maximumContactHeight = 2.3f;
    [SerializeField, Range(-1f, 1f)] private float _minimumForwardDot = -0.15f;

    private void Awake()
    {
        if (_contactZone == null)
        {
            _contactZone = GetComponentInChildren<BallContactZone>();
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(_setKey) ||
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
                _minimumForwardDot) ||
            !_contactZone.TryConsumeContact(ball))
        {
            return;
        }

        Vector3 setDirection =
            Vector3.up * _verticalComponent + transform.forward * _forwardComponent;

        ball.ResetVelocity();
        ball.ApplyImpulse(setDirection, _setForce);
        ball.GetComponent<BallTouchTracker>()?.RegisterTouch(CourtSide.Player);
        ActionFeedbackController.PlayFeedback(
            ActionFeedbackType.Set,
            ball.transform.position);
    }

    private void OnDrawGizmosSelected()
    {
        _contactZone?.DrawHeightRange(
            _minimumContactHeight,
            _maximumContactHeight,
            Color.cyan);
    }
}
