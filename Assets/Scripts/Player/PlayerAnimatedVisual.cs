using UnityEngine;

public sealed class PlayerAnimatedVisual : MonoBehaviour
{
    [SerializeField] private GameObject _model;
    [SerializeField] private RuntimeAnimatorController _animatorController;
    [SerializeField] private Transform _visualRoot;
    [SerializeField] private GameObject _staticFallback;
    [SerializeField] private float _speedSmoothing = 12f;
    [SerializeField] private float _idleThreshold = 0.15f;
    [SerializeField] private float _runThreshold = 6.5f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int VerticalVelocityHash = Animator.StringToHash("VerticalVelocity");

    private CharacterController _characterController;
    private Animator _animator;
    private float _smoothedSpeed;
    private float _previousHeight;

    public string CurrentAnimationState => GetCurrentAnimationState();
    public float AnimationSpeed => _smoothedSpeed;
    public bool AnimationGrounded { get; private set; }
    public float AnimationVerticalVelocity { get; private set; }

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _previousHeight = transform.position.y;
        AnimationGrounded = _characterController != null && _characterController.isGrounded;

        if (_model == null || _visualRoot == null || !_model.transform.IsChildOf(_visualRoot))
        {
            SetFallbackActive(true);
            return;
        }

        _animator = _model.GetComponentInChildren<Animator>(true);
        if (_animator != null)
        {
            _animator.enabled = true;
            _animator.speed = 1f;
            _animator.applyRootMotion = false;
            _animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            _animator.runtimeAnimatorController = _animatorController;

            if (_animatorController != null)
            {
                _animator.Rebind();
                _animator.SetLayerWeight(0, 1f);
                _animator.SetFloat(SpeedHash, 0f);
                _animator.Play("Locomotion", 0, 0f);
                _animator.Update(0f);
            }

        }

        SetFallbackActive(false);
        LogAnimationSetup(_animator, _model);
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

        _animator.SetFloat(SpeedHash, _smoothedSpeed);
        _animator.SetBool(IsGroundedHash, AnimationGrounded);
        _animator.SetFloat(VerticalVelocityHash, AnimationVerticalVelocity);

        _previousHeight = transform.position.y;
    }

    private string GetCurrentAnimationState()
    {
        if (_animator == null)
        {
            return "None";
        }

        AnimatorStateInfo state = _animator.GetCurrentAnimatorStateInfo(0);
        if (state.IsName("Jump"))
        {
            return "Jump";
        }

        if (state.IsName("Fall"))
        {
            return "Fall";
        }

        if (state.IsName("Land"))
        {
            return "Land";
        }

        if (_smoothedSpeed <= _idleThreshold)
        {
            return "Idle";
        }

        return _smoothedSpeed < _runThreshold ? "Walk" : "Run";
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
        Debug.Log(
            "PLAYER ANIMATION DEBUG\n" +
            $"Animator found: {animator != null}\n" +
            $"Animator GameObject: {(animator != null ? animator.gameObject.name : "none")}\n" +
            $"Animator enabled: {(animator != null && animator.enabled)}\n" +
            $"Animator speed: {(animator != null ? animator.speed : 0f)}\n" +
            $"Avatar: {avatarStatus}\n" +
            $"Controller: {controllerName}\n" +
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
