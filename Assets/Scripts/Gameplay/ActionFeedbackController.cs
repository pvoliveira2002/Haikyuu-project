using System;
using UnityEngine;

public enum ActionFeedbackType
{
    Receive,
    Set,
    Spike,
    Block,
    Serve,
    AIReceive,
    AIAttack,
    AIServe
}

public sealed class ActionFeedbackController : MonoBehaviour
{
    [SerializeField] private VolleyballCameraController _cameraController;
    [SerializeField] private VolleyballBall _ball;
    [SerializeField] private bool _enableFeedback = true;
    [SerializeField] private bool _enableCameraImpulse = true;
    [SerializeField] private bool _enableTrail = true;
    [SerializeField, Min(0.01f)] private float _contactEffectDuration = 0.1f;
    [SerializeField, Min(0.01f)] private float _contactEffectScale = 0.35f;
    [SerializeField, Min(0.01f)] private float _trailTime = 0.2f;
    [SerializeField, Min(0f)] private float _trailMinSpeed = 10f;

    private static ActionFeedbackController _instance;
    private GameObject _contactEffect;
    private MeshRenderer _contactRenderer;
    private TrailRenderer _trail;
    private float _effectEndTime;
    private float _trailEndTime;

    public static event Action<ActionFeedbackType, Vector3> FeedbackPlayed;

    private void Awake()
    {
        _instance = this;
        CreateContactEffect();
        ConfigureTrail();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    private void Update()
    {
        if (_contactEffect != null && _contactEffect.activeSelf)
        {
            float remaining = Mathf.Clamp01(
                (_effectEndTime - Time.time) / _contactEffectDuration);
            _contactEffect.transform.localScale =
                Vector3.one * _contactEffectScale * remaining;

            if (Time.time >= _effectEndTime)
            {
                _contactEffect.SetActive(false);
            }
        }

        if (_trail != null && _trail.emitting && Time.time >= _trailEndTime)
        {
            _trail.emitting = false;
        }
    }

    public static void PlayFeedback(
        ActionFeedbackType type,
        Vector3 contactPosition)
    {
        FeedbackPlayed?.Invoke(type, contactPosition);
        _instance?.ShowFeedback(type, contactPosition);
    }

    private void ShowFeedback(ActionFeedbackType type, Vector3 position)
    {
        if (!_enableFeedback)
        {
            return;
        }

        Color color = GetColor(type);
        float scaleMultiplier = type == ActionFeedbackType.Spike ? 1.3f : 1f;
        _contactEffect.transform.position = position;
        _contactEffect.transform.localScale =
            Vector3.one * _contactEffectScale * scaleMultiplier;
        _contactRenderer.material.color = color;
        _contactEffect.SetActive(true);
        _effectEndTime = Time.time + _contactEffectDuration;

        if (_enableCameraImpulse && _cameraController != null)
        {
            _cameraController.ApplyImpulse(GetCameraStrength(type));
        }

        if (_enableTrail && IsStrongAction(type) && _trail != null &&
            _ball != null && _ball.Velocity.magnitude >= _trailMinSpeed)
        {
            _trail.startColor = color;
            _trail.endColor = new Color(color.r, color.g, color.b, 0f);
            _trail.Clear();
            _trail.emitting = true;
            _trailEndTime = Time.time + _trailTime;
        }
    }

    private void CreateContactEffect()
    {
        _contactEffect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        _contactEffect.name = "ActionContactFeedback";
        _contactEffect.transform.SetParent(transform, true);
        Destroy(_contactEffect.GetComponent<Collider>());
        _contactRenderer = _contactEffect.GetComponent<MeshRenderer>();
        _contactRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _contactEffect.SetActive(false);
    }

    private void ConfigureTrail()
    {
        if (_ball == null)
        {
            return;
        }

        _trail = _ball.GetComponent<TrailRenderer>();
        if (_trail == null)
        {
            _trail = _ball.gameObject.AddComponent<TrailRenderer>();
        }

        _trail.time = _trailTime;
        _trail.startWidth = 0.18f;
        _trail.endWidth = 0f;
        _trail.material = new Material(Shader.Find("Sprites/Default"));
        _trail.emitting = false;
    }

    private static bool IsStrongAction(ActionFeedbackType type)
    {
        return type == ActionFeedbackType.Spike ||
               type == ActionFeedbackType.Serve ||
               type == ActionFeedbackType.AIAttack;
    }

    private static float GetCameraStrength(ActionFeedbackType type)
    {
        switch (type)
        {
            case ActionFeedbackType.Serve: return 0.02f;
            case ActionFeedbackType.Block: return 0.03f;
            case ActionFeedbackType.Spike: return 0.04f;
            case ActionFeedbackType.AIAttack: return 0.03f;
            case ActionFeedbackType.AIServe: return 0.02f;
            default: return 0f;
        }
    }

    private static Color GetColor(ActionFeedbackType type)
    {
        switch (type)
        {
            case ActionFeedbackType.Receive: return new Color(0.35f, 0.75f, 1f);
            case ActionFeedbackType.Set: return Color.cyan;
            case ActionFeedbackType.Spike: return new Color(1f, 0.25f, 0.05f);
            case ActionFeedbackType.Block: return Color.yellow;
            case ActionFeedbackType.Serve: return Color.white;
            case ActionFeedbackType.AIReceive: return new Color(0.65f, 0.4f, 1f);
            case ActionFeedbackType.AIAttack: return new Color(1f, 0.35f, 0.65f);
            case ActionFeedbackType.AIServe: return new Color(0.85f, 0.75f, 1f);
            default: return Color.white;
        }
    }
}
