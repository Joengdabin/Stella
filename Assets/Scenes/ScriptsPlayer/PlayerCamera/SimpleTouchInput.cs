using UnityEngine;

/// <summary>
/// Mobile touch helper:
/// - One finger drag => look delta
/// - Two finger pinch => zoom delta
/// Works alongside mouse input on PC (handled elsewhere).
/// </summary>
public class SimpleTouchInput : MonoBehaviour
{
    [Header("Drag")]
    [Tooltip("Touch drag sensitivity multiplier.")]
    public float dragSensitivity = 0.12f;

    [Header("Pinch")]
    [Tooltip("Pinch zoom sensitivity multiplier.")]
    public float pinchSensitivity = 0.01f;

    public bool IsDragging { get; private set; }
    public Vector2 DragDelta { get; private set; }     // per-frame delta (pixels scaled)
    public float PinchDelta { get; private set; }      // per-frame delta (positive = zoom out, negative = zoom in)

    private int _activeFingerId = -1;
    private Vector2 _lastPos;

    private float _lastPinchDist;

    void Update()
    {
        DragDelta = Vector2.zero;
        PinchDelta = 0f;
        IsDragging = false;

        if (Input.touchCount == 1)
        {
            Touch t = Input.GetTouch(0);

            if (t.phase == TouchPhase.Began)
            {
                _activeFingerId = t.fingerId;
                _lastPos = t.position;
            }

            if (t.fingerId == _activeFingerId &&
                (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary))
            {
                Vector2 cur = t.position;
                Vector2 rawDelta = cur - _lastPos;
                _lastPos = cur;

                DragDelta = rawDelta * dragSensitivity;
                IsDragging = rawDelta.sqrMagnitude > 0.01f;
            }

            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                _activeFingerId = -1;
            }
        }
        else if (Input.touchCount >= 2)
        {
            // Two finger pinch zoom
            Touch a = Input.GetTouch(0);
            Touch b = Input.GetTouch(1);

            float dist = Vector2.Distance(a.position, b.position);

            if (a.phase == TouchPhase.Began || b.phase == TouchPhase.Began)
            {
                _lastPinchDist = dist;
            }
            else
            {
                float delta = dist - _lastPinchDist;
                _lastPinchDist = dist;

                // Positive delta => fingers apart => zoom OUT (increase distance)
                PinchDelta = -delta * pinchSensitivity;
            }
        }
    }
}