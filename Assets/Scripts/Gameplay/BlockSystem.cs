using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public sealed class BlockSystem : MonoBehaviour
{
    [SerializeField] private PlayerJump _playerJump;
    [SerializeField] private BallContactZone _sharedContactZone;
    [SerializeField] private KeyCode _blockKey = KeyCode.C;
    [SerializeField, Min(0f)] private float _blockForce = 1.2f;
    [SerializeField, Min(0f)] private float _downwardBias = 0.2f;
    [SerializeField, Min(0f)] private float _blockWindow = 0.3f;

    private bool _blockActive;
    private bool _hasBlockedBall;
    private float _blockEndTime;

    private void Awake()
    {
        if (_playerJump == null)
        {
            _playerJump = GetComponentInParent<PlayerJump>();
        }

        if (_sharedContactZone == null)
        {
            _sharedContactZone = GetComponentInParent<PlayerJump>()
                ?.GetComponentInChildren<BallContactZone>();
        }
    }

    private void Update()
    {
        if (_playerJump == null || _playerJump.IsGrounded)
        {
            _blockActive = false;
            return;
        }

        if (_blockActive && Time.time > _blockEndTime)
        {
            _blockActive = false;
        }

        if (!_blockActive && Input.GetKeyDown(_blockKey))
        {
            _blockActive = true;
            _hasBlockedBall = false;
            _blockEndTime = Time.time + _blockWindow;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!_blockActive ||
            _hasBlockedBall ||
            Time.time > _blockEndTime ||
            _playerJump == null ||
            _playerJump.IsGrounded ||
            !other.TryGetComponent(out VolleyballBall ball) ||
            (_sharedContactZone != null && !_sharedContactZone.TryConsumeContact(ball)))
        {
            return;
        }

        Vector3 blockDirection =
            transform.forward + Vector3.down * _downwardBias;

        ball.ApplyImpulse(blockDirection, _blockForce);
        ball.GetComponent<BallTouchTracker>()?.RegisterTouch(CourtSide.Player);
        ActionFeedbackController.PlayFeedback(
            ActionFeedbackType.Block,
            ball.transform.position);
        _hasBlockedBall = true;
        _blockActive = false;
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider contactCollider = GetComponent<BoxCollider>();
        if (contactCollider == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(contactCollider.center, contactCollider.size);
    }
}
