using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class PlayerLantern : MonoBehaviour
{
    [Header("Light Ref (auto if empty)")]
    [SerializeField] private Light lanternLight;

    [Header("Raycast (auto mask if empty)")]
    [SerializeField] private LayerMask lanternMask;
    [SerializeField] private float rayDistance = 10f;

    [Header("Hold to Change Level")]
    [SerializeField] private float holdTime = 0.35f;

    [Header("PC Fallback")]
    [SerializeField] private KeyCode keyToggle = KeyCode.Q;

    [Header("Brightness Levels")]
    [SerializeField] private float level1Intensity = 1.2f;
    [SerializeField] private float level2Intensity = 2.2f;
    [SerializeField] private float level3Intensity = 3.5f;

    [SerializeField] private float level1Range = 4f;
    [SerializeField] private float level2Range = 6f;
    [SerializeField] private float level3Range = 8f;

    [Header("Flicker / Sway (Visual)")]
    [SerializeField] private float idleFlickerIntensity = 0.05f;
    [SerializeField] private float moveFlickerIntensity = 0.18f;
    [SerializeField] private float flickerSpeed = 7f;
    [SerializeField] private float rangeWobble = 0.12f;
    [SerializeField] private float stopSwayDamping = 0.45f;

    [Header("Movement Source (auto if empty)")]
    [SerializeField] private CharacterController characterController;

    [Header("Debug")]
    [SerializeField] private bool logLevel = false;

    // runtime
    private int _level = 1;
    private float _pressTimer;
    private bool _pressing;
    private int _pressFingerId = -1;
    private bool _pressIsOnLantern;

    private Camera _cam;

    private float _baseIntensity;
    private float _baseRange;
    private float _moveAmountSmoothed;
    private float _stopSway;

    public bool IsPressingLantern => _pressing && _pressIsOnLantern;
    public int CurrentLevel => _level;

    void Awake()
    {
        // Auto camera
        _cam = Camera.main;

        // Auto light
        if (!lanternLight)
            lanternLight = GetComponentInChildren<Light>();

        // Auto movement source
        if (!characterController)
            characterController = GetComponentInParent<CharacterController>();

        // Auto mask if nothing selected
        if (lanternMask.value == 0)
        {
            int lanternLayer = LayerMask.NameToLayer("Lantern");
            if (lanternLayer >= 0)
                lanternMask = 1 << lanternLayer;
            else
                lanternMask = ~0; // fallback
        }

        ApplyLevel();
    }

    void Update()
    {
        if (Input.GetKeyDown(keyToggle))
            NextLevel();

#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseHold();
#else
        HandleTouchHold();
#endif

        ApplyFlickerAndSway();
    }

    void HandleMouseHold()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            _pressIsOnLantern = IsLanternHit(Input.mousePosition);
            _pressing = _pressIsOnLantern;
            _pressTimer = 0f;
        }

        if (_pressing)
        {
            if (!Input.GetMouseButton(0))
            {
                _pressing = false;
                _pressIsOnLantern = false;
                return;
            }

            _pressTimer += Time.deltaTime;
            if (_pressTimer >= holdTime)
            {
                NextLevel();
                _pressing = false;
                _pressIsOnLantern = false;
            }
        }

        if (Input.GetMouseButtonUp(0))
        {
            _pressing = false;
            _pressIsOnLantern = false;
        }
    }

    void HandleTouchHold()
    {
        if (Input.touchCount <= 0) return;

        if (_pressing)
        {
            bool found = false;
            Touch t = default;

            for (int i = 0; i < Input.touchCount; i++)
            {
                if (Input.GetTouch(i).fingerId == _pressFingerId)
                {
                    t = Input.GetTouch(i);
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                _pressing = false;
                _pressIsOnLantern = false;
                _pressFingerId = -1;
                return;
            }

            if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
            {
                _pressing = false;
                _pressIsOnLantern = false;
                _pressFingerId = -1;
                return;
            }

            _pressTimer += Time.deltaTime;
            if (_pressTimer >= holdTime)
            {
                NextLevel();
                _pressing = false;
                _pressIsOnLantern = false;
                _pressFingerId = -1;
            }

            return;
        }

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch t = Input.GetTouch(i);
            if (t.phase != TouchPhase.Began) continue;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.fingerId))
                continue;

            bool hit = IsLanternHit(t.position);
            if (!hit) continue;

            _pressing = true;
            _pressIsOnLantern = true;
            _pressFingerId = t.fingerId;
            _pressTimer = 0f;
            break;
        }
    }

    bool IsLanternHit(Vector2 screenPos)
    {
        if (!_cam) _cam = Camera.main;
        if (!_cam) return false;

        Ray ray = _cam.ScreenPointToRay(screenPos);
        return Physics.Raycast(ray, rayDistance, lanternMask, QueryTriggerInteraction.Ignore);
    }

    void NextLevel()
    {
        _level++;
        if (_level > 3) _level = 1;

        ApplyLevel();

        if (logLevel)
            Debug.Log($"[Lantern] Level={_level}");

        _stopSway = 1f;
    }

    void ApplyLevel()
    {
        if (!lanternLight) return;

        if (_level == 1)
        {
            _baseIntensity = level1Intensity;
            _baseRange = level1Range;
        }
        else if (_level == 2)
        {
            _baseIntensity = level2Intensity;
            _baseRange = level2Range;
        }
        else
        {
            _baseIntensity = level3Intensity;
            _baseRange = level3Range;
        }

        lanternLight.intensity = _baseIntensity;
        lanternLight.range = _baseRange;
    }

    void ApplyFlickerAndSway()
    {
        if (!lanternLight) return;

        float speed01 = 0f;
        if (characterController != null)
        {
            Vector3 v = characterController.velocity;
            v.y = 0f;
            float speed = v.magnitude;
            speed01 = Mathf.InverseLerp(0f, 6f, speed);
        }

        _moveAmountSmoothed = Mathf.Lerp(_moveAmountSmoothed, speed01, 1f - Mathf.Exp(-10f * Time.deltaTime));

        if (_moveAmountSmoothed < 0.05f)
            _stopSway = Mathf.Lerp(_stopSway, 0f, 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, stopSwayDamping)));
        else
            _stopSway = 1f;

        float amp = idleFlickerIntensity + moveFlickerIntensity * Mathf.Max(_moveAmountSmoothed, _stopSway * 0.35f);
        if (amp <= 0.0001f)
        {
            lanternLight.intensity = _baseIntensity;
            lanternLight.range = _baseRange;
            return;
        }

        float n = Mathf.PerlinNoise(Time.time * flickerSpeed, 0.123f);
        float centered = (n - 0.5f) * 2f;

        lanternLight.intensity = Mathf.Max(0f, _baseIntensity * (1f + centered * amp));
        lanternLight.range = Mathf.Max(0.1f, _baseRange * (1f + centered * rangeWobble * Mathf.Max(_moveAmountSmoothed, 0.2f)));
    }
}