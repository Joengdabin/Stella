using UnityEngine;

public class AnimalPerception : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] Transform player;
    [SerializeField] string playerTag = "Player";

    [Header("Sight (Inspector Tunable)")]
    [SerializeField] float eyeHeight = 0.6f;
    [SerializeField] float seeDistance = 12f;
    [SerializeField] LayerMask obstacleMask = ~0;

    [Header("Runtime (Read Only)")]
    [SerializeField] bool runtimeCanSeePlayer;
    [SerializeField] float runtimeDistToPlayer;

    public Transform Player => player;
    public bool CanSeePlayer => runtimeCanSeePlayer;
    public float DistToPlayer => runtimeDistToPlayer;
    public Vector3 PlayerPosition => player ? player.position : transform.position;

    void Awake()
    {
        ResolvePlayerIfNeeded();
    }

    public void Tick()
    {
        ResolvePlayerIfNeeded();

        if (!player)
        {
            runtimeCanSeePlayer = false;
            runtimeDistToPlayer = float.PositiveInfinity;
            return;
        }

        runtimeDistToPlayer = Vector3.Distance(transform.position, player.position);

        // 거리 밖이면 못 본 걸로
        if (runtimeDistToPlayer > seeDistance)
        {
            runtimeCanSeePlayer = false;
            return;
        }

        Vector3 origin = transform.position + Vector3.up * eyeHeight;
        Vector3 target = player.position + Vector3.up * eyeHeight;
        Vector3 dir = (target - origin);
        float dist = dir.magnitude;

        if (dist < 0.001f)
        {
            runtimeCanSeePlayer = true;
            return;
        }

        dir /= dist;

        // 장애물 체크: 막히면 못 봄
        bool blocked = Physics.Raycast(origin, dir, dist, obstacleMask, QueryTriggerInteraction.Ignore);
        runtimeCanSeePlayer = !blocked;
    }

    void ResolvePlayerIfNeeded()
    {
        if (player) return;
        var go = GameObject.FindGameObjectWithTag(playerTag);
        if (go) player = go.transform;
    }
}