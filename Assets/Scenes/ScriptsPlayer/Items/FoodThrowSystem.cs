using UnityEngine;

[DisallowMultipleComponent]
public class FoodThrowSystem : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private FoodQuickSlot quickSlot;
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private Camera cam;

    [Header("Prefabs")]
    [SerializeField] private GameObject foodWorldPrefab;
    [SerializeField] private GameObject heldVisualPrefab;

    [Header("Landing Indicator")]
    [Tooltip("착지 위치를 보여주는 프리팹(Quad/Decal/Projector 등)")]
    [SerializeField] private GameObject landingIndicatorPrefab;
    [SerializeField] private float indicatorYOffset = 0.02f;
    [SerializeField] private bool indicatorOnlyWhileAiming = true;

    [Header("Drop Settings")]
    [SerializeField] private float dropForwardOffset = 0.6f;
    [SerializeField] private float dropUpOffset = 0.2f;
    [SerializeField] private float dropForwardImpulse = 1.2f;

    [Header("Throw Charge (Max distance tuning)")]
    [SerializeField] private float minThrowSpeed = 8f;
    [SerializeField] private float maxThrowSpeed = 22f;
    [SerializeField] private float chargeTime = 0.9f;

    [Header("Trajectory")]
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private LayerMask trajectoryHitMask = ~0;
    [SerializeField] private float simMaxTime = 4.5f;
    [SerializeField] private float simTimeStep = 0.03f;
    [SerializeField] private float lineWidth = 0.05f;

    [Header("Aim Visual Offset")]
    [SerializeField] private float startForwardNudge = 0.15f;

    [Header("Debug")]
    [SerializeField] private bool logDebug = false;

    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private SkyOrbitCamera skyCam;

    private bool _aiming;
    private float _aimStartTime;
    private float _currentSpeed;

    private GameObject _heldVisualInstance;
    private GameObject _indicatorInstance;
    private bool _hasLandingPoint;
    private Vector3 _landingPoint;

    public bool IsAiming => _aiming;
    public float CurrentThrowSpeed => _currentSpeed;

    private void Awake()
    {
        if (!quickSlot) quickSlot = GetComponent<FoodQuickSlot>();
        if (!cam) cam = Camera.main;
        if (!skyCam && cam) skyCam = cam.GetComponent<SkyOrbitCamera>();

        SetupLineRenderer();
        RefreshHeldVisual();
        EnsureIndicator();
        SetIndicatorVisible(false);
    }

    private void Update()
    {
        RefreshHeldVisual();

        if (quickSlot == null || quickSlot.HandEmpty)
        {
            EndAimInternal();
        }

        // 조준 중 아니면 인디케이터 숨김(옵션)
        if (indicatorOnlyWhileAiming && !_aiming)
            SetIndicatorVisible(false);
    }

    // ======================
    // Public Commands
    // ======================

    public void BeginAim()
    {
        if (quickSlot == null || quickSlot.HandEmpty) return;
        if (!throwOrigin || !cam) return;

        _aiming = true;
        _aimStartTime = Time.unscaledTime;
        _currentSpeed = minThrowSpeed;

        if (logDebug) Debug.Log("[Throw] BeginAim");
        ShowTrajectory(_currentSpeed);
        if (skyCam) skyCam.SetAimMode(true);
    }

    public void UpdateAim()
    {
        if (!_aiming) return;
        if (quickSlot == null || quickSlot.HandEmpty) { EndAimInternal(); return; }

        float held = Time.unscaledTime - _aimStartTime;
        float t = Mathf.Clamp01(held / Mathf.Max(0.01f, chargeTime));
        _currentSpeed = Mathf.Lerp(minThrowSpeed, maxThrowSpeed, t);

        ShowTrajectory(_currentSpeed);
    }

    public void ReleaseThrow()
    {
        if (!_aiming) return;
        if (quickSlot == null || quickSlot.HandEmpty) { EndAimInternal(); return; }

        float speed = _currentSpeed;
        EndAimInternal();

        ThrowOne(speed);
    }

    public void Drop()
    {
        if (quickSlot == null || quickSlot.HandEmpty) return;

        if (!quickSlot.TryConsumeFromHand(1)) return;

        Vector3 pos = transform.position + transform.forward * dropForwardOffset + Vector3.up * dropUpOffset;
        var go = Instantiate(foodWorldPrefab, pos, Quaternion.identity);

        var rb = go.GetComponent<Rigidbody>();
        if (rb) rb.linearVelocity = transform.forward * dropForwardImpulse;

        if (logDebug) Debug.Log("[Throw] Drop 1");
    }

    public void CancelAim()
    {
        EndAimInternal();
    }

    // ======================
    // Internals
    // ======================

    private void ThrowOne(float speed)
    {
        if (!throwOrigin || !cam) return;
        if (!foodWorldPrefab) return;

        if (!quickSlot.TryConsumeFromHand(1)) return;

        Vector3 start = throwOrigin.position + cam.transform.forward * startForwardNudge;
        var go = Instantiate(foodWorldPrefab, start, Quaternion.identity);

        var rb = go.GetComponent<Rigidbody>();
        if (!rb) rb = go.AddComponent<Rigidbody>();

        rb.linearVelocity = cam.transform.forward * speed;

        if (logDebug) Debug.Log($"[Throw] Throw 1 speed={speed:0.0}");
    }

    private void EndAimInternal()
    {
        if (skyCam) skyCam.SetAimMode(false);
        _aiming = false;
        HideTrajectory();
        SetIndicatorVisible(false);
    }

    private void SetupLineRenderer()
    {
        if (!lineRenderer) return;

        lineRenderer.useWorldSpace = true;
        lineRenderer.widthMultiplier = lineWidth;
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
    }

    private void EnsureIndicator()
    {
        if (_indicatorInstance) return;
        if (!landingIndicatorPrefab) return;

        _indicatorInstance = Instantiate(landingIndicatorPrefab);
        _indicatorInstance.name = "[LandingIndicatorInstance]";
    }

    private void SetIndicatorVisible(bool visible)
    {
        if (!_indicatorInstance) return;
        _indicatorInstance.SetActive(visible);
    }

    private void UpdateIndicator(Vector3 point)
    {
        EnsureIndicator();
        if (!_indicatorInstance) return;

        // ✅ point에서 아래로 쏴서 바닥에만 고정
        if (Physics.Raycast(point + Vector3.up * 2f, Vector3.down, out RaycastHit hit, 10f, groundMask, QueryTriggerInteraction.Ignore))
        {
            _indicatorInstance.transform.position = new Vector3(hit.point.x, hit.point.y + indicatorYOffset, hit.point.z);
            _indicatorInstance.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            SetIndicatorVisible(true);
        }
        else
        {
            // 바닥 못 찾으면 숨김
            SetIndicatorVisible(false);
        }
    }

    private void ShowTrajectory(float speed)
    {
        if (!lineRenderer || !throwOrigin || !cam) return;

        lineRenderer.enabled = true;

        Vector3 start = throwOrigin.position + cam.transform.forward * startForwardNudge;
        Vector3 v0 = cam.transform.forward * speed;
        Vector3 g = Physics.gravity;

        int steps = Mathf.CeilToInt(simMaxTime / Mathf.Max(0.01f, simTimeStep));
        if (steps < 2) steps = 2;

        Vector3 prev = start;
        Vector3[] pts = new Vector3[steps + 1];
        int count = 0;

        pts[count++] = start;

        _hasLandingPoint = false;

        for (int i = 1; i <= steps; i++)
        {
            float t = i * simTimeStep;
            Vector3 p = start + v0 * t + 0.5f * g * t * t;

            Vector3 seg = p - prev;
            float len = seg.magnitude;

            if (len > 0.0001f)
            {
                if (Physics.Raycast(prev, seg / len, out RaycastHit hit, len, trajectoryHitMask, QueryTriggerInteraction.Ignore))
                {
                    pts[count++] = hit.point;
                    _hasLandingPoint = true;
                    _landingPoint = hit.point;
                    break;
                }
            }

            pts[count++] = p;
            prev = p;
        }

        lineRenderer.positionCount = count;
        for (int i = 0; i < count; i++)
            lineRenderer.SetPosition(i, pts[i]);

        // ✅ 착지 인디케이터
        if (_hasLandingPoint)
            UpdateIndicator(_landingPoint);
        else
            SetIndicatorVisible(false);
    }

    private void HideTrajectory()
    {
        if (!lineRenderer) return;
        lineRenderer.enabled = false;
        lineRenderer.positionCount = 0;
    }

    private void RefreshHeldVisual()
    {
        if (quickSlot == null || quickSlot.HandEmpty)
        {
            if (_heldVisualInstance)
            {
                Destroy(_heldVisualInstance);
                _heldVisualInstance = null;
            }
            return;
        }

        if (!heldVisualPrefab || !throwOrigin) return;

        if (_heldVisualInstance == null)
        {
            _heldVisualInstance = Instantiate(heldVisualPrefab, throwOrigin);
            _heldVisualInstance.transform.localPosition = Vector3.zero;
            _heldVisualInstance.transform.localRotation = Quaternion.identity;

            var rb = _heldVisualInstance.GetComponent<Rigidbody>();
            if (rb) Destroy(rb);
            var col = _heldVisualInstance.GetComponent<Collider>();
            if (col) Destroy(col);
        }
    }
}