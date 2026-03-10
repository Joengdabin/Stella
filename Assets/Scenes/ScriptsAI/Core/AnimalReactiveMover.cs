using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// ✅ 모든 동물 공용 "반응 이동" 드라이버
/// - Brain(IAnimalBrain)이 내는 상태/지각 정보를 읽고
/// - NavMeshAgent(또는 AnimalLocomotionNavMesh)가 목적지를 잡아 이동하게 함
///
/// 사용법:
/// 1) 동물 오브젝트에 붙이기
/// 2) Brain : RabbitBrain(=IAnimalBrain 구현체) 자동 참조(없으면 자동 탐색)
/// 3) Agent : 동물의 NavMeshAgent 자동 참조
/// 4) (선택) Loco : AnimalLocomotionNavMesh 자동 참조
/// </summary>
[DisallowMultipleComponent]
public class AnimalReactiveMover : MonoBehaviour
{
    [Header("Refs (auto)")]
    [SerializeField] MonoBehaviour brainComponent; // IAnimalBrain을 인스펙터에 드래그 가능하게
    IAnimalBrain brain;

    [SerializeField] NavMeshAgent agent;
    [SerializeField] AnimalLocomotionNavMesh loco; // 있으면 사용(랜덤목적지/도망목적지 유틸)

    [Header("Wander")]
    public float wanderRadius = 6f;
    public float repathCooldown = 0.35f;

    [Header("Freeze")]
    public float freezeSeconds = 0.25f;

    [Header("Retreat/Flee")]
    public float retreatDistance = 7f; // Retreat 목적지 샘플 거리
    public float fleeDistance = 12f;   // Flee 목적지 샘플 거리

    float _freezeT;
    float _repathT;

    void Awake()
    {
        ResolveRefs();
    }

    void OnValidate()
    {
        // 에디터에서 바뀌어도 자동 반영
        if (brainComponent != null) brain = brainComponent as IAnimalBrain;
    }

    void ResolveRefs()
    {
        // Brain
        if (brain == null)
        {
            if (brainComponent != null) brain = brainComponent as IAnimalBrain;
            if (brain == null)
            {
                // 같은 오브젝트에서 IAnimalBrain 구현 MonoBehaviour 찾아보기
                var monos = GetComponents<MonoBehaviour>();
                foreach (var m in monos)
                {
                    if (m is IAnimalBrain b)
                    {
                        brain = b;
                        brainComponent = m;
                        break;
                    }
                }
            }
        }

        // NavMeshAgent
        if (!agent) agent = GetComponent<NavMeshAgent>();

        // Loco(선택)
        if (!loco) loco = GetComponent<AnimalLocomotionNavMesh>();
    }

    void Update()
    {
        if (brain == null || agent == null) return;

        _repathT -= Time.deltaTime;

        AnimalState state = brain.CurrentAnimalState;
        // ✅ BondHold: 귀환 거부 상태(잠깐 더 머무르기)
        if (state == AnimalState.BondHold)
        {
            // 너무 가까우면 잠깐 멈추고, 멀면 따라가기
            // 수치는 공용이므로 대충 기본값. (나중에 Profile/Personality와 연결 가능)
            float followMin = 2.0f;
            float followMax = 5.0f;

            if (brain.DistToPlayer <= followMin)
            {
                StopAgent();
                return;
            }

            if (brain.DistToPlayer >= followMax)
            {
                // 플레이어에게 접근(또는 loco가 있으면 그 유틸 사용)
                if (loco != null)
                {
                    // loco에 ApproachTo(Vector3) 있으면 그걸 쓰고,
                    // 없으면 fallback으로 agent.SetDestination 사용.
                    // (리플렉션 쓰는 구조가 이미 있으니 네 방식에 맞게 한 줄로만)
                }

                ResumeAgent();
                agent.SetDestination(brain.PlayerPosition);
                return;
            }

            // followMin~followMax 사이는 "근처 머무르기"
            StopAgent();
            return;
        }
        // 1) Freeze류: 잠깐 멈추기
        if (state == AnimalState.Freeze || state == AnimalState.FreezeStartle || state == AnimalState.FreezeRecheck)
        {
            if (_freezeT <= 0f) _freezeT = freezeSeconds;

            _freezeT -= Time.deltaTime;
            StopAgent();
            return;
        }
        else
        {
            _freezeT = 0f;
        }

        // 2) Retreat/Flee: 플레이어 반대방향으로 목적지 설정
        if (state == AnimalState.Retreat || state == AnimalState.Flee || state == AnimalState.FleeDespawn)
        {
            if (_repathT > 0f) return;
            _repathT = repathCooldown;

            float dist = (state == AnimalState.Retreat) ? retreatDistance : fleeDistance;
            Vector3 threatPos = brain.PlayerPosition;

            // loco가 있으면 loco 유틸 우선 사용
            if (loco != null)
            {
                // ✅ 네가 추가해둔 공용 함수(이전 대화에서 만들었던 것) 우선 호출
                // - 있으면 그대로 사용
                if (TryCallLocoSetDestinationAwayFrom(threatPos, dist)) return;
                if (TryCallLocoFleeFrom(threatPos, dist)) return;
            }

            // fallback: 여기서 직접 목적지 샘플
            SetDestinationAwayFrom(threatPos, dist);
            return;
        }

        // 3) Wander/기타: 랜덤 목적지로 배회
        if (_repathT > 0f) return;
        _repathT = repathCooldown;

        if (loco != null)
        {
            // loco에 랜덤 목적지 함수가 있으면 그걸 사용
            if (TryCallLocoRandom(wanderRadius)) return;
        }

        SetRandomDestination(wanderRadius);
    }

    // -----------------------------
    // 기본 NavMesh 유틸 (fallback)
    // -----------------------------

    void StopAgent()
    {
        if (!agent.enabled) return;
        agent.isStopped = true;
        agent.ResetPath();
    }

    void ResumeAgent()
    {
        if (!agent.enabled) return;
        agent.isStopped = false;
    }

    bool SetRandomDestination(float radius)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return false;

        Vector3 origin = transform.position;
        for (int i = 0; i < 12; i++)
        {
            Vector3 random = origin + Random.insideUnitSphere * radius;
            random.y = origin.y;

            if (NavMesh.SamplePosition(random, out var hit, 2f, NavMesh.AllAreas))
            {
                ResumeAgent();
                agent.SetDestination(hit.position);
                return true;
            }
        }
        return false;
    }

    bool SetDestinationAwayFrom(Vector3 threatPos, float dist)
    {
        if (!agent.enabled || !agent.isOnNavMesh) return false;

        Vector3 dir = (transform.position - threatPos);
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) dir = Random.insideUnitSphere;

        Vector3 target = transform.position + dir.normalized * dist;

        if (NavMesh.SamplePosition(target, out var hit, 3f, NavMesh.AllAreas))
        {
            ResumeAgent();
            agent.SetDestination(hit.position);
            return true;
        }

        // 샘플 실패 시 fallback 랜덤
        return SetRandomDestination(wanderRadius);
    }

    // ---------------------------------------
    // loco에 있는 함수들을 "있으면" 호출 (리플렉션)
    // - 프로젝트마다 함수명이 조금 달라도 크래시 없이 대응하기 위함
    // ---------------------------------------

    bool TryCallLocoRandom(float radius)
    {
        // 1) SetRandomDestination(float)
        var t = loco.GetType();
        var m = t.GetMethod("SetRandomDestination", new[] { typeof(float) });
        if (m != null)
        {
            var ret = m.Invoke(loco, new object[] { radius });
            if (ret is bool b) return b;
            return true;
        }

        return false;
    }

    bool TryCallLocoSetDestinationAwayFrom(Vector3 threatPos, float dist)
    {
        // 1) SetDestinationAwayFrom(Vector3,float)
        var t = loco.GetType();
        var m = t.GetMethod("SetDestinationAwayFrom", new[] { typeof(Vector3), typeof(float) });
        if (m != null)
        {
            var ret = m.Invoke(loco, new object[] { threatPos, dist });
            if (ret is bool b) return b;
            return true;
        }
        return false;
    }

    bool TryCallLocoFleeFrom(Vector3 threatPos, float dist)
    {
        // 2) FleeFrom(Vector3,float) 또는 RetreatFrom(Vector3,float)
        var t = loco.GetType();

        var m1 = t.GetMethod("FleeFrom", new[] { typeof(Vector3), typeof(float) });
        if (m1 != null)
        {
            m1.Invoke(loco, new object[] { threatPos, dist });
            return true;
        }

        var m2 = t.GetMethod("RetreatFrom", new[] { typeof(Vector3), typeof(float) });
        if (m2 != null)
        {
            m2.Invoke(loco, new object[] { threatPos, dist });
            return true;
        }

        return false;
    }
}
