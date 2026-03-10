using UnityEngine;

[DisallowMultipleComponent]
public class SkyOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform Target;
    public Vector3 TargetOffset = new Vector3(0f, 1.7f, 0f);

    [Header("Distance (Zoom)")]
    public float Distance = 4.5f;
    public float MinDistance = 2f;
    public float MaxDistance = 8f;
    public float ZoomSpeedMouseWheel = 2f;
    public float ZoomSmoothing = 14f;

    [Header("Rotation")]
    public float Yaw = 0f;
    public float Pitch = 18f;
    public float PitchMin = -35f;
    public float PitchMax = 80f;
    public float MouseLookSensitivity = 2f;
    public float RotationSmoothing = 18f;

    [Header("Follow")]
    public float FollowSmoothing = 16f;

    [Header("PC Input")]
    [Tooltip("0=Left, 1=Right, 2=Middle")]
    public int RotateMouseButton = 1;
    public bool InvertY = false;

    [Header("Mobile Input (optional)")]
    public SimpleTouchInput TouchInput; // 있으면 연결

    [Header("Collision")]
    public bool EnableCollision = true;
    public float CollisionRadius = 0.25f;
    public float CollisionPadding = 0.15f;
    public LayerMask CollisionMask = ~0;

    [Header("Aim Mode")]
    public bool EnableAimMode = true;
    public float AimDistance = 2.6f;              // 조준 시 줌(거리)
    public float AimFov = 50f;                    // 조준 시 FOV (0이면 변경 안함)
    [Range(0.1f, 1f)] public float AimSensitivityMul = 0.55f;
    public float AimBlendSpeed = 10f;

    [Header("Aim Offset")]
    public Vector3 AimTargetOffset = new Vector3(0f, 1.6f, 0f);

    // internal
    private Vector3 _followVel;
    private float _desiredDistance;
    private float _distanceVel;
    private float _yawVel;
    private float _pitchVel;

    private Camera _cam;

    // aim state
    private bool _aimOn;
    private float _baseDistance;
    private float _baseFov;
    private float _baseSensitivity;

    private void Awake()
    {
        _cam = GetComponent<Camera>();
        if (!_cam) _cam = Camera.main;

        _desiredDistance = Distance;

        _baseDistance = Distance;
        _baseSensitivity = MouseLookSensitivity;
        _baseFov = _cam ? _cam.fieldOfView : 60f;
    }

    private void LateUpdate()
    {
        if (!Target) return;

        // 1) Input (PC 기본 입력만)
        Vector2 lookDelta = ReadLookDelta();
        float zoomDelta = ReadZoomDelta();

        if (lookDelta.sqrMagnitude > 0f)
        {
            float invY = InvertY ? -1f : 1f;
            Yaw += lookDelta.x * MouseLookSensitivity;
            Pitch += lookDelta.y * MouseLookSensitivity * invY;
            Pitch = Mathf.Clamp(Pitch, PitchMin, PitchMax);
        }

        if (!_aimOn && Mathf.Abs(zoomDelta) > 0.0001f)
        {
            _desiredDistance -= zoomDelta * ZoomSpeedMouseWheel;
            _desiredDistance = Mathf.Clamp(_desiredDistance, MinDistance, MaxDistance);
        }

        // 2) Aim Mode blend
        ApplyAimMode();

        // 3) Smooth rotation
        float smoothYaw = Mathf.SmoothDampAngle(transform.eulerAngles.y, Yaw, ref _yawVel, 1f / Mathf.Max(1f, RotationSmoothing));
        float smoothPitch = Mathf.SmoothDampAngle(transform.eulerAngles.x, Pitch, ref _pitchVel, 1f / Mathf.Max(1f, RotationSmoothing));
        Quaternion rot = Quaternion.Euler(smoothPitch, smoothYaw, 0f);

        // 4) target position
        Vector3 offset = _aimOn ? AimTargetOffset : TargetOffset;
        Vector3 targetPos = Target.position + offset;

        // 5) Smooth distance
        float dist = Mathf.SmoothDamp(Distance, _desiredDistance, ref _distanceVel, 1f / Mathf.Max(1f, ZoomSmoothing));
        dist = Mathf.Clamp(dist, MinDistance, MaxDistance);

        Vector3 desiredCamPos = targetPos - rot * Vector3.forward * dist;

        // 6) Collision
        if (EnableCollision)
        {
            Vector3 dir = (desiredCamPos - targetPos);
            float len = dir.magnitude;
            if (len > 0.0001f)
            {
                dir /= len;
                float maxLen = Mathf.Max(0f, len - CollisionPadding);

                if (Physics.SphereCast(targetPos, CollisionRadius, dir, out RaycastHit hit, maxLen, CollisionMask, QueryTriggerInteraction.Ignore))
                {
                    float hitDist = Mathf.Max(0f, hit.distance - CollisionPadding);
                    desiredCamPos = targetPos + dir * hitDist;
                    dist = Vector3.Distance(targetPos, desiredCamPos);
                }
            }
        }

        // 7) Apply
        transform.position = desiredCamPos;
        transform.rotation = rot;

        Distance = dist;
    }

    // ✅ 외부에서 조준 중 회전 주입(모바일 ThrowController가 호출)
    public void AddLookDelta(Vector2 deltaPixels)
    {
        float invY = InvertY ? -1f : 1f;

        // 픽셀 → 회전값 변환 스케일 (원하면 인스펙터로 빼도 됨)
        const float pixelToRotation = 0.02f;

        Yaw += deltaPixels.x * MouseLookSensitivity * pixelToRotation;
        Pitch += deltaPixels.y * MouseLookSensitivity * pixelToRotation * invY;
        Pitch = Mathf.Clamp(Pitch, PitchMin, PitchMax);
    }

    public void SetAimMode(bool on)
    {
        if (!EnableAimMode) return;
        _aimOn = on;
    }

    private void ApplyAimMode()
    {
        if (!EnableAimMode) return;

        if (!_aimOn)
        {
            _baseDistance = Distance;
            _baseSensitivity = MouseLookSensitivity;
            if (_cam) _baseFov = _cam.fieldOfView;
        }

        float targetDist = _aimOn ? AimDistance : _baseDistance;
        float targetSens = _aimOn ? (_baseSensitivity * AimSensitivityMul) : _baseSensitivity;
        float targetFov = _aimOn ? AimFov : _baseFov;

        float a = 1f - Mathf.Exp(-AimBlendSpeed * Time.deltaTime);

        _desiredDistance = Mathf.Lerp(_desiredDistance, Mathf.Clamp(targetDist, MinDistance, MaxDistance), a);
        MouseLookSensitivity = Mathf.Lerp(MouseLookSensitivity, targetSens, a);

        if (_cam && AimFov > 0f)
            _cam.fieldOfView = Mathf.Lerp(_cam.fieldOfView, targetFov, a);
    }

    private Vector2 ReadLookDelta()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetMouseButton(RotateMouseButton))
        {
            float mx = Input.GetAxisRaw("Mouse X");
            float my = Input.GetAxisRaw("Mouse Y");
            return new Vector2(mx, my);
        }
        return Vector2.zero;
#else
        if (TouchInput != null)
            return TouchInput.LookDelta;
        return Vector2.zero;
#endif
    }

    private float ReadZoomDelta()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        return Input.mouseScrollDelta.y;
#else
        return 0f;
#endif
    }
}