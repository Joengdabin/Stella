using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 저등급 몬스터 1차 프로토타입 AI.
/// - 단순 상태 흐름: Patrol -> Alert -> Chase -> AttackWindup -> Attack -> Cooldown
/// - 랜턴 ON + 근접 시 Retreat
/// - 공격 시 손(퀵슬롯) 먹이 우선 드랍, 없으면 인벤토리 드랍
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class LowMonsterAI : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private LowMonsterProfileSO profile;

    [Header("References (auto if empty)")]
    [SerializeField] private Transform player;
    [SerializeField] private MonoBehaviour threatSourceComponent; // ILowMonsterThreatSource 구현체
    [SerializeField] private PlayerSimpleHealth playerHealth;
    [SerializeField] private PlayerItemDropper playerDropper;

    [Header("Line of Sight")]
    [SerializeField] private Transform eyeOrigin;
    [SerializeField] private float eyeHeight = 0.7f;
    [SerializeField] private LayerMask sightBlockMask = ~0;

    [Header("State Timers (read-only)")]
    [SerializeField] private LowMonsterState currentState = LowMonsterState.Patrol;
    [SerializeField] private float stateElapsed;

    [Header("Debug")]
    [SerializeField] private bool forcePlayerFindByTag = true;
    [SerializeField] private string playerTag = "Player";

    private NavMeshAgent _agent;
    private ILowMonsterThreatSource _threatSource;

    private float _attackCooldownTimer;
    private float _retreatTimer;
    private bool _attackDashStarted;
    private bool _didAttackInThisCycle;
    private bool _hasPatrolDestination;

    private Vector3 _spawnPosition;

    public LowMonsterState CurrentState => currentState;
    public LowMonsterProfileSO Profile => profile;
    public Transform Player => player;
    public bool HasTarget => player != null;
    public float DistanceToPlayer => player ? Vector3.Distance(transform.position, player.position) : float.PositiveInfinity;

    public event Action<LowMonsterState, LowMonsterState> OnStateChanged;
    public event Action OnAttackWindupStarted;
    public event Action OnAttackTriggered;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _spawnPosition = transform.position;
        ResolveReferences();
        ApplySpeedFromState(currentState);
    }

    private void OnValidate()
    {
        if (threatSourceComponent != null && !(threatSourceComponent is ILowMonsterThreatSource))
            threatSourceComponent = null;
    }

    private void Update()
    {
        if (!HasValidSetup())
            return;

        ResolveReferences();

        if (_attackCooldownTimer > 0f) _attackCooldownTimer -= Time.deltaTime;
        if (_retreatTimer > 0f) _retreatTimer -= Time.deltaTime;

        stateElapsed += Time.deltaTime;

        // 랜턴 공포 반응은 우선순위 높음
        if (ShouldRetreatBecauseLantern() && currentState != LowMonsterState.Retreat)
            ChangeState(LowMonsterState.Retreat);

        TickStateMachine();
        FaceMoveDirection();
    }

    private bool HasValidSetup()
    {
        if (_agent == null) return false;
        if (profile == null) return false;
        return true;
    }

    private void ResolveReferences()
    {
        if (forcePlayerFindByTag && player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag(playerTag);
            if (p) player = p.transform;
        }

        if (_threatSource == null)
        {
            if (threatSourceComponent is ILowMonsterThreatSource threat)
            {
                _threatSource = threat;
            }
            else if (player != null)
            {
                var monos = player.GetComponents<MonoBehaviour>();
                for (int i = 0; i < monos.Length; i++)
                {
                    if (monos[i] is ILowMonsterThreatSource ts)
                    {
                        _threatSource = ts;
                        break;
                    }
                }
            }
        }

        if (playerHealth == null && player != null)
            playerHealth = player.GetComponent<PlayerSimpleHealth>();

        if (playerDropper == null && player != null)
            playerDropper = player.GetComponent<PlayerItemDropper>();
    }

    private void TickStateMachine()
    {
        switch (currentState)
        {
            case LowMonsterState.Idle:
                TickIdle();
                break;
            case LowMonsterState.Patrol:
                TickPatrol();
                break;
            case LowMonsterState.Alert:
                TickAlert();
                break;
            case LowMonsterState.Chase:
                TickChase();
                break;
            case LowMonsterState.AttackWindup:
                TickAttackWindup();
                break;
            case LowMonsterState.Attack:
                TickAttack();
                break;
            case LowMonsterState.Cooldown:
                TickCooldown();
                break;
            case LowMonsterState.Retreat:
                TickRetreat();
                break;
        }
    }

    private void TickIdle()
    {
        // 저등급은 기본적으로 계속 움직이게, Idle은 짧은 중간 상태로만 사용
        if (stateElapsed >= profile.alertHoldTime)
            ChangeState(LowMonsterState.Patrol);
    }

    private void TickPatrol()
    {
        // 플레이어를 못 봐도 계속 이동
        if (!_hasPatrolDestination || ReachedDestination())
        {
            if (TryGetPatrolPoint(out var patrolPoint))
            {
                _hasPatrolDestination = true;
                MoveTo(patrolPoint);
            }
            else
            {
                // 랜덤 실패 시 스폰 주변으로라도 이동
                MoveTo(_spawnPosition + UnityEngine.Random.insideUnitSphere * profile.alwaysMoveMinDistance);
            }
        }

        if (CanSensePlayerSimple())
            ChangeState(LowMonsterState.Alert);
    }

    private void TickAlert()
    {
        StopAgent();

        if (!CanSensePlayerSimple())
        {
            ChangeState(LowMonsterState.Patrol);
            return;
        }

        if (stateElapsed >= profile.alertHoldTime)
            ChangeState(LowMonsterState.Chase);
    }

    private void TickChase()
    {
        if (!player)
        {
            ChangeState(LowMonsterState.Patrol);
            return;
        }

        MoveTo(player.position);

        if (!CanSensePlayerSimple() && DistanceToPlayer > profile.loseRange)
        {
            ChangeState(LowMonsterState.Patrol);
            return;
        }

        if (DistanceToPlayer <= profile.attackRange && _attackCooldownTimer <= 0f)
            ChangeState(LowMonsterState.AttackWindup);
    }

    private void TickAttackWindup()
    {
        StopAgent();

        if (!player)
        {
            ChangeState(LowMonsterState.Patrol);
            return;
        }

        FaceTarget(player.position);

        if (DistanceToPlayer > profile.attackRange * 1.5f)
        {
            ChangeState(LowMonsterState.Chase);
            return;
        }

        if (stateElapsed >= profile.attackWindupTime)
            ChangeState(LowMonsterState.Attack);
    }

    private void TickAttack()
    {
        if (!player)
        {
            ChangeState(LowMonsterState.Cooldown);
            return;
        }

        if (!_attackDashStarted)
        {
            _attackDashStarted = true;
            StartDashToPlayer();
            OnAttackTriggered?.Invoke();
        }

        if (!_didAttackInThisCycle)
        {
            TryApplyAttackHit();
            _didAttackInThisCycle = true;
        }

        if (stateElapsed >= profile.attackActiveTime)
            ChangeState(LowMonsterState.Cooldown);
    }

    private void TickCooldown()
    {
        StopAgent();

        if (_attackCooldownTimer > 0f)
            return;

        if (CanSensePlayerSimple())
            ChangeState(LowMonsterState.Chase);
        else
            ChangeState(LowMonsterState.Patrol);
    }

    private void TickRetreat()
    {
        if (!player)
        {
            ChangeState(LowMonsterState.Patrol);
            return;
        }

        Vector3 away = transform.position - player.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f)
            away = UnityEngine.Random.insideUnitSphere;

        Vector3 target = transform.position + away.normalized * profile.retreatDistance;
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, profile.retreatDistance, NavMesh.AllAreas))
            MoveTo(hit.position);

        if (_retreatTimer <= 0f)
            ChangeState(CanSensePlayerSimple() ? LowMonsterState.Alert : LowMonsterState.Patrol);
    }

    private bool CanSensePlayerSimple()
    {
        if (!player) return false;

        float dist = DistanceToPlayer;
        if (dist > profile.detectRange) return false;

        Vector3 toPlayer = player.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude <= 0.0001f) return true;

        float angle = Vector3.Angle(transform.forward, toPlayer.normalized);
        if (angle > profile.viewAngle * 0.5f) return false;

        return HasLineOfSight();
    }

    private bool HasLineOfSight()
    {
        if (!player) return false;

        Vector3 origin = eyeOrigin ? eyeOrigin.position : transform.position + Vector3.up * eyeHeight;
        Vector3 target = player.position + Vector3.up * eyeHeight;
        Vector3 dir = target - origin;
        float dist = dir.magnitude;

        if (dist <= 0.001f) return true;
        dir /= dist;

        bool blocked = Physics.Raycast(origin, dir, dist, sightBlockMask, QueryTriggerInteraction.Ignore);
        return !blocked;
    }

    private bool ShouldRetreatBecauseLantern()
    {
        if (!player || _threatSource == null) return false;
        if (!_threatSource.IsLanternOn) return false;

        float fearRange = profile.lanternFearDistance * Mathf.Max(0.1f, _threatSource.LanternFearMultiplier);
        return DistanceToPlayer <= fearRange;
    }

    private void StartDashToPlayer()
    {
        if (!player || !_agent || !_agent.enabled || !_agent.isOnNavMesh) return;

        Vector3 dir = (player.position - transform.position);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = transform.forward;

        Vector3 dashTarget = transform.position + dir.normalized * profile.attackDashDistance;
        if (NavMesh.SamplePosition(dashTarget, out NavMeshHit hit, profile.attackDashDistance, NavMesh.AllAreas))
            dashTarget = hit.position;

        _agent.isStopped = false;
        _agent.speed = profile.chaseSpeed;
        _agent.SetDestination(dashTarget);
    }

    private void TryApplyAttackHit()
    {
        if (!player) return;
        if (DistanceToPlayer > profile.attackReach) return;

        Vector3 hitDir = (player.position - transform.position).normalized;

        if (playerHealth != null)
            playerHealth.ApplyDamage(profile.attackDamage, hitDir, profile.knockbackForce);

        if (playerDropper != null)
            playerDropper.DropOnHitLowTier();
    }

    private bool TryGetPatrolPoint(out Vector3 point)
    {
        Vector3 origin = _spawnPosition;

        for (int i = 0; i < 10; i++)
        {
            Vector3 random = origin + UnityEngine.Random.insideUnitSphere * profile.patrolRadius;
            random.y = origin.y;

            if (NavMesh.SamplePosition(random, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                point = hit.position;
                return true;
            }
        }

        point = transform.position;
        return false;
    }

    private void ChangeState(LowMonsterState next)
    {
        if (currentState == next) return;

        LowMonsterState prev = currentState;
        currentState = next;
        stateElapsed = 0f;

        if (next == LowMonsterState.AttackWindup)
            OnAttackWindupStarted?.Invoke();

        if (next == LowMonsterState.Attack)
        {
            _attackDashStarted = false;
            _didAttackInThisCycle = false;
        }

        if (next == LowMonsterState.Cooldown)
            _attackCooldownTimer = profile.attackCooldownTime;

        if (next == LowMonsterState.Retreat)
            _retreatTimer = profile.retreatDuration;

        if (next == LowMonsterState.Patrol)
            _hasPatrolDestination = false;

        ApplySpeedFromState(next);

        if (profile != null && profile.logStateChanges)
            Debug.Log($"[LowMonsterAI] {name}: {prev} -> {next}", this);

        OnStateChanged?.Invoke(prev, next);
    }

    private void ApplySpeedFromState(LowMonsterState state)
    {
        if (_agent == null || profile == null) return;

        _agent.angularSpeed = profile.turnSpeed * 100f;

        switch (state)
        {
            case LowMonsterState.Retreat:
                _agent.speed = profile.retreatSpeed;
                break;
            case LowMonsterState.Chase:
            case LowMonsterState.Attack:
                _agent.speed = profile.chaseSpeed;
                break;
            default:
                _agent.speed = profile.moveSpeed;
                break;
        }
    }

    private void MoveTo(Vector3 destination)
    {
        if (_agent == null || !_agent.enabled || !_agent.isOnNavMesh) return;

        _agent.isStopped = false;
        _agent.SetDestination(destination);
    }

    private void StopAgent()
    {
        if (_agent == null || !_agent.enabled) return;

        _agent.isStopped = true;
        _agent.ResetPath();
    }

    private bool ReachedDestination()
    {
        if (_agent == null || !_agent.enabled || _agent.pathPending) return false;
        return _agent.remainingDistance <= Mathf.Max(0.1f, _agent.stoppingDistance + profile.alwaysMoveMinDistance * 0.1f);
    }

    private void FaceMoveDirection()
    {
        if (_agent == null) return;

        Vector3 velocity = _agent.velocity;
        velocity.y = 0f;
        if (velocity.sqrMagnitude < 0.001f) return;

        Quaternion targetRot = Quaternion.LookRotation(velocity.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-profile.turnSpeed * Time.deltaTime));
    }

    private void FaceTarget(Vector3 worldPos)
    {
        Vector3 dir = worldPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion target = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-profile.turnSpeed * Time.deltaTime));
    }

    private void OnDrawGizmosSelected()
    {
        if (profile == null || !profile.showDebug) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, profile.detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, profile.attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, profile.lanternFearDistance);

        Vector3 left = Quaternion.Euler(0f, -profile.viewAngle * 0.5f, 0f) * transform.forward;
        Vector3 right = Quaternion.Euler(0f, profile.viewAngle * 0.5f, 0f) * transform.forward;

        Gizmos.color = new Color(1f, 0.65f, 0f);
        Gizmos.DrawRay(transform.position, left * profile.detectRange);
        Gizmos.DrawRay(transform.position, right * profile.detectRange);
    }

    private void OnGUI()
    {
        if (profile == null || !profile.showDebug) return;

        Vector3 screenPos = Camera.main ? Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f) : Vector3.zero;
        if (screenPos.z <= 0f) return;

        Rect r = new Rect(screenPos.x - 70f, Screen.height - screenPos.y - 20f, 140f, 40f);
        GUI.Box(r, $"{profile.monsterName}\n{currentState}");
    }
}
