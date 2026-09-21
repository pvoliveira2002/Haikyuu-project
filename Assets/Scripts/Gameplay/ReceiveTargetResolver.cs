using UnityEngine;

public sealed class ReceiveTargetResolver : MonoBehaviour
{
    [SerializeField] private Transform _controlledTarget;
    [SerializeField] private Transform _directReturnTarget;
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private Vector2 _controlledXLimits = new Vector2(-4f, 4f);
    [SerializeField] private Vector2 _controlledZLimits = new Vector2(-8f, -1.25f);
    [SerializeField] private Vector2 _directXLimits = new Vector2(-3f, 3f);
    [SerializeField] private Vector2 _directZLimits = new Vector2(3.5f, 7f);
    [SerializeField, Min(0f)] private float _directLateralVariation = 0.65f;
    [SerializeField, Min(0f)] private float _cameraAimInfluence = 0.75f;

    public Vector3 ResolveControlledTarget()
    {
        Vector3 target = _controlledTarget != null
            ? _controlledTarget.position
            : transform.position + transform.forward * 1.8f;
        target.x = Mathf.Clamp(
            target.x,
            Mathf.Min(_controlledXLimits.x, _controlledXLimits.y),
            Mathf.Max(_controlledXLimits.x, _controlledXLimits.y));
        target.z = Mathf.Clamp(
            target.z,
            Mathf.Min(_controlledZLimits.x, _controlledZLimits.y),
            Mathf.Max(_controlledZLimits.x, _controlledZLimits.y));
        return target;
    }

    public Vector3 ResolveDirectReturnTarget()
    {
        Vector3 target = _directReturnTarget != null
            ? _directReturnTarget.position
            : new Vector3(0f, 0.21f, 5.5f);
        float cameraInfluence = 0f;
        if (_cameraTransform != null)
        {
            Vector3 cameraForward = _cameraTransform.forward;
            cameraForward.y = 0f;
            if (cameraForward.sqrMagnitude > 0.0001f)
            {
                cameraInfluence = cameraForward.normalized.x * _cameraAimInfluence;
            }
        }

        target.x += cameraInfluence +
            Random.Range(-_directLateralVariation, _directLateralVariation);
        target.x = Mathf.Clamp(
            target.x,
            Mathf.Min(_directXLimits.x, _directXLimits.y),
            Mathf.Max(_directXLimits.x, _directXLimits.y));
        target.z = Mathf.Clamp(
            target.z,
            Mathf.Min(_directZLimits.x, _directZLimits.y),
            Mathf.Max(_directZLimits.x, _directZLimits.y));
        return target;
    }
}
