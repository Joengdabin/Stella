using UnityEngine;

[DisallowMultipleComponent]
public class FoodThrowController : MonoBehaviour
{
    [SerializeField] private FoodThrowSystem throwSystem;

    [Header("Tap / LongPress")]
    [SerializeField] private float longPressTime = 0.25f;

    // ป๓ลย
    private bool pressing;
    private float pressStart;
    private bool aiming;

    private void Awake()
    {
        if (!throwSystem) throwSystem = GetComponent<FoodThrowSystem>();
    }

    private void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouse();
#else
        HandleTouch();
#endif
    }

    private void HandleMouse()
    {
        if (Input.GetMouseButtonDown(0))
        {
            pressing = true;
            aiming = false;
            pressStart = Time.unscaledTime;
        }

        if (!pressing) return;

        float held = Time.unscaledTime - pressStart;

        if (!aiming && held >= longPressTime)
        {
            aiming = true;
            throwSystem.BeginAim();
        }

        if (aiming)
            throwSystem.UpdateAim();

        if (Input.GetMouseButtonUp(0))
        {
            pressing = false;

            if (!aiming)
                throwSystem.Drop();
            else
                throwSystem.ReleaseThrow();

            aiming = false;
        }
    }

    private void HandleTouch()
    {
        if (Input.touchCount <= 0) return;

        var t = Input.GetTouch(0);

        if (t.phase == TouchPhase.Began)
        {
            pressing = true;
            aiming = false;
            pressStart = Time.unscaledTime;
        }

        if (!pressing) return;

        float held = Time.unscaledTime - pressStart;

        if (!aiming && held >= longPressTime)
        {
            aiming = true;
            throwSystem.BeginAim();
        }

        if (aiming)
            throwSystem.UpdateAim();

        if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
        {
            pressing = false;

            if (!aiming)
                throwSystem.Drop();
            else
                throwSystem.ReleaseThrow();

            aiming = false;
        }
    }
}