using UnityEngine;

[DisallowMultipleComponent]
public class PlayerStimulusSource : MonoBehaviour
{
    [Header("Optional References (auto if empty)")]
    public Light lanternLight;

    [Header("Tuning (Speed thresholds, m/s)")]
    public float walkSpeedThreshold = 0.2f;
    public float runSpeedThreshold = 2.2f;
    public float suddenStopThreshold = 0.3f;

    [Header("Tuning (Light shock)")]
    public float lightShockThreshold = 0.35f;

    [Header("Tuning (Hand action)")]
    public float handActionHoldSeconds = 0.35f;

    [Header("Noise Memory (Attack/Release)")]
    public float noiseAttack = 4.0f;
    public float noiseRelease = 0.7f;

    [Header("Runtime (Read Only)")]
    [SerializeField] float currentSpeed;
    [SerializeField] float lightIntensity01;
    [SerializeField] float noiseLevel01;
    [SerializeField] string noiseLevelName = "Silent";

    // ✅ 외부에서 읽을 수 있게 공개 (RabbitPerception/EmotionModel이 씀)
    public bool LightShock { get; private set; }
    public bool JustMadeLoudNoise { get; private set; }
    public bool JustSuddenStop { get; private set; }

    // 내부 상태
    Vector3 _prevPos;
    float _quietTimer;
    float _handActionTimer;
    float _lightShockTimer;

    void Awake()
    {
        if (!lanternLight)
        {
            // 씬에 LanternLight 라는 이름이 있으면 잡아주고, 없으면 그냥 null이어도 됨
            var go = GameObject.Find("LanternLight");
            if (go) lanternLight = go.GetComponent<Light>();
        }

        _prevPos = transform.position;
    }

    void Update()
    {
        // 1) 속도 계산(가장 안정적): 위치 변화 기반 (CharacterController.velocity 의존 X)
        var pos = transform.position;
        var vel = (pos - _prevPos) / Mathf.Max(Time.deltaTime, 0.0001f);
        _prevPos = pos;

        // 수평 속도만
        vel.y = 0f;
        float newSpeed = vel.magnitude;

        // 2) 갑자기 멈춤 감지
        JustSuddenStop = (currentSpeed >= runSpeedThreshold && newSpeed <= suddenStopThreshold);

        currentSpeed = newSpeed;

        // 3) 빛 강도(없으면 0)
        lightIntensity01 = lanternLight ? Mathf.Clamp01(lanternLight.intensity) : 0f;

        // 4) LightShock 감지(빛이 임계 이상이면 잠깐 true)
        if (lightIntensity01 >= lightShockThreshold)
        {
            _lightShockTimer = 0.15f; // 짧게 펄스
        }
        if (_lightShockTimer > 0f) _lightShockTimer -= Time.deltaTime;
        LightShock = _lightShockTimer > 0f;

        // 5) LoudNoise 감지(달리는 중이거나 갑자기 멈춤이면 잠깐 true)
        if (currentSpeed >= runSpeedThreshold || JustSuddenStop)
        {
            // 달리기/급정지 이벤트는 순간 자극으로 처리
            JustMadeLoudNoise = true;
        }
        else
        {
            JustMadeLoudNoise = false;
        }

        // 6) 노이즈 레벨 계산(0..1)
        // - 걷기/달리기 속도 기반 + 빛 쇼크 가중치
        float speed01 = Mathf.InverseLerp(walkSpeedThreshold, runSpeedThreshold, currentSpeed);
        float light01 = LightShock ? 1f : 0f;
        float now = Mathf.Clamp01(Mathf.Max(speed01, light01));

        // Attack/Release로 부드럽게 메모리
        if (now > noiseLevel01) noiseLevel01 = Mathf.MoveTowards(noiseLevel01, now, noiseAttack * Time.deltaTime);
        else noiseLevel01 = Mathf.MoveTowards(noiseLevel01, now, noiseRelease * Time.deltaTime);

        // 이름(디버그용)
        noiseLevelName =
            noiseLevel01 < 0.1f ? "Silent" :
            noiseLevel01 < 0.4f ? "Walk" :
            noiseLevel01 < 0.8f ? "Run" : "Panic";
    }

    /// <summary>현재 프레임 기준 자극 강도(0..1)</summary>
    public float NoiseNow01()
    {
        // 지금 순간값이 필요하면 noiseLevel01(메모리)로 반환
        return noiseLevel01;
    }

    /// <summary>
    /// 조용한 상태인지(최근 hold초 동안 noise가 threshold 아래였으면 true)
    /// </summary>
    public bool IsQuiet(float threshold01 = 0.2f, float holdSeconds = 0.25f)
    {
        if (noiseLevel01 <= threshold01) _quietTimer += Time.deltaTime;
        else _quietTimer = 0f;

        return _quietTimer >= holdSeconds;
    }

    /// <summary>손 액션 같은 “위협 행동”을 외부에서 호출하고 싶을 때 사용(선택)</summary>
    public void PulseHandAction()
    {
        _handActionTimer = handActionHoldSeconds;
    }

    public bool HandActionActive => _handActionTimer > 0f;

    void LateUpdate()
    {
        if (_handActionTimer > 0f) _handActionTimer -= Time.deltaTime;
    }
}
