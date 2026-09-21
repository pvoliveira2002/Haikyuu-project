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

    private void Awake()
    {
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

        Animator animator = model.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.enabled = true;
            animator.speed = 1f;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.runtimeAnimatorController = _animatorController;

            if (_animatorController != null)
            {
                animator.Play("IdleAnimated", 0, 0f);
                animator.Update(0f);
            }

            StartCoroutine(ValidateBoneMovement(animator));
        }

        SetFallbackActive(false);
        LogAnimationSetup(animator, model);
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
            "State: IdleAnimated\n" +
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
