using UnityEngine;

[DisallowMultipleComponent]
public class PlayerProximityInteractor : MonoBehaviour
{
    [Header("Scan")]
    [SerializeField] private float radius = 2.5f;
    [SerializeField] private LayerMask scanMask = ~0;
    [SerializeField] private QueryTriggerInteraction triggerMode = QueryTriggerInteraction.Ignore;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool mobileTapToInteract = true;

    [Header("Selection")]
    [Tooltip("각도 가중치: 0이면 거리만, 1이면 시선 방향도 조금 반영")]
    [Range(0f, 1f)]
    [SerializeField] private float viewWeight = 0.35f;

    [Header("Debug")]
    [SerializeField] private bool drawGizmos = true;

    public IInteractable CurrentTarget { get; private set; }

    private Collider[] _hits = new Collider[32];

    void Update()
    {
        FindNearestInteractable();

        if (PressedInteract())
            TryInteract();
    }

    private void FindNearestInteractable()
    {
        CurrentTarget = null;

        int count = Physics.OverlapSphereNonAlloc(transform.position, radius, _hits, scanMask, triggerMode);
        if (count <= 0) return;

        float bestScore = float.MaxValue;

        // 플레이어 "앞" 방향(캐릭터 회전 기준)
        Vector3 forward = transform.forward;

        for (int i = 0; i < count; i++)
        {
            var col = _hits[i];
            if (col == null) continue;

            var interactable = FindInteractableFromCollider(col);
            if (interactable == null) continue;

            // 조건 검사
            var ctx = new InteractorContext { interactor = transform, camera = null, hitPoint = col.transform.position, hitCollider = col };
            if (!interactable.CanInteract(ctx)) continue;

            // 점수: 거리 + (살짝)시선 가중치
            Vector3 to = (col.transform.position - transform.position);
            float dist = to.magnitude;

            float anglePenalty = 0f;
            if (viewWeight > 0f && dist > 0.001f)
            {
                to /= dist;
                float dot = Vector3.Dot(forward, to); // 1 = 정면
                anglePenalty = (1f - Mathf.Clamp01((dot + 1f) * 0.5f)) * radius;
            }

            float score = dist + anglePenalty * viewWeight;

            if (score < bestScore)
            {
                bestScore = score;
                CurrentTarget = interactable;
            }
        }
    }

    private IInteractable FindInteractableFromCollider(Collider col)
    {
        Transform t = col.transform;
        while (t != null)
        {
            var monos = t.GetComponents<MonoBehaviour>();
            for (int i = 0; i < monos.Length; i++)
                if (monos[i] is IInteractable it) return it;
            t = t.parent;
        }
        return null;
    }

    private bool PressedInteract()
    {
        if (Input.GetKeyDown(interactKey)) return true;

        if (mobileTapToInteract && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            return true;

        return false;
    }

    private void TryInteract()
    {
        if (CurrentTarget == null) return;

        var targetBefore = CurrentTarget;
        targetBefore.Interact(new InteractorContext { interactor = transform, camera = null });

        // Destroy 가능성 있으니 즉시 비우기
        CurrentTarget = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmos) return;
        Gizmos.color = Color.white;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}