using UnityEngine;

[RequireComponent(typeof(VolleyballBall))]
public sealed class BallAudioFeedback : MonoBehaviour
{
    [SerializeField, Range(0f, 1f)] private float _actionVolume = 0.55f;
    [SerializeField, Range(0f, 1f)] private float _collisionVolume = 0.35f;
    [SerializeField, Min(0f)] private float _minimumCollisionSpeed = 1.5f;
    [SerializeField, Min(0f)] private float _repeatCooldown = 0.06f;

    private AudioSource _audioSource;
    private AudioClip _softContactClip;
    private AudioClip _hardContactClip;
    private float _lastPlayedTime = float.NegativeInfinity;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1f;
        _audioSource.minDistance = 2f;
        _audioSource.maxDistance = 24f;
        _audioSource.dopplerLevel = 0.2f;

        _softContactClip = CreateContactClip("Ball Soft Contact", 135f, 0.075f, 0.18f);
        _hardContactClip = CreateContactClip("Ball Hard Contact", 105f, 0.1f, 0.32f);
    }

    private void OnEnable()
    {
        ActionFeedbackController.FeedbackPlayed += HandleActionFeedback;
    }

    private void OnDisable()
    {
        ActionFeedbackController.FeedbackPlayed -= HandleActionFeedback;
    }

    private void OnCollisionEnter(Collision collision)
    {
        float impactSpeed = collision.relativeVelocity.magnitude;
        if (impactSpeed < _minimumCollisionSpeed)
        {
            return;
        }

        float strength = Mathf.InverseLerp(
            _minimumCollisionSpeed,
            14f,
            impactSpeed);
        PlayContact(
            strength > 0.55f ? _hardContactClip : _softContactClip,
            _collisionVolume * Mathf.Lerp(0.45f, 1f, strength),
            Mathf.Lerp(1.08f, 0.9f, strength));
    }

    private void HandleActionFeedback(ActionFeedbackType type, Vector3 position)
    {
        bool strong = type == ActionFeedbackType.Spike ||
                      type == ActionFeedbackType.Block ||
                      type == ActionFeedbackType.Serve ||
                      type == ActionFeedbackType.AIAttack ||
                      type == ActionFeedbackType.AIServe;

        PlayContact(
            strong ? _hardContactClip : _softContactClip,
            strong ? _actionVolume : _actionVolume * 0.72f,
            strong ? Random.Range(0.92f, 1.02f) : Random.Range(1.02f, 1.12f));
    }

    private void PlayContact(AudioClip clip, float volume, float pitch)
    {
        if (_audioSource == null || clip == null ||
            Time.unscaledTime - _lastPlayedTime < _repeatCooldown)
        {
            return;
        }

        _audioSource.pitch = pitch;
        _audioSource.PlayOneShot(clip, volume);
        _lastPlayedTime = Time.unscaledTime;
    }

    private static AudioClip CreateContactClip(
        string clipName,
        float frequency,
        float duration,
        float noiseAmount)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int index = 0; index < sampleCount; index++)
        {
            float time = index / (float)sampleRate;
            float progress = index / (float)sampleCount;
            float envelope = Mathf.Pow(1f - progress, 3f);
            float tone = Mathf.Sin(2f * Mathf.PI * frequency * time);
            float noise = Random.Range(-1f, 1f) * noiseAmount;
            samples[index] = (tone * 0.7f + noise) * envelope;
        }

        AudioClip clip = AudioClip.Create(
            clipName,
            sampleCount,
            1,
            sampleRate,
            false);
        clip.SetData(samples, 0);
        return clip;
    }
}
