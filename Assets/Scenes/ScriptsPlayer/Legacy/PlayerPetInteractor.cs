using UnityEngine;

public class PlayerPetInteractor : MonoBehaviour
{
    [Header("Camera")]
    public Camera cam;

    [Header("Ray")]
    public float maxDistance = 50f;
    public LayerMask hitMask = ~0; // 기본: 전부

    [Header("UI Debug")]
    public bool showDebug = true;

    Transform _lastTarget;
    string _lastMsg = "";

    void Awake()
    {
        if (!cam) cam = Camera.main;
    }

    void Update()
    {
        // PC: 마우스 클릭 / 모바일: 터치 시작
        bool pressed = Input.GetMouseButtonDown(0);
        if (!pressed && Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            pressed = true;

        if (!pressed) return;
        if (!cam) return;

        Vector3 sp = Input.mousePosition;
        if (Input.touchCount > 0) sp = Input.GetTouch(0).position;

        Ray r = cam.ScreenPointToRay(sp);

        if (Physics.Raycast(r, out RaycastHit hit, maxDistance, hitMask, QueryTriggerInteraction.Ignore))
        {
            var pettable = hit.collider.GetComponentInParent<AnimalPettable>();
            if (pettable != null)
            {
                bool ok = pettable.TryPet(this.transform);

                _lastTarget = pettable.transform;
                _lastMsg = ok ? "PET: ACCEPT" : "PET: REJECT/COOLDOWN/RANGE";
            }
        }
    }

    void OnGUI()
    {
        if (!showDebug) return;

        GUILayout.BeginArea(new Rect(10, 10, 420, 120), GUI.skin.box);
        GUILayout.Label("Pet Debug (Click / Touch)");
        if (_lastTarget) GUILayout.Label("Target: " + _lastTarget.name);
        GUILayout.Label("Result: " + _lastMsg);
        GUILayout.EndArea();
    }
}