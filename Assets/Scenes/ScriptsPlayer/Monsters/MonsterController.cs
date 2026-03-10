using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
[RequireComponent(typeof(NavMeshAgent))]
public class MonsterController : MonoBehaviour
{
    [Header("Profile")]
    [SerializeField] private MonsterProfileSO profile;

    [Header("Refs (auto if empty)")]
    [SerializeField] private Transform player;
    [SerializeField] private PlayerLantern playerLantern;
    [SerializeField] private PlayerItemDropper playerDropper;

    [Header("Debug")]
    [SerializeField] private bool logState = false;

    private NavMeshAgent agent;
    private float nextAttackTime;
    private float fleeTimer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();

        if (!player)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }

        if (!playerLantern)
            playerLantern = FindFirstObjectByType<PlayerLantern>();

        if (!playerDropper && player)
            playerDropper = player.GetComponent<PlayerItemDropper>();
    }

    private void Update()
    {
        if (!profile || !player) return;

        float d = Vector3.Distance(transform.position, player.position);

        // 1) 저등급: 등불 레벨이 높으면 도망
        if (profile.tier == MonsterTier.Low && playerLantern != null)
        {
            int lvl = playerLantern.CurrentLevel;
            if (lvl >= profile.fleeAtLanternLevel && d <= profile.fleeReactRange)
            {
                fleeTimer = profile.fleeDuration;
            }
        }

        if (fleeTimer > 0f)
        {
            fleeTimer -= Time.deltaTime;
            DoFlee();
            return;
        }

        // 2) 추적/대기
        if (d <= profile.detectRange)
        {
            DoChase();

            // 3) 공격
            if (d <= profile.attackRange && Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + profile.attackCooldown;
                DoAttack();
            }
        }
        else if (d > profile.loseRange)
        {
            DoIdle();
        }
    }

    void DoIdle()
    {
        agent.speed = profile.chaseSpeed;
        agent.isStopped = true;
        if (logState) Debug.Log("[Monster] Idle");
    }

    void DoChase()
    {
        agent.isStopped = false;
        agent.speed = profile.chaseSpeed;
        agent.SetDestination(player.position);
        if (logState) Debug.Log("[Monster] Chase");
    }

    void DoFlee()
    {
        agent.isStopped = false;
        agent.speed = profile.fleeSpeed;

        Vector3 away = (transform.position - player.position).normalized;
        Vector3 target = transform.position + away * 6f;

        // NavMesh 위로 보정
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 6f, NavMesh.AllAreas))
            target = hit.position;

        agent.SetDestination(target);
        if (logState) Debug.Log("[Monster] Flee");
    }

    void DoAttack()
    {
        if (logState) Debug.Log("[Monster] Attack!");

        // 지금은 “맞으면 아이템 드랍”만
        if (playerDropper != null)
            playerDropper.DropOnHit();
    }
}