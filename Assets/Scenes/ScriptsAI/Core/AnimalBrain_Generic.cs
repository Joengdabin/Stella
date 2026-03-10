using UnityEngine;

public class AnimalBrain_Generic : MonoBehaviour
{
    [Header("Refs (auto)")]
    [SerializeField] AnimalPerception perception;
    [SerializeField] AnimalActions_Generic actions;

    [Header("Player")]
    [SerializeField] string playerTag = "Player";  // perception이 못 잡을 때 대비
    [SerializeField] Transform player;

    [Header("State Thresholds (Inspector Tunable)")]
    [SerializeField] float observeDistance = 12f;
    [SerializeField] float retreatDistance = 6f;
    [SerializeField] float fleeDistance = 3f;

    [Header("Run Reaction (Inspector Tunable)")]
    [SerializeField] float runSpeedThreshold = 2.2f;
    [SerializeField] float runDistanceMultiplier = 1.35f;

    [Header("Friendly Test (Inspector Tunable)")]
    [SerializeField] bool forceFriendly = false;        // 나중에 bond로 대체
    [SerializeField] float friendlyApproachStart = 10f; // 이 거리 안이면 다가오기 시작
    [SerializeField] float friendlyStopDistance = 2.0f; // 이 거리면 멈추고 쳐다보기

    [Header("Debug (Inspector only)")]
    [SerializeField] bool debugLogState = false;

    [Header("Runtime (Read Only)")]
    [SerializeField] AnimalState runtimeState = AnimalState.Wander;
    [SerializeField] float runtimeDistToPlayer;
    [SerializeField] bool runtimeCanSeePlayer;
    [SerializeField] float runtimePlayerSpeed;
    [SerializeField] bool runtimePlayerRunning;

    CharacterController _playerCC;

    void Awake()
    {
        if (!perception) perception = GetComponent<AnimalPerception>();
        if (!actions) actions = GetComponent<AnimalActions_Generic>();
        ResolvePlayer();
    }

    void Update() => Tick();

    public void Tick()
    {
        if (!actions || !actions.IsReady()) return;

        ResolvePlayer();
        if (perception) perception.Tick();

        if (!player)
        {
            SetState(AnimalState.Wander);
            actions.WanderTick();
            WriteRuntime(float.PositiveInfinity, false, 0f, false);
            return;
        }

        float playerSpeed = GetPlayerSpeed();
        bool isRunning = playerSpeed >= runSpeedThreshold;

        float obs = observeDistance;
        float ret = retreatDistance;
        float flee = fleeDistance;

        if (isRunning)
        {
            obs *= runDistanceMultiplier;
            ret *= runDistanceMultiplier;
            flee *= runDistanceMultiplier;
        }

        float dist = Vector3.Distance(transform.position, player.position);

        // “보임”은 perception 우선, 근데 perception이 실패해도 거리로 fallback
        bool canSeeByPerception = perception ? perception.CanSeePlayer : false;
        bool canSeeByDistance = dist <= obs;
        bool canSeePlayer = canSeeByPerception || canSeeByDistance;

        WriteRuntime(dist, canSeePlayer, playerSpeed, isRunning);

        // ---- Friendly (테스트) ----
        if (forceFriendly)
        {
            if (canSeePlayer && dist <= friendlyApproachStart)
            {
                if (dist > friendlyStopDistance)
                {
                    SetState(AnimalState.Approach);
                    // 접근도 “과장”이 필요하면 Actions에 별도 함수로 확장 가능
                    // 지금은 그냥 Observe로 멈추고 보는 걸로 유지해도 됨
                    actions.Resume();
                    // 가까이 가되, 밀치지 않게 stopDistance는 NavMeshAgent에서 조절
                    // (진짜 접근 연출은 다음 단계에서 별도 Action으로 분리 추천)
                    return;
                }
                SetState(AnimalState.Observe);
                actions.ObserveTick(player);
                return;
            }

            SetState(AnimalState.Wander);
            actions.WanderTick();
            return;
        }

        // ---- Normal (경계) ----
        if (canSeePlayer)
        {
            if (dist <= flee)
            {
                SetState(AnimalState.Flee);
                actions.FleeTick(player.position);
                return;
            }

            if (dist <= ret)
            {
                SetState(AnimalState.Retreat);
                actions.RetreatTick(player.position);
                return;
            }

            SetState(AnimalState.Observe);
            actions.ObserveTick(player);
            return;
        }

        SetState(AnimalState.Wander);
        actions.WanderTick();
    }

    void ResolvePlayer()
    {
        if (!player)
        {
            var go = GameObject.FindGameObjectWithTag(playerTag);
            if (go) player = go.transform;
        }

        if (player && !_playerCC)
        {
            _playerCC = player.GetComponent<CharacterController>();
        }
    }

    float GetPlayerSpeed()
    {
        if (_playerCC) return _playerCC.velocity.magnitude;

        // 혹시 CharacterController가 없으면 0 처리(나중에 Rigidbody 지원 추가 가능)
        return 0f;
    }

    void SetState(AnimalState s)
    {
        if (runtimeState == s) return;
        runtimeState = s;
        if (debugLogState) Debug.Log($"[Brain] State -> {runtimeState}", this);
    }

    void WriteRuntime(float dist, bool canSee, float spd, bool running)
    {
        runtimeDistToPlayer = dist;
        runtimeCanSeePlayer = canSee;
        runtimePlayerSpeed = spd;
        runtimePlayerRunning = running;
    }
}