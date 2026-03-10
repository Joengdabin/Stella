using UnityEngine;
using UnityEngine.AI;

[DefaultExecutionOrder(60)] // Locomotion/Brain 이후에 "충돌 직전"만 제어
public class AnimalPlayerAvoidance : MonoBehaviour
{
    [Header("Refs (auto)")]
    public Transform player;
    public string playerTag = "Player";
    public NavMeshAgent agent;

    [Header("Distances")]
    [Tooltip("이 거리 안으로 들어오면 '겹치기 전에' 개입 시작")]
    public float avoidStartDistance = 1.6f;

    [Tooltip("이 거리보다 더 가까우면 강제로 뒤로 물러나게 함")]
    public float hardBackoffDistance = 1.2f;

    [Tooltip("뒤로 물러날 때 목표로 삼는 여유 거리")]
    public float desiredDistance = 1.8f;

    [Header("Behavior")]
    [Tooltip("가까우면 바로 멈춤(Freeze 연출)")]
    public bool stopWhenClose = false;

    [Tooltip("옆으로 비키는 느낌을 주기 위한 측면 오프셋")]
    public float sidestep = 0.8f;

    [Tooltip("목표 갱신 빈도(초). 너무 자주면 덜 자연스러움")]
    public float repathInterval = 0.15f;

    float _nextRepath;

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
        ResolvePlayerIfNeeded();
    }

    void Update()
    {
        if (!agent || !agent.enabled) return;
        ResolvePlayerIfNeeded();
        if (!player) return;

        Vector3 a = transform.position; a.y = 0f;
        Vector3 p = player.position; p.y = 0f;
        float d = Vector3.Distance(a, p);

        if (d > avoidStartDistance)
            return; // 멀면 개입 안 함

        if (Time.time < _nextRepath)
            return;

        _nextRepath = Time.time + repathInterval;

        // 너무 가까우면 무조건 뒤로 물러남
        if (d <= hardBackoffDistance)
        {
            BackOff(a, p, d);
            return;
        }

        // 가까우면 "멈춤" 또는 "옆으로 회피"
        if (stopWhenClose)
        {
            agent.ResetPath();
            agent.isStopped = true;
            return;
        }

        // 옆으로 비키기(회피)
        Vector3 away = (a - p).normalized;
        Vector3 side = Vector3.Cross(Vector3.up, away).normalized; // 좌/우
        if (Random.value < 0.5f) side = -side;

        Vector3 raw = transform.position + away * (desiredDistance - d) + side * sidestep;
        if (NavMesh.SamplePosition(raw, out var hit, 1.5f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }
        else
        {
            // 샘플 실패하면 그냥 뒤로
            BackOff(a, p, d);
        }
    }

    void BackOff(Vector3 aXZ, Vector3 pXZ, float d)
    {
        Vector3 away = (aXZ - pXZ).normalized;
        Vector3 raw = transform.position + away * Mathf.Max(0.4f, desiredDistance - d);

        if (NavMesh.SamplePosition(raw, out var hit, 2.0f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
        }
        else
        {
            // 최후: 멈춤
            agent.ResetPath();
            agent.isStopped = true;
        }
    }

    void ResolvePlayerIfNeeded()
    {
        if (player) return;
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go) player = go.transform;
    }
}