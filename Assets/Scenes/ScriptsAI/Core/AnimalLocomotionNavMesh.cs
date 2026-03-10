using UnityEngine;
using UnityEngine.AI;

public class AnimalLocomotionNavMesh : MonoBehaviour
{
    [Header("Refs")]
    public Transform player;
    public NavMeshAgent agent;

    [Header("Speeds")]
    public float speedObserve = 2.8f;
    public float speedApproach = 3.3f;
    public float speedRetreatCruise = 4.8f;
    public float speedRetreatBurst = 6.2f;
    public float speedFlee = 7.4f;

    [Header("Path Stability")]
    public float repathCooldown = 0.25f;
    public float invalidPathRepathCooldown = 0.35f;
    public float onNavmeshSnapRadius = 2f;

    float _nextRepathTime = 0f;

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
    }

    // -------------------------
    // Core helpers
    // -------------------------
    public bool IsReady()
    {
        return agent && agent.enabled && agent.isOnNavMesh;
    }

    public void Stop()
    {
        if (!IsReady()) return;
        agent.isStopped = true;
        agent.ResetPath();
    }

    public void Resume()
    {
        if (!IsReady()) return;
        agent.isStopped = false;
    }

    public void SetSpeed(float s)
    {
        if (!agent) return;
        agent.speed = s;
    }

    public bool SetDestination(Vector3 worldPos)
    {
        if (!IsReady()) return false;

        agent.isStopped = false;

        // 목적지가 NavMesh 위가 아니면 근처로 스냅
        if (NavMesh.SamplePosition(worldPos, out var hit, onNavmeshSnapRadius, NavMesh.AllAreas))
            worldPos = hit.position;

        return agent.SetDestination(worldPos);
    }

    // -------------------------
    // RabbitActions 호환 함수들
    // -------------------------

    // ✅ Wander용: 반경 내 랜덤 목적지 찍기
    public bool SetRandomDestination(float radius)
    {
        if (!IsReady()) return false;
        if (Time.time < _nextRepathTime) return false;

        Vector3 origin = transform.position;

        for (int i = 0; i < 12; i++)
        {
            Vector3 random = origin + Random.insideUnitSphere * radius;
            if (NavMesh.SamplePosition(random, out var hit, 2f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.SetDestination(hit.position);
                _nextRepathTime = Time.time + repathCooldown;
                return true;
            }
        }

        _nextRepathTime = Time.time + invalidPathRepathCooldown;
        return false;
    }

    // ✅ Retreat / Flee용: 위협(플레이어) 반대 방향으로 목적지
    public bool SetDestinationAwayFrom(Vector3 threatPos, float dist)
    {
        if (!IsReady()) return false;
        if (Time.time < _nextRepathTime) return false;

        Vector3 dir = (transform.position - threatPos);
        dir.y = 0f;

        if (dir.sqrMagnitude < 0.0001f)
            dir = Random.insideUnitSphere;

        Vector3 target = transform.position + dir.normalized * dist;

        if (NavMesh.SamplePosition(target, out var hit, 3f, NavMesh.AllAreas))
        {
            agent.isStopped = false;
            agent.SetDestination(hit.position);
            _nextRepathTime = Time.time + repathCooldown;
            return true;
        }

        _nextRepathTime = Time.time + invalidPathRepathCooldown;
        return false;
    }

    // ✅ 이름 호환용 (네가 쓰던 함수명들)
    public void FleeFrom(Vector3 threatPos, float minDistance)
    {
        SetSpeed(speedFlee);
        SetDestinationAwayFrom(threatPos, minDistance);
    }

    public void RetreatFrom(Vector3 threatPos, float minDistance)
    {
        SetSpeed(speedRetreatCruise);
        SetDestinationAwayFrom(threatPos, minDistance);
    }

    public void ApproachTo(Vector3 targetPos)
    {
        SetSpeed(speedApproach);
        SetDestination(targetPos);
    }

    public void Observe()
    {
        SetSpeed(speedObserve);
        // Observe는 “멈춰서 보기” 느낌이면 stop
        Stop();
    }
}
