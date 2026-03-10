using UnityEngine;
using UnityEngine.AI;

public class AnimalActions_Generic : MonoBehaviour
{
    [Header("Refs (auto)")]
    [SerializeField] NavMeshAgent agent;

    [Header("General (Inspector Tunable)")]
    [SerializeField] float repathCooldown = 0.2f;

    [Header("Wander (Inspector Tunable)")]
    [SerializeField] float wanderRadius = 6f;
    [SerializeField] float wanderInterval = 2f;

    [Header("Observe (Inspector Tunable)")]
    [SerializeField] float observeStepBackDistance = 1.6f; // 너무 가까우면 물러나기 시작
    [SerializeField] float observeFaceTurnSpeed = 900f;     // 과장 회전(눈에 보이게)

    [Header("Retreat / Flee (Inspector Tunable)")]
    [SerializeField] float retreatMoveDistance = 5f; // “한 번” 떨어질 거리(행동 과장)
    [SerializeField] float fleeMoveDistance = 12f;   // 더 크게 도망

    [Header("Debug (Inspector only)")]
    [SerializeField] bool debugDrawGizmos = true;

    float _nextRepath;
    float _nextWander;

    void Awake()
    {
        if (!agent) agent = GetComponent<NavMeshAgent>();
    }

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

    public void WanderTick()
    {
        if (!IsReady()) return;
        if (Time.time < _nextWander) return;

        _nextWander = Time.time + wanderInterval;

        Vector3 origin = transform.position;
        Vector3 random = origin + Random.insideUnitSphere * wanderRadius;
        random.y = origin.y;

        if (NavMesh.SamplePosition(random, out var hit, wanderRadius, NavMesh.AllAreas))
        {
            MoveTo(hit.position);
        }
    }

    public void ObserveTick(Transform player)
    {
        if (!IsReady() || !player) return;

        // 1) 멈춤
        Stop();

        // 2) “보고 있다”가 확 보이게 과장 회전
        FaceWorldPoint(player.position, observeFaceTurnSpeed);

        // 3) 너무 가까우면 살짝 물러남 (밀치기/관통 방지)
        float d = Vector3.Distance(transform.position, player.position);
        if (d < observeStepBackDistance)
        {
            RetreatFrom(player.position, retreatMoveDistance * 0.5f);
        }
    }

    public void RetreatTick(Vector3 threatPos)
    {
        if (!IsReady()) return;
        RetreatFrom(threatPos, retreatMoveDistance);
    }

    public void FleeTick(Vector3 threatPos)
    {
        if (!IsReady()) return;
        RetreatFrom(threatPos, fleeMoveDistance);
    }

    // --- helpers ---
    void MoveTo(Vector3 worldPos)
    {
        if (!IsReady()) return;
        if (Time.time < _nextRepath) return;
        _nextRepath = Time.time + repathCooldown;

        agent.isStopped = false;
        agent.SetDestination(worldPos);
    }

    void RetreatFrom(Vector3 fromPos, float distance)
    {
        // fromPos에서 반대 방향으로 “확실히” 이동
        Vector3 dir = (transform.position - fromPos);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = -transform.forward;
        dir.Normalize();

        Vector3 target = transform.position + dir * distance;
        if (NavMesh.SamplePosition(target, out var hit, distance, NavMesh.AllAreas))
        {
            MoveTo(hit.position);
        }
        else
        {
            MoveTo(target);
        }
    }

    public void FaceWorldPoint(Vector3 targetPos, float turnSpeedDegPerSec)
    {
        Vector3 to = (targetPos - transform.position);
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return;

        Quaternion targetRot = Quaternion.LookRotation(to.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, turnSpeedDegPerSec * Time.deltaTime);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!debugDrawGizmos) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);
    }
#endif
}