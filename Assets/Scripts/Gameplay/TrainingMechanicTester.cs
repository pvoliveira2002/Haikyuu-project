using UnityEngine;

public sealed class TrainingMechanicTester : MonoBehaviour
{
    [SerializeField] private VolleyballBall _ball;
    [SerializeField] private Transform _setTestPoint;
    [SerializeField] private Transform _spikeTestPoint;
    [SerializeField] private Transform _ballResetPoint;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            PrepareBall(_setTestPoint);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            PrepareBall(_spikeTestPoint);
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            PrepareBall(_ballResetPoint);
        }
    }

    private void PrepareBall(Transform targetPoint)
    {
        if (_ball == null || targetPoint == null)
        {
            return;
        }

        _ball.ResetPosition(targetPoint.position);
    }
}
