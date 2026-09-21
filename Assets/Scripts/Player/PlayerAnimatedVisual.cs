using System.Collections;
using UnityEngine;

public sealed class PlayerAnimatedVisual : MonoBehaviour
{
    [SerializeField] private GameObject _modelPrefab;
    [SerializeField] private AnimationClip _defaultAnimation;
    [SerializeField] private RuntimeAnimatorController _animatorController;
    [SerializeField] private GameObject _staticFallback;
    [SerializeField] private Vector3 _localPosition = new Vector3(0f, -1f, 0f);
    [SerializeField] private Vector3 _localEulerAngles;
    [SerializeField] private Vector3 _localScale = Vector3.one;
    [SerializeField] private float _speedSmoothing = 12f;
    [SerializeField] private float _idleThreshold = 0.15f;
    [SerializeField] private float _runThreshold = 6.5f;
    [SerializeField] private float _verticalThreshold = 0.1f;
    [SerializeField] private float _runToStopDuration = 0.3f;
    [SerializeField] private float _landingDuration = 0.25f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");
    private static readonly int IsStoppingHash = Animator.StringToHash("IsStopping");

    private CharacterController _characterController;
    private Animator _animator;
    private float _smoothedSpeed;
    private float _previousSpeed;
    private float _previousHeight;
    private float _stateEndTime;
    private float _lastRunTime = float.NegativeInfinity;
    private bool _wasGrounded;
    private VisualState _state;

    public string CurrentAnimationState => GetDisplayState();
    public float AnimationSpeed => _smoothedSpeed;
    public bool AnimationGrounded { get; private set; }
    public float AnimationVerticalVelocity { get; private set; }

    private enum VisualState
    {
        Locomotion,
        RunToStop,
        Jump,
        Fall,
        Land
    }

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _previousHeight = transform.position.y;
        _wasGrounded = _characterController != null && _characterController.isGrounded;

        if (_modelPrefab == null)
        {
            SetFallbackActive(true);
            return;
        }

        GameObject model = Instantiate(_modelPrefab, transform);
        model.name = "KageyamaAnimatedVisual";
        model.transform.localPosition = _localPosition;
        model.transform.localRotation = Quaternion.Euler(_localEulerAngles);
        model.transform.localScale = _localScale;

        _animator = model.GetComponentInChildren<Animator>();
        if (_animator != null)
        {
            _animator.enabled = true;
            _animator.speed = 1f;
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _animator.runtimeAnimatorController = _animatorController;

            if (_animatorController != null)
            {
                _animator.Play("Locomotion", 0, 0f);
                _animator.Update(0f);
            }

            StartCoroutine(ValidateBoneMovement(_animator));
        }

        SetFallbackActive(false);
        LogAnimationSetup(_animator, model);
    }

    private void Update()
    {
        if (_animator == null || _characterController == null)
        {
            return;
        }

        Vector3 velocity = _characterController.velocity;
        float horizontalSpeed = new Vector2(velocity.x, velocity.z).magnitude;
        _smoothedSpeed = Mathf.Lerp(
            _smoothedSpeed,
            horizontalSpeed,
            1f - Mathf.Exp(-_speedSmoothing * Time.deltaTime));

        AnimationGrounded = _characterController.isGrounded;
        AnimationVerticalVelocity = Time.deltaTime > 0f
            ? (transform.position.y - _previousHeight) / Time.deltaTime
            : 0f;

        bool landedThisFrame = !_wasGrounded && AnimationGrounded;
        if (horizontalSpeed >= _runThreshold)
        {
            _lastRunTime = Time.time;
        }

        bool stoppedFromRun = AnimationGrounded &&
                              Time.time - _lastRunTime <= 0.5f &&
                              _previousSpeed > _idleThreshold &&
                              horizontalSpeed <= _idleThreshold;
        bool movementResumed = horizontalSpeed > _idleThreshold;

        _animator.SetFloat(SpeedHash, _smoothedSpeed);
        _animator.SetBool(IsGroundedHash, AnimationGrounded);
        _animator.SetFloat(VerticalVelocityHash, AnimationVerticalVelocity);
        _animator.SetBool(IsStoppingHash, stoppedFromRun);

        UpdateVisualState(landedThisFrame, stoppedFromRun, movementResumed);

        _previousSpeed = horizontalSpeed;
        _previousHeight = transform.position.y;
        _wasGrounded = AnimationGrounded;
    }

    private void UpdateVisualState(bool landedThisFrame, bool stoppedFromRun, bool movementResumed)
    {
        if (!AnimationGrounded)
        {
            SetState(AnimationVerticalVelocity > _verticalThreshold
                ? VisualState.Jump
                : VisualState.Fall, 0.05f);
            return;
        }

        if (landedThisFrame)
        {
            SetTimedState(VisualState.Land, _landingDuration, 0.05f);
            return;
        }

        if ((_state == VisualState.Land || _state == VisualState.RunToStop) &&
            Time.time < _stateEndTime &&
            !movementResumed)
        {
            return;
        }

        if (stoppedFromRun)
        {
            _lastRunTime = float.NegativeInfinity;
            SetTimedState(VisualState.RunToStop, _runToStopDuration, 0.05f);
            return;
        }

        SetState(VisualState.Locomotion, 0.08f);
    }

    private void SetTimedState(VisualState state, float duration, float transitionDuration)
    {
        SetState(state, transitionDuration);
        _stateEndTime = Time.time + duration;
    }

    private void SetState(VisualState state, float transitionDuration)
    {
        if (_state == state)
        {
            return;
        }

        _state = state;
        _animator.CrossFade(state.ToString(), transitionDuration, 0);
    }

    private string GetDisplayState()
    {
        if (_state != VisualState.Locomotion)
        {
            return _state.ToString();
        }

        if (_smoothedSpeed <= _idleThreshold)
        {
            return "Idle";
        }

        return _smoothedSpeed < _runThreshold ? "Walk" : "Run";
    }

    private IEnumerator ValidateBoneMovement(Animator animator)
    {
        Transform testBone = animator != null && animator.isHuman
            ? animator.GetBoneTransform(HumanBodyBones.LeftUpperArm)
            : null;
        Quaternion initialRotation = testBone != null
            ? testBone.localRotation
            : Quaternion.identity;

        yield return new WaitForSeconds(0.1f);

        bool boneChanged = testBone != null &&
            Quaternion.Angle(initialRotation, testBone.localRotation) > 0.1f;
        Debug.Log($"PLAYER ANIMATION DEBUG | Bone: LeftUpperArm | Changed: {boneChanged}");
    }

    private void LogAnimationSetup(Animator animator, GameObject model)
    {
        bool animatedRendererActive = false;
        Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            animatedRendererActive |= renderer.enabled && renderer.gameObject.activeInHierarchy;
        }

        string avatarStatus = animator != null && animator.avatar != null
            ? $"{animator.avatar.name} (valid: {animator.avatar.isValid}, human: {animator.avatar.isHuman})"
            : "null";
        string controllerName = animator != null && animator.runtimeAnimatorController != null
            ? animator.runtimeAnimatorController.name
            : "null";
        string clipName = _defaultAnimation != null ? _defaultAnimation.name : "null";

        Debug.Log(
            "PLAYER ANIMATION DEBUG\n" +
            $"Animator found: {animator != null}\n" +
            $"Animator GameObject: {(animator != null ? animator.gameObject.name : "none")}\n" +
            $"Animator enabled: {(animator != null && animator.enabled)}\n" +
            $"Animator speed: {(animator != null ? animator.speed : 0f)}\n" +
            $"Avatar: {avatarStatus}\n" +
            $"Controller: {controllerName}\n" +
            $"Current clip: {clipName}\n" +
            "State: Locomotion\n" +
            $"Static fallback active: {(_staticFallback != null && _staticFallback.activeSelf)}\n" +
            $"Animated renderer active: {animatedRendererActive}");
    }

    private void SetFallbackActive(bool active)
    {
        if (_staticFallback != null)
        {
            _staticFallback.SetActive(active);
        }
    }
}
