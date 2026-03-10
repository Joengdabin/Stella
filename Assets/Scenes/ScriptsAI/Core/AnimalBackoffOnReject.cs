using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public class AnimalBackoffOnReject : MonoBehaviour
{
    [Header("Refs (auto)")]
    public AnimalPettable pettable;
    public Transform player;
    public string playerTag = "Player";
    public NavMeshAgent agent;

    [Header("Backoff Tuning")]
    [Tooltip("플레이어와 이 거리보다 가까워지면 '밀기' 방지를 위해 자동 회피/정지/물러남")]
    public float minDistanceToPlayer = 0.9f;

    [Tooltip("거절/근접 시 뒤로 물러나는 거리(목표점은 네브메시 위로 보정)")]
    public float backoffDistance = 1.2f;

    [Tooltip("물러난 뒤 잠깐 멈춰서 눈치보는 시간")]
    public float pauseSeconds = 0.35f;

    [Tooltip("연타 방지 쿨다운")]
    public float cooldown = 0.6f;

    [Header("NavMesh Sample")]
    public float sampleRadius = 1.5f;

    float _nextAllowedTime;
    float _pauseLeft;

    void Awake()
    {
        if (!pettable) pettable = GetComponent<AnimalPettable>();
        if (!agent) agent = GetComponent<NavMeshAgent>();

        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go) player = go.transform;
        }
    }

    void OnEnable()
    {
        if (pettable != null)
            pettable.OnPetRejected += HandleRejected;
    }

    void OnDisable()
    {
        if (pettable != null)
            pettable.OnPetRejected -= HandleRejected;
    }

    void Update()
    {
        if (!agent || !agent.enabled) return;

        // 멈춤 타이머
        if (_pauseLeft > 0f)
        {
            _pauseLeft -= Time.deltaTime;
            agent.isStopped = true;
            return;
        }

        // 너무 가까우면 자동 회피(밀기 방지)
        if (player && Time.time >= _nextAllowedTime)
        {
            float d = FlatDistance(transform.position, player.position);
            if (d < minDistanceToPlayer)
            {
                DoBackoff(player, isFromReject: false);
            }
        }
    }

    void HandleRejected(AnimalPettable p, Transform pl)
    {
        if (Time.time < _nextAllowedTime) return;
        if (!pl) pl = player;
        if (!pl) return;

        DoBackoff(pl, isFromReject: true);
    }

    void DoBackoff(Transform pl, bool isFromReject)
    {
        _nextAllowedTime = Time.time + cooldown;

        if (!agent || !agent.enabled) return;

        // 플레이어 반대 방향으로 한 발 물러나는 목표점
        Vector3 a = transform.position;
        Vector3 p = pl.position;

        Vector3 away = (a - p);
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f) away = transform.forward;
        away.Normalize();

        Vector3 rawTarget = a + away * backoffDistance;

        // 네브메시 위로 보정
        if (NavMesh.SamplePosition(rawTarget, out var hit, sampleRadius, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
        else
            agent.SetDestination(rawTarget);

        // 잠깐 멈춰서 "물러남 연출" 느낌 주기
        _pauseLeft = pauseSeconds;
    }

    static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f; b.y = 0f;
        return Vector3.Distance(a, b);
    }
}