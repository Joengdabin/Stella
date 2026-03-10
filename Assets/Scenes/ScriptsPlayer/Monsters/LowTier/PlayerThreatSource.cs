using UnityEngine;

/// <summary>
/// 테스트용/브릿지용 플레이어 위협 소스.
/// - PlayerLantern이 있으면 해당 레벨 기반으로 ON 판정.
/// - 없으면 키 입력으로 수동 ON/OFF 테스트 가능.
/// </summary>
[DisallowMultipleComponent]
public class PlayerThreatSource : MonoBehaviour, ILowMonsterThreatSource
{
    [Header("Refs (optional)")]
    [SerializeField] private PlayerLantern playerLantern;

    [Header("Fallback Keyboard Toggle")]
    [SerializeField] private bool useKeyboardFallback = true;
    [SerializeField] private KeyCode toggleLanternKey = KeyCode.L;
    [SerializeField] private bool lanternOnInFallback = false;

    [Header("Lantern Mapping")]
    [Tooltip("PlayerLantern의 이 레벨 이상이면 몬스터 입장에서 등불 ON")]
    [SerializeField] private int lanternOnMinLevel = 2;

    [Header("Threat")]
    [SerializeField, Min(0.1f)] private float lanternFearMultiplier = 1f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    public Transform ThreatTransform => transform;
    public bool IsLanternOn => ResolveLanternOn();
    public float LanternFearMultiplier => lanternFearMultiplier;

    private void Awake()
    {
        if (!playerLantern)
            playerLantern = GetComponentInChildren<PlayerLantern>();
    }

    private void Update()
    {
        if (!useKeyboardFallback) return;

        if (Input.GetKeyDown(toggleLanternKey))
        {
            lanternOnInFallback = !lanternOnInFallback;
            if (debugLog)
                Debug.Log($"[ThreatSource] Fallback lantern toggled: {lanternOnInFallback}", this);
        }
    }

    private bool ResolveLanternOn()
    {
        if (playerLantern != null)
            return playerLantern.CurrentLevel >= lanternOnMinLevel;

        return useKeyboardFallback && lanternOnInFallback;
    }
}
