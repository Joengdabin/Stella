using UnityEngine;

[DisallowMultipleComponent]
public class AnimalBrainOverride : MonoBehaviour, IAnimalBrain
{
    [Header("Target Brain (optional)")]
    [SerializeField] MonoBehaviour targetBrainComponent; // IAnimalBrain 구현체
    IAnimalBrain target;

    [Header("Override State (optional)")]
    public bool overrideState;
    public AnimalState forcedState = AnimalState.Idle;

    public AnimalState CurrentAnimalState => overrideState ? forcedState : (target != null ? target.CurrentAnimalState : AnimalState.Idle);
    public bool CanSeePlayer => target != null && target.CanSeePlayer;
    public Vector3 PlayerPosition => target != null ? target.PlayerPosition : transform.position;
    public float DistToPlayer => target != null ? target.DistToPlayer : float.PositiveInfinity;

    void Awake()
    {
        Resolve();
    }

    void OnValidate()
    {
        Resolve();
    }

    void Resolve()
    {
        target = targetBrainComponent as IAnimalBrain;
        if (target != null) return;

        // 자동 탐색
        var monos = GetComponents<MonoBehaviour>();
        foreach (var m in monos)
        {
            if (m == this) continue;
            if (m is IAnimalBrain b)
            {
                target = b;
                targetBrainComponent = m;
                break;
            }
        }
    }

    // ✅ 인터페이스 구현(필수)
    public void Tick(float dt)
    {
        // override만 할 뿐, 기본은 target brain 실행
        if (target != null)
            target.Tick(dt);
    }
}