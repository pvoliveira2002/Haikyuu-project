using UnityEngine;

public sealed class SpikeSystem : MonoBehaviour
{
    [SerializeField] private BallContactZone _contactZone;
    [SerializeField] private PlayerJump _playerJump;
    [SerializeField] private KeyCode _spikeKey = KeyCode.F;
    [SerializeField, Min(0f)] private float _spikeForce = 4f;
    [SerializeField, Min(0f)] private float _forwardBias = 1f;
    [SerializeField, Min(0f)] private float _downwardBias = 0.12f;
    [SerializeField, Min(0f)] private float _minimumContactHeight = 1.5f;
    [SerializeField, Min(0f)] private float _maximumContactHeight = 3f;
    [SerializeField, Min(0f)] private float _spikeWindow = 0.25f;
    [SerializeField, Min(0f)] private float _inputBuffer = 0.15f;
    [SerializeField, Range(-1f, 1f)] private float _minimumForwardDot = 0.15f;

    private float _bufferEndTime = float.NegativeInfinity;
    private float _spikeWindowEndTime = float.NegativeInfinity;

    private void Awake()
    {
        if (_contactZone == null)
        {
            _contactZone = GetComponentInChildren<BallContactZone>();
        }

        if (_playerJump == null)
        {
            _playerJump = GetComponent<PlayerJump>();
        }
    }

    private void Update()
    {
        if (_contactZone == null || _playerJump == null)
        {
            return;
        }

        if (Input.GetKeyDown(_spikeKey))
        {
            _bufferEndTime = Time.time + _inputBuffer;
        }

        if (Time.time <= _bufferEndTime && !_playerJump.IsGrounded)
        {
            _spikeWindowEndTime = Time.time + _spikeWindow;
            _bufferEndTime = 0f;
        }

        if (_playerJump.IsGrounded || Time.time > _spikeWindowEndTime)
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

        Vector3 spikeDirection =
            transform.forward * _forwardBias + Vector3.down * _downwardBias;

        ball.ResetVelocity();
        ball.ApplyImpulse(spikeDirection, _spikeForce);
        ball.GetComponent<BallTouchTracker>()?.RegisterTouch(CourtSide.Player);
        ActionFeedbackController.PlayFeedback(
            ActionFeedbackType.Spike,
            ball.transform.position);
        _spikeWindowEndTime = 0f;
    }

    private void OnDrawGizmosSelected()
    {
        _contactZone?.DrawHeightRange(
            _minimumContactHeight,
            _maximumContactHeight,
            Color.red);
    }
}
