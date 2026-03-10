using UnityEngine;
using UnityEngine.AI;

public class AnimalFoodEater : MonoBehaviour
{
    [Header("Refs (auto)")]
    public NavMeshAgent agent;
    public AnimalFoodSensor sensor;

    [Tooltip("있으면 위협/불안할 때 먹기 금지에 사용됨 (없어도 동작)")]
    public MonoBehaviour emotionModel; // AnimalEmotionModel 같은 것 (타입 의존 방지)

    [Header("Tuning")]
    public float eatDistance = 1.2f;            // 이 거리 안이면 먹기
    public float repathInterval = 0.35f;        // 목적지 갱신 주기
    public float minEatCooldown = 1.0f;         // 연속 먹기 방지
    public float ignoreIfAnxietyAbove = 8.0f;   // Anxiety가 이 값보다 높으면 먹이 무시 (감정모델 있을 때만)

    [Header("Debug")]
    public bool debugLog;

    AnimalFood _target;
    float _repathT;
    float _cooldownT;

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        if (!sensor) sensor = GetComponent<AnimalFoodSensor>();

        if (!emotionModel)
        {
            // 이름이 AnimalEmotionModel일 가능성이 높아서 자동 탐색 (없으면 그냥 null)
            emotionModel = GetComponent<MonoBehaviour>(); // 안전용 (원하면 아래 2줄로 바꿔도 됨)
            // emotionModel = GetComponent<AnimalEmotionModel>(); // 네 프로젝트에 타입이 확실하면 이걸로
        }
    }

    void Update()
    {
        if (!agent || !sensor) return;

        if (_cooldownT > 0f) _cooldownT -= Time.deltaTime;

        // 1) 위협/불안 상태면 먹이 행동 끔 (감정모델 있을 때만)
        if (IsTooAnxious())
        {
            ClearTarget();
            return;
        }

        // 2) 타겟이 없거나, 이미 먹혔으면 새로 찾기
        if (_target == null || _target.consumed)
        {
            TryAcquireTarget();
        }

        if (_target == null) return;

        // 3) 목적지 갱신
        _repathT += Time.deltaTime;
        if (_repathT >= repathInterval)
        {
            _repathT = 0f;
            if (agent.isOnNavMesh)
            {
                agent.isStopped = false;
                agent.SetDestination(_target.transform.position);
            }
        }

        // 4) 먹기 거리 체크
        float sqr = (transform.position - _target.transform.position).sqrMagnitude;
        if (sqr <= eatDistance * eatDistance)
        {
            TryEatTarget();
        }
    }

    void TryAcquireTarget()
    {
        if (_cooldownT > 0f) return;

        if (sensor.TryFindNearestFood(out var food))
        {
            _target = food;
            _repathT = repathInterval; // 바로 path 갱신하게
            if (debugLog) Debug.Log($"[FoodEater] target={food.name}");
        }
    }

    void TryEatTarget()
    {
        if (_target == null) return;
        if (_target.consumed) { ClearTarget(); return; }
        if (_cooldownT > 0f) return;

        // 먹기 실행
        _target.Consume();
        AnimalBondSystem bond = GetComponent<AnimalBondSystem>();
        if (bond != null)
        {
            float bondValue = 5f;

            if (_target.foodType == AnimalFoodType.Berry)
                bondValue = 10f;
            else if (_target.foodType == AnimalFoodType.Herb)
                bondValue = 7f;

            bond.AddBond(bondValue);
            
        }
        // 먹이 효과를 감정/호감 시스템에 전달
        // (의존성 줄이려고 SendMessage 사용: 해당 함수가 있으면 실행, 없어도 에러 없음)
        SendMessage("OnEatFood", _target, SendMessageOptions.DontRequireReceiver);

        if (debugLog) Debug.Log($"[FoodEater] ate={_target.name}");

        // 쿨다운/타겟 정리
        _cooldownT = minEatCooldown;
        ClearTarget();
    }

    void ClearTarget()
    {
        _target = null;
        _repathT = 0f;
    }

    bool IsTooAnxious()
    {
        if (!emotionModel) return false;

        // emotionModel에 public float Anxiety가 있으면 읽어서 판단 (리플렉션 최소 사용)
        var t = emotionModel.GetType();
        var f = t.GetField("Anxiety");
        if (f != null && f.FieldType == typeof(float))
        {
            float anx = (float)f.GetValue(emotionModel);
            return anx > ignoreIfAnxietyAbove;
        }

        var p = t.GetProperty("Anxiety");
        if (p != null && p.PropertyType == typeof(float))
        {
            float anx = (float)p.GetValue(emotionModel);
            return anx > ignoreIfAnxietyAbove;
        }

        return false;
    }
    // AnimalFoodEater.cs에 추가 (또는 기존 함수 교체)
    public bool TryEatTransform(Transform foodTr)
    {
        if (foodTr == null) return false;

        // 먹이 오브젝트에 AnimalFood 컴포넌트가 있으면 우선 사용
        var food = foodTr.GetComponent<AnimalFood>();
        if (food != null)
        {
            // 여기에서 포만/유대감/감정 보상 처리 가능
            Destroy(food.gameObject);
            return true;
        }

        // 컴포넌트가 없으면 그냥 해당 오브젝트를 먹이로 간주하고 삭제(임시)
        Destroy(foodTr.gameObject);
        return true;
    }
}