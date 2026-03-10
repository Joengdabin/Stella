using UnityEngine;

[DisallowMultipleComponent]
public class AnimalAIUpdateDriver : MonoBehaviour
{
    [Header("Refs (auto)")]
    [SerializeField] MonoBehaviour brainComponent; // IAnimalBrain 구현체
    IAnimalBrain brain;

    [Header("Tick")]
    public bool useUnscaledTime = false;

    void Awake()
    {
        ResolveRefs();
    }

    void OnValidate()
    {
        ResolveRefs();
    }

    void ResolveRefs()
    {
        if (brainComponent != null)
        {
            brain = brainComponent as IAnimalBrain;
            if (brain != null) return;
        }

        // 같은 오브젝트에서 IAnimalBrain 구현 MonoBehaviour 자동 탐색
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

    void Update()
    {
        if (brain == null) return;

        float dt = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        brain.Tick(dt);
    }
}