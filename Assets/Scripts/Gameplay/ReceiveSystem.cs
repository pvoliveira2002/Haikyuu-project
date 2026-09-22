using UnityEngine;

public sealed class ReceiveSystem : MonoBehaviour
{
    [SerializeField] private BallContactZone _contactZone;
    [SerializeField] private ReceiveTargetResolver _targetResolver;
    [SerializeField] private Transform _netTopReference;
    [SerializeField] private KeyCode _receiveKey = KeyCode.E;
    [SerializeField, Min(0.1f)] private float _controlledFlightTime = 1.15f;
    [SerializeField, Min(0.1f)] private float _directMinimumFlightTime = 1.2f;
    [SerializeField, Min(0.1f)] private float _directMaximumFlightTime = 1.8f;
    [SerializeField, Min(0f)] private float _directNetClearance = 0.45f;
    [SerializeField, Min(0f)] private float _minimumContactHeight = 0.3f;
    [SerializeField, Min(0f)] private float _maximumContactHeight = 1.7f;
    [SerializeField, Range(-1f, 1f)] private float _minimumForwardDot = -0.25f;
    [SerializeField] private TeamMember _teamMember;
    [SerializeField] private TeamPlayCoordinator _teamPlayCoordinator;

    public string CurrentMode => Input.GetKey(KeyCode.LeftAlt)
        ? "DIRECT RETURN READY"
        : "CONTROLLED";

    private void Awake()
    {
        if (_contactZone == null)
        {
            _contactZone = GetComponentInChildren<BallContactZone>();
        }

        if (_targetResolver == null)
        {
            _targetResolver = GetComponent<ReceiveTargetResolver>();
        }
    }

    private void Update()
    {
        if (!Input.GetKeyDown(_receiveKey) ||
            Input.GetKeyDown(KeyCode.Q) ||
            Input.GetKeyDown(KeyCode.F) ||
            Input.GetKeyDown(KeyCode.C) ||
            _contactZone == null ||
            _targetResolver == null)
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

        if (!accepted)
        {
            return;
        }

        bool coordinatedReceive = _teamPlayCoordinator != null &&
            _teamPlayCoordinator.CanRegisterReceive(_teamMember);
        bool directReturn = !coordinatedReceive && Input.GetKey(KeyCode.LeftAlt);
        Vector3 target = coordinatedReceive
            ? _teamPlayCoordinator.GetReceiveTarget(_teamMember)
            : directReturn
                ? _targetResolver.ResolveDirectReturnTarget()
                : _targetResolver.ResolveControlledTarget();
        Vector3 launchVelocity;
        if (directReturn)
        {
            if (!TryBuildDirectReturnVelocity(ball.transform.position, target, out launchVelocity))
            {
                return;
            }
        }
        else
        {
            launchVelocity = CalculateLaunchVelocity(
                ball.transform.position,
                target,
                _controlledFlightTime);
        }

        ball.ResetVelocity();
        ball.ApplyImpulse(
            launchVelocity.normalized,
            launchVelocity.magnitude * ball.Mass);
        ball.GetComponent<BallTouchTracker>()?.RegisterTouch(CourtSide.Player);
        _teamPlayCoordinator?.NotifyContact(
            _teamMember,
            coordinatedReceive
                ? TeamPlayAction.Receive
                : TeamPlayAction.SafeReturn);
        ActionFeedbackController.PlayFeedback(
            ActionFeedbackType.Receive,
            ball.transform.position);
        Debug.Log(
            $"RECEIVE | Mode={(coordinatedReceive ? "TeamPass" : directReturn ? "DirectReturn" : "Controlled")} | " +
            $"Target={target}");
    }

    private bool TryBuildDirectReturnVelocity(
        Vector3 start,
        Vector3 target,
        out Vector3 launchVelocity)
    {
        float minimumTime = Mathf.Min(
            _directMinimumFlightTime,
            _directMaximumFlightTime);
        float maximumTime = Mathf.Max(
            _directMinimumFlightTime,
            _directMaximumFlightTime);
        launchVelocity = Vector3.zero;
        for (float flightTime = minimumTime;
             flightTime <= maximumTime + 0.001f;
             flightTime += 0.05f)
        {
            launchVelocity = CalculateLaunchVelocity(start, target, flightTime);
            if (ClearsNet(start, target, launchVelocity, flightTime))
            {
                return true;
            }
        }

        return false;
    }

    private bool ClearsNet(
        Vector3 start,
        Vector3 target,
        Vector3 launchVelocity,
        float flightTime)
    {
        if (_netTopReference == null)
        {
            return true;
        }

        float zDistance = target.z - start.z;
        if (Mathf.Abs(zDistance) <= 0.001f)
        {
            return false;
        }

        float fraction = (_netTopReference.position.z - start.z) / zDistance;
        if (fraction <= 0f || fraction >= 1f)
        {
            return true;
        }

        float timeAtNet = flightTime * fraction;
        float heightAtNet = start.y +
            launchVelocity.y * timeAtNet +
            0.5f * Physics.gravity.y * timeAtNet * timeAtNet;
        return heightAtNet >= _netTopReference.position.y + _directNetClearance;
    }

    private static Vector3 CalculateLaunchVelocity(
        Vector3 start,
        Vector3 target,
        float flightTime)
    {
        Vector3 displacement = target - start;
        Vector3 horizontalVelocity =
            new Vector3(displacement.x, 0f, displacement.z) / flightTime;
        float verticalVelocity =
            (displacement.y - 0.5f * Physics.gravity.y * flightTime * flightTime) /
            flightTime;
        return horizontalVelocity + Vector3.up * verticalVelocity;
    }

    private void OnDrawGizmosSelected()
    {
        _contactZone?.DrawHeightRange(
            _minimumContactHeight,
            _maximumContactHeight,
            Color.green);
    }
}
