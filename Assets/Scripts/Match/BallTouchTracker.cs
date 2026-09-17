using UnityEngine;

public sealed class BallTouchTracker : MonoBehaviour
{
    public bool HasLastTouch { get; private set; }
    public CourtSide LastTouch { get; private set; }

    public void RegisterTouch(CourtSide side)
    {
        LastTouch = side;
        HasLastTouch = true;
    }

    public void Clear()
    {
        HasLastTouch = false;
    }
}
