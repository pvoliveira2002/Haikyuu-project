using UnityEngine;

public sealed class SpikeTargetResolver : MonoBehaviour
{
    [SerializeField] private Vector2 _opponentHorizontalLimits = new Vector2(-3.9f, 3.9f);
    [SerializeField] private Vector2 _opponentDepthLimits = new Vector2(1f, 8.2f);
    [SerializeField, Min(0f)] private float _lateralInputInfluence = 2f;
    [SerializeField, Min(0f)] private float _defaultTargetDepth = 5.5f;
    [SerializeField, Range(0f, 1f)] private float _minimumAimAssist = 0.55f;

    public Vector3 LastTarget { get; private set; }
    public Vector3 PredictedLanding => LastTarget;
    public float LastContactQuality { get; private set; }
    public string LastContactCategory { get; private set; } = "N/A";
    public bool LastTargetInBounds { get; private set; }

    public Vector3 ResolveTarget(
        Vector3 ballPosition,
        Vector3 desiredDirection,
        float lateralInput,
        float contactQuality,
        float landingHeight)
    {
        Vector3 horizontal = Vector3.ProjectOnPlane(desiredDirection, Vector3.up);
        if (horizontal.sqrMagnitude <= 0.0001f)
        {
            horizontal = Vector3.forward;
        }

        horizontal.Normalize();
        horizontal.z = Mathf.Max(horizontal.z, 0.2f);
        horizontal.Normalize();

        float targetDepth = Mathf.Clamp(
            _defaultTargetDepth + horizontal.z * 1.2f,
            _opponentDepthLimits.x,
            _opponentDepthLimits.y);
        float travelToDepth = Mathf.Max(0f, targetDepth - ballPosition.z);
        float rawX = ballPosition.x +
            horizontal.x / Mathf.Max(horizontal.z, 0.2f) * travelToDepth +
            lateralInput * _lateralInputInfluence;
        Vector3 rawTarget = new Vector3(rawX, landingHeight, targetDepth);
        Vector3 safeTarget = new Vector3(
            Mathf.Clamp(rawTarget.x, _opponentHorizontalLimits.x, _opponentHorizontalLimits.y),
            landingHeight,
            Mathf.Clamp(rawTarget.z, _opponentDepthLimits.x, _opponentDepthLimits.y));
        float aimAssist = Mathf.Lerp(_minimumAimAssist, 1f, contactQuality);

        LastTarget = Vector3.Lerp(rawTarget, safeTarget, aimAssist);
        LastContactQuality = contactQuality;
        LastContactCategory = contactQuality >= 0.75f
            ? "Perfect"
            : contactQuality >= 0.45f ? "Good" : "Poor";
        LastTargetInBounds = IsInsideSafeArea(LastTarget);
        return LastTarget;
    }

    private bool IsInsideSafeArea(Vector3 target)
    {
        return target.x >= _opponentHorizontalLimits.x &&
               target.x <= _opponentHorizontalLimits.y &&
               target.z >= _opponentDepthLimits.x &&
               target.z <= _opponentDepthLimits.y;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(
            new Vector3(
                (_opponentHorizontalLimits.x + _opponentHorizontalLimits.y) * 0.5f,
                0.05f,
                (_opponentDepthLimits.x + _opponentDepthLimits.y) * 0.5f),
            new Vector3(
                _opponentHorizontalLimits.y - _opponentHorizontalLimits.x,
                0.1f,
                _opponentDepthLimits.y - _opponentDepthLimits.x));
    }
}
