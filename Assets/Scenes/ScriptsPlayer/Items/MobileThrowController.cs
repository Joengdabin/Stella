using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class MobileThrowController : MonoBehaviour
{
    [Header("Refs (auto if empty)")]
    [SerializeField] private FoodThrowSystem throwSystem;
    [SerializeField] private SkyOrbitCamera skyCam;
    [SerializeField] private PlayerLantern lantern;

    [Header("Screen Regions (Start only)")]
    [Range(0.1f, 0.9f)]
    [SerializeField] private float rightRegionStartNormalizedX = 0.5f;

    [Header("Tap / LongPress")]
    [SerializeField] private float longPressTime = 0.25f;
    [SerializeField] private float moveCancelThresholdPixels = 18f;

    [Header("Aim Look")]
    [SerializeField] private bool allowLookWhileAiming = true;

    private bool pressing;
    private bool aiming;
    private int fingerId = -1;
    private float pressStartTime;
    private Vector2 pressStartPos;
    private bool mouseStartedInRightRegion;

    void Awake()
    {
        if (!throwSystem) throwSystem = GetComponent<FoodThrowSystem>();

        if (!skyCam)
        {
            var c = Camera.main;
            if (c) skyCam = c.GetComponent<SkyOrbitCamera>();
        }

        // ✅ 자동으로 "Lantern 오브젝트에 붙은 PlayerLantern" 찾기
        if (!lantern)
        {
            lantern = FindFirstObjectByType<PlayerLantern>();
        }
    }

    bool IsInRightRegion(Vector2 screenPos)
    {
        float nx = screenPos.x / Screen.width;
        return nx >= rightRegionStartNormalizedX;
    }

    bool IsPointerOverUI(int fid)
    {
        if (EventSystem.current == null) return false;
        return EventSystem.current.IsPointerOverGameObject(fid);
    }

    void Update()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseStartRight_DragAnywhere();
#else
        HandleTouchRightRegion();
#endif
    }

    void HandleTouchRightRegion()
    {
        if (Input.touchCount <= 0) return;

        if (pressing)
        {
            Touch t = default;
            bool found = false;

            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).fingerId == fingerId)
                {
                    t = Input.GetTouch(i);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                ResetState();
                return;
            }

            if (t.phase == TouchPhase.Moved || t.phase == TouchPhase.Stationary)
            {
                float held = Time.unscaledTime - pressStartTime;

                if (!aiming && held >= longPressTime)
                {
                    aiming = true;
                    throwSystem.BeginAim();
                }

                if (aiming)
                {
                    throwSystem.UpdateAim();

                    if (allowLookWhileAiming && skyCam != null && t.phase == TouchPhase.Moved)
                        skyCam.AddLookDelta(t.deltaPosition);
                }
            }

            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                float held = Time.unscaledTime - pressStartTime;
                float moved = Vector2.Distance(t.position, pressStartPos);

                if (!aiming)
                {
                    if (held < longPressTime && moved <= moveCancelThresholdPixels)
                        throwSystem.Drop();
                }
                else
                {
                    throwSystem.ReleaseThrow();
                }

                ResetState();
            }

            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase != TouchPhase.Began) continue;

            if (!IsInRightRegion(t.position)) continue;
            if (IsPointerOverUI(t.fingerId)) continue;

            // ✅ 등불을 누르는 중이면 던지기 입력 시작 금지(등불 우선)
            if (lantern != null && lantern.IsPressingLantern)
                continue;

            pressing = true;
            aiming = false;
            fingerId = t.fingerId;
            pressStartTime = Time.unscaledTime;
            pressStartPos = t.position;
            break;
        }
    }

    void HandleMouseStartRight_DragAnywhere()
    {
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 pos = Input.mousePosition;

        if (Input.GetMouseButtonDown(0))
        {
            mouseStartedInRightRegion = IsInRightRegion(pos);
            if (!mouseStartedInRightRegion) return;

            if (lantern != null && lantern.IsPressingLantern)
                return;

            pressing = true;
            aiming = false;
            pressStartTime = Time.unscaledTime;
            pressStartPos = pos;
        }

        if (!pressing) return;

        if (Input.GetMouseButton(0))
        {
            float held = Time.unscaledTime - pressStartTime;

            if (!aiming && held >= longPressTime)
            {
                aiming = true;
                throwSystem.BeginAim();
            }

            if (aiming)
            {
                throwSystem.UpdateAim();

                if (allowLookWhileAiming && skyCam != null)
                {
                    float mx = Input.GetAxisRaw("Mouse X");
                    float my = Input.GetAxisRaw("Mouse Y");
                    skyCam.AddLookDelta(new Vector2(mx, my) * 25f);
                }
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            float held = Time.unscaledTime - pressStartTime;
            float moved = Vector2.Distance((Vector2)Input.mousePosition, pressStartPos);

            if (!aiming)
            {
                if (held < longPressTime && moved <= moveCancelThresholdPixels)
                    throwSystem.Drop();
            }
            else
            {
                throwSystem.ReleaseThrow();
            }

            ResetState();
        }
    }

    void ResetState()
    {
        pressing = false;
        aiming = false;
        fingerId = -1;
        mouseStartedInRightRegion = false;
    }
}