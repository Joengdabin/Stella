using UnityEngine;

[DisallowMultipleComponent]
public class PlayerInteractor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera cam;

    [Header("Ray")]
    [SerializeField] private float maxDistance = 50.0f;
    [SerializeField] private LayerMask hitMask = ~0;

    [Header("Input")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private bool mobileTapToInteract = true;

    [Header("Debug")]
    [SerializeField] private bool showDebug = true;
    [SerializeField] private bool drawRay = true;

    public IInteractable CurrentTarget { get; private set; }
    public string CurrentPrompt { get; private set; }
    public bool HasTarget => CurrentTarget != null;

    // Debug
    private string _hitName = "(none)";
    private string _hitLayer = "(none)";
    private float _hitDist = 0f;
    private string _why = "";

    void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    void Update()
    {
        if (!cam)
        {
            if (Camera.main) cam = Camera.main;
            return;
        }

        FindTarget();

        if (PressedInteract())
            TryInteract();
    }

    void FindTarget()
    {
        CurrentTarget = null;
        CurrentPrompt = "";
        _hitName = "(none)";
        _hitLayer = "(none)";
        _hitDist = 0f;
        _why = "";

        Ray r = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (drawRay) Debug.DrawRay(r.origin, r.direction * maxDistance, Color.yellow);

        if (!Physics.Raycast(r, out RaycastHit hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            _why = "Raycast가 아무것도 못 맞춤(거리/시선/레이어)";
            return;
        }

        _hitName = hit.collider.name;
        _hitLayer = LayerMask.LayerToName(hit.collider.gameObject.layer);
        _hitDist = hit.distance;

        var interactable = FindInteractableFromCollider(hit.collider);
        if (interactable == null)
        {
            _why = "Ray는 맞췄는데 IInteractable이 없음(다른 오브젝트를 먼저 맞춘 것)";
            return;
        }

        var ctx = new InteractorContext
        {
            interactor = transform,
            camera = cam,
            hitPoint = hit.point,
            hitCollider = hit.collider
        };

        if (!interactable.CanInteract(ctx))
        {
            _why = "IInteractable은 찾았지만 CanInteract가 false(조건 실패)";
            return;
        }

        CurrentTarget = interactable;
        CurrentPrompt = interactable.GetPrompt(ctx);
        _why = "OK";
    }

    IInteractable FindInteractableFromCollider(Collider col)
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

    bool PressedInteract()
    {
        if (Input.GetKeyDown(interactKey)) return true;

        if (mobileTapToInteract && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            return true;

        return false;
    }

    void TryInteract()
    {
        if (CurrentTarget == null) return;

        var targetBefore = CurrentTarget;

        targetBefore.Interact(new InteractorContext { interactor = transform, camera = cam });

        // ✅ 상호작용 후 대상이 Destroy될 수 있으니 즉시 비우기
        CurrentTarget = null;
        CurrentPrompt = "";
    }

    void OnGUI()
    {
        if (!showDebug) return;

        GUI.Box(new Rect(10, 10, 560, 140), "Interactor Debug");
        GUI.Label(new Rect(20, 35, 540, 20), cam ? $"Cam: {cam.name}" : "Cam: (null)");
        GUI.Label(new Rect(20, 55, 540, 20), $"RayHit: {_hitName}  Layer: {_hitLayer}  Dist: {_hitDist:F2}");
        GUI.Label(new Rect(20, 75, 540, 20), HasTarget ? $"Target: {CurrentTarget.GetTransform().name}" : "Target: (none)");
        GUI.Label(new Rect(20, 95, 540, 20), HasTarget ? $"Prompt: {CurrentPrompt}" : "Prompt: (none)");
        GUI.Label(new Rect(20, 115, 540, 20), $"Why: {_why}");
    }
}