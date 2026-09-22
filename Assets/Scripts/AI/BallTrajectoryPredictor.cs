using UnityEngine;

public sealed class BallTrajectoryPredictor : MonoBehaviour
{
    [SerializeField] private VolleyballBall _ball;
    [SerializeField] private Collider _courtFloor;
    [SerializeField, Min(0f)] private float _ballRadius = 0.21f;
    [SerializeField, Min(0f)] private float _predictionUpdateThreshold = 0.25f;
    [SerializeField, Min(0f)] private float _fastPredictionUpdateThreshold = 0.12f;
    [SerializeField, Min(0f)] private float _incomingSpeedThreshold = 9f;
    [SerializeField, Min(0f)] private float _urgentLandingTime = 1.2f;

    public Vector3 PredictedLandingPoint { get; private set; }
    public bool HasPrediction { get; private set; }
    public float TimeToLanding { get; private set; }
    public bool IsFastIncomingBall { get; private set; }
    public float IncomingSpeed => _ball != null ? _ball.Velocity.magnitude : 0f;

    private void Update()
    {
        UpdatePrediction();
    }

    private void UpdatePrediction()
    {
        bool hadPrediction = HasPrediction;
        HasPrediction = false;
        TimeToLanding = 0f;
        IsFastIncomingBall = false;

        if (_ball == null || _courtFloor == null)
        {
            return;
        }

        Vector3 position = _ball.transform.position;
        Vector3 velocity = _ball.Velocity;
        float gravity = Physics.gravity.y;
        float landingHeight = _courtFloor.bounds.max.y + _ballRadius;
        float discriminant = velocity.y * velocity.y -
            2f * gravity * (position.y - landingHeight);

        if (gravity >= 0f || discriminant < 0f)
        {
            return;
        }

        float squareRoot = Mathf.Sqrt(discriminant);
        float firstTime = (-velocity.y + squareRoot) / gravity;
        float secondTime = (-velocity.y - squareRoot) / gravity;
        float landingTime = SelectPositiveTime(firstTime, secondTime);

        if (landingTime <= 0f)
        {
            return;
        }

        Vector3 newLandingPoint = new Vector3(
            position.x + velocity.x * landingTime,
            landingHeight,
            position.z + velocity.z * landingTime);

        IsFastIncomingBall = velocity.magnitude >= _incomingSpeedThreshold &&
            landingTime <= _urgentLandingTime;
        float updateThreshold = IsFastIncomingBall
            ? _fastPredictionUpdateThreshold
            : _predictionUpdateThreshold;

        if (!hadPrediction ||
            Vector3.Distance(PredictedLandingPoint, newLandingPoint) >=
            updateThreshold)
        {
            PredictedLandingPoint = newLandingPoint;
        }

        TimeToLanding = landingTime;
        HasPrediction = true;
    }

    private static float SelectPositiveTime(float firstTime, float secondTime)
    {
        const float MinimumPredictionTime = 0.01f;
        bool firstValid = firstTime > MinimumPredictionTime;
        bool secondValid = secondTime > MinimumPredictionTime;

        if (firstValid && secondValid)
        {
            return Mathf.Min(firstTime, secondTime);
        }

        if (firstValid)
        {
            return firstTime;
        }

        return secondValid ? secondTime : 0f;
    }

    private void OnDrawGizmosSelected()
    {
        if (!HasPrediction || _ball == null)
        {
            return;
        }

        Gizmos.color = Color.green;
        Gizmos.DrawLine(_ball.transform.position, PredictedLandingPoint);
        Gizmos.DrawWireSphere(PredictedLandingPoint, 0.25f);
    }
}
